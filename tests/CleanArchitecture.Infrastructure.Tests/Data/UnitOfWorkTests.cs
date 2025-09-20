using Microsoft.EntityFrameworkCore;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Domain.ValueObjects;
using CleanArchitecture.Infrastructure.Tests.Data.Repositories;

namespace CleanArchitecture.Infrastructure.Tests.Data;

public class UnitOfWorkTests : RepositoryTestBase
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkTests()
    {
        _unitOfWork = Context;
    }

    [Fact]
    public async Task SaveChangesAsync_WithChanges_ReturnsSavedEntitiesCount()
    {
        // Arrange
        var user1 = User.Create("User 1", Email.Create("user1@example.com"));
        var user2 = User.Create("User 2", Email.Create("user2@example.com"));
        
        Context.Users.AddRange(user1, user2);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(2, result);
        
        var savedUsers = await Context.Users.ToListAsync();
        Assert.Equal(2, savedUsers.Count);
    }

    [Fact]
    public async Task SaveChangesAsync_WithNoChanges_ReturnsZero()
    {
        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Repository_Generic_ReturnsRepositoryInstance()
    {
        // Act
        var userRepository = _unitOfWork.Repository<User>();
        var productRepository = _unitOfWork.Repository<Product>();

        // Assert
        Assert.NotNull(userRepository);
        Assert.NotNull(productRepository);
        Assert.IsAssignableFrom<IRepository<User>>(userRepository);
        Assert.IsAssignableFrom<IRepository<Product>>(productRepository);
    }

    [Fact]
    public async Task Repository_SameType_ReturnsSameInstance()
    {
        // Act
        var userRepository1 = _unitOfWork.Repository<User>();
        var userRepository2 = _unitOfWork.Repository<User>();

        // Assert
        Assert.Same(userRepository1, userRepository2);
    }

    [Fact]
    public async Task BeginTransactionAsync_StartsTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert
        Assert.NotNull(Context.Database.CurrentTransaction);
    }

    [Fact]
    public async Task CommitTransactionAsync_WithChanges_CommitsSuccessfully()
    {
        // Arrange
        var user = User.Create("Test User", Email.Create("test@example.com"));
        Context.Users.Add(user);

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        Assert.Null(Context.Database.CurrentTransaction);
        
        var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == "Test User");
        Assert.NotNull(savedUser);
    }

    [Fact]
    public async Task RollbackTransactionAsync_WithChanges_RollsBackSuccessfully()
    {
        // Arrange
        var user = User.Create("Test User", Email.Create("test@example.com"));
        Context.Users.Add(user);

        // Act
        await _unitOfWork.BeginTransactionAsync();
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        Assert.Null(Context.Database.CurrentTransaction);
        
        var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == "Test User");
        Assert.Null(savedUser);
    }

    [Fact]
    public async Task TransactionWorkflow_WithMultipleOperations_WorksCorrectly()
    {
        // Arrange
        var user = User.Create("Test User", Email.Create("test@example.com"));
        
        // Act
        await _unitOfWork.BeginTransactionAsync();
        
        var userRepository = _unitOfWork.Repository<User>();
        await userRepository.AddAsync(user);
        
        var product = Product.Create("Test Product", Money.Create(100.00m, "USD"), user.Id);
        var productRepository = _unitOfWork.Repository<Product>();
        await productRepository.AddAsync(product);
        
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        Assert.Null(Context.Database.CurrentTransaction);
        
        var savedUser = await Context.Users.FirstOrDefaultAsync(u => u.Name == "Test User");
        var savedProduct = await Context.Products.FirstOrDefaultAsync(p => p.Name == "Test Product");
        
        Assert.NotNull(savedUser);
        Assert.NotNull(savedProduct);
        Assert.Equal(savedUser.Id, savedProduct.UserId);
    }

    [Fact]
    public async Task TransactionWorkflow_WithException_RollsBackAutomatically()
    {
        // Arrange
        var user = User.Create("Test User", Email.Create("test@example.com"));
        Context.Users.Add(user);

        // Act & Assert
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            // This should cause an exception due to duplicate email
            var duplicateUser = User.Create("Another User", Email.Create("test@example.com"));
            Context.Users.Add(duplicateUser);
            
            await _unitOfWork.CommitTransactionAsync();
            
            // Should not reach here
            Assert.True(false, "Expected exception was not thrown");
        }
        catch
        {
            // Transaction should be rolled back automatically
            Assert.Null(Context.Database.CurrentTransaction);
            
            // Verify no users were saved
            var userCount = await Context.Users.CountAsync();
            Assert.Equal(0, userCount);
        }
    }

    [Fact]
    public async Task BeginTransactionAsync_WhenTransactionAlreadyExists_DoesNotCreateNewTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();
        var firstTransaction = Context.Database.CurrentTransaction;
        
        await _unitOfWork.BeginTransactionAsync();
        var secondTransaction = Context.Database.CurrentTransaction;

        // Assert
        Assert.Same(firstTransaction, secondTransaction);
        
        // Cleanup
        await _unitOfWork.RollbackTransactionAsync();
    }
}
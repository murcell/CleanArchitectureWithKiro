using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Data.Configurations;
using CleanArchitecture.Infrastructure.Data.Repositories;
using System.Reflection;

namespace CleanArchitecture.Infrastructure.Data;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
    private IDbContextTransaction? _currentTransaction;
    private readonly Dictionary<Type, object> _repositories = new();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();
        
        var result = await base.SaveChangesAsync(cancellationToken);
        
        // Dispatch domain events after successful save
        await DispatchDomainEventsAsync();
        
        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction != null)
        {
            return;
        }

        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await SaveChangesAsync(cancellationToken);
            await (_currentTransaction?.CommitAsync(cancellationToken) ?? Task.CompletedTask);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await (_currentTransaction?.RollbackAsync(cancellationToken) ?? Task.CompletedTask);
        }
        finally
        {
            _currentTransaction?.Dispose();
            _currentTransaction = null;
        }
    }

    public IRepository<T> Repository<T>() where T : BaseEntity<int>
    {
        var type = typeof(T);
        
        if (_repositories.ContainsKey(type))
        {
            return (IRepository<T>)_repositories[type];
        }

        var repository = new Repository<T>(this);
        _repositories.Add(type, repository);
        
        return repository;
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries<AuditableEntity<int>>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    // CreatedAt is set in the entity constructor
                    // CreatedBy can be set through dependency injection or current user context
                    break;
                case EntityState.Modified:
                    entry.Entity.MarkAsUpdated();
                    // UpdatedBy can be set through dependency injection or current user context
                    break;
            }
        }
    }

    private async Task DispatchDomainEventsAsync()
    {
        var domainEntities = ChangeTracker
            .Entries<BaseEntity<int>>()
            .Where(x => x.Entity.DomainEvents.Any())
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.Entity.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.Entity.ClearDomainEvents());

        // Domain event dispatching will be implemented when we add MediatR integration
        // For now, we just clear the events to prevent them from accumulating
        await Task.CompletedTask;
    }
}
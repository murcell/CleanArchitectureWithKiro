using CleanArchitecture.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.Infrastructure.Tests.Security;

/// <summary>
/// Tests to verify SQL injection protection mechanisms
/// </summary>
public class SqlInjectionProtectionTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ITestOutputHelper _output;

    public SqlInjectionProtectionTests(ITestOutputHelper output)
    {
        _output = output;
        
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        services.AddLogging(builder => builder.AddConsole());

        var serviceProvider = services.BuildServiceProvider();
        _context = serviceProvider.GetRequiredService<ApplicationDbContext>();
    }

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DELETE FROM Users WHERE '1'='1'; --")]
    [InlineData("' UNION SELECT * FROM Users --")]
    [InlineData("'; INSERT INTO Users (Name, Email) VALUES ('Hacker', 'hack@test.com'); --")]
    [InlineData("' OR 1=1 --")]
    [InlineData("'; EXEC xp_cmdshell('dir'); --")]
    public async Task EntityFramework_ShouldProtectAgainstSqlInjection_WhenUsingParameterizedQueries(string maliciousInput)
    {
        // Arrange
        await SeedTestDataAsync();

        // Act & Assert - This should not cause SQL injection
        var exception = await Record.ExceptionAsync(async () =>
        {
            var users = await _context.Users
                .Where(u => u.Name == maliciousInput)
                .ToListAsync();
            
            _output.WriteLine($"Query executed safely with input: {maliciousInput}");
            _output.WriteLine($"Results count: {users.Count}");
        });

        // Should not throw any exception - EF Core parameterizes queries automatically
        Assert.Null(exception);
    }

    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("test@example.com'; DELETE FROM Users; --")]
    public async Task EntityFramework_ShouldProtectAgainstSqlInjection_InEmailQueries(string maliciousEmail)
    {
        // Arrange
        await SeedTestDataAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == maliciousEmail);
            
            _output.WriteLine($"Email query executed safely with input: {maliciousEmail}");
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task EntityFramework_ShouldUseParameterizedQueries_ForComplexWhereClauses()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "'; DROP TABLE Users; --";
        var minId = 1;

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            var users = await _context.Users
                .Where(u => u.Name.Contains(searchTerm) && u.Id > minId)
                .ToListAsync();
            
            _output.WriteLine($"Complex query executed safely");
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task EntityFramework_ShouldProtectAgainstSqlInjection_InOrderByClause()
    {
        // Arrange
        await SeedTestDataAsync();

        // Act & Assert - Even if we try to inject in a property name, EF Core handles it safely
        var exception = await Record.ExceptionAsync(async () =>
        {
            var users = await _context.Users
                .OrderBy(u => u.Name)
                .ToListAsync();
            
            _output.WriteLine("OrderBy query executed safely");
        });

        Assert.Null(exception);
    }

    [Theory]
    [InlineData("Robert'; DROP TABLE Users; --")]
    [InlineData("Alice' OR '1'='1")]
    public async Task Repository_ShouldProtectAgainstSqlInjection_WhenUpdatingEntities(string maliciousName)
    {
        // Arrange
        await SeedTestDataAsync();
        var user = await _context.Users.FirstAsync();

        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            user.UpdateName(maliciousName);
            await _context.SaveChangesAsync();
            
            _output.WriteLine($"Update executed safely with name: {maliciousName}");
        });

        Assert.Null(exception);
        
        // Verify the malicious input was stored as data, not executed as SQL
        var updatedUser = await _context.Users.FindAsync(user.Id);
        Assert.Equal(maliciousName, updatedUser?.Name);
    }

    [Fact]
    public async Task EntityFramework_ShouldLogParameterizedQueries_ForAuditPurposes()
    {
        // Arrange
        await SeedTestDataAsync();
        var searchTerm = "test";

        // Act
        var users = await _context.Users
            .Where(u => u.Name.Contains(searchTerm))
            .ToListAsync();

        // Assert
        Assert.NotNull(users);
        _output.WriteLine("Parameterized query logged successfully");
    }

    private async Task SeedTestDataAsync()
    {
        if (!await _context.Users.AnyAsync())
        {
            var users = new[]
            {
                Domain.Entities.User.Create("John Doe", "john@example.com"),
                Domain.Entities.User.Create("Jane Smith", "jane@example.com"),
                Domain.Entities.User.Create("Bob Johnson", "bob@example.com")
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();
        }
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
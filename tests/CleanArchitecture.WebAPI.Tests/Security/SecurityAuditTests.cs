using CleanArchitecture.Application.Common.Validators;
using CleanArchitecture.Application.DTOs.Requests;
using CleanArchitecture.WebAPI.Tests.Common;
using FluentValidation.TestHelper;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace CleanArchitecture.WebAPI.Tests.Security;

/// <summary>
/// Comprehensive security audit tests
/// </summary>
[Collection("Test Collection")]
public class SecurityAuditTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public SecurityAuditTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        _output = output;
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<img onerror='alert(1)' src='x'>")]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    public async Task API_ShouldRejectMaliciousInput_InUserCreation(string maliciousInput)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = maliciousInput,
            Email = "test@example.com"
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"Successfully rejected malicious input: {maliciousInput}");
    }

    [Theory]
    [InlineData("test@<script>alert('xss')</script>.com")]
    [InlineData("test'; DROP TABLE Users; --@example.com")]
    [InlineData("javascript:alert('xss')@example.com")]
    public async Task API_ShouldRejectMaliciousInput_InEmailField(string maliciousEmail)
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Name = "Test User",
            Email = maliciousEmail
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"Successfully rejected malicious email: {maliciousEmail}");
    }

    [Fact]
    public async Task API_ShouldHaveSecurityHeaders()
    {
        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.True(response.Headers.Contains("X-XSS-Protection"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.True(response.Headers.Contains("Permissions-Policy"));
        
        _output.WriteLine("All required security headers are present");
    }

    [Theory]
    [InlineData("X-Forwarded-Host", "malicious.com")]
    [InlineData("X-Original-URL", "/admin/delete-all")]
    [InlineData("X-Rewrite-URL", "/../../etc/passwd")]
    public async Task API_ShouldRejectDangerousHeaders(string headerName, string headerValue)
    {
        // Arrange
        _client.DefaultRequestHeaders.Add(headerName, headerValue);

        // Act
        var response = await _client.GetAsync("/api/health");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"Successfully rejected dangerous header: {headerName}");
        
        // Cleanup
        _client.DefaultRequestHeaders.Remove(headerName);
    }

    [Theory]
    [InlineData("?search=<script>alert('xss')</script>")]
    [InlineData("?filter='; DROP TABLE Users; --")]
    [InlineData("?sort=javascript:alert('xss')")]
    public async Task API_ShouldRejectMaliciousQueryParameters(string maliciousQuery)
    {
        // Act
        var response = await _client.GetAsync($"/api/users{maliciousQuery}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _output.WriteLine($"Successfully rejected malicious query: {maliciousQuery}");
    }

    [Fact]
    public void Validator_ShouldDetectSqlInjectionAttempts()
    {
        // Arrange
        var validator = new CreateUserRequestValidator();
        var request = new CreateUserRequest
        {
            Name = "'; DROP TABLE Users; --",
            Email = "test@example.com"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Input contains potentially dangerous SQL patterns");
        
        _output.WriteLine("SQL injection validation working correctly");
    }

    [Fact]
    public void Validator_ShouldDetectXssAttempts()
    {
        // Arrange
        var validator = new CreateUserRequestValidator();
        var request = new CreateUserRequest
        {
            Name = "<script>alert('xss')</script>",
            Email = "test@example.com"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("Input contains potentially dangerous script patterns");
        
        _output.WriteLine("XSS validation working correctly");
    }

    [Fact]
    public void Validator_ShouldDetectHtmlTags()
    {
        // Arrange
        var validator = new CreateUserRequestValidator();
        var request = new CreateUserRequest
        {
            Name = "<div>Test User</div>",
            Email = "test@example.com"
        };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("HTML tags are not allowed in this field");
        
        _output.WriteLine("HTML tag validation working correctly");
    }

    [Theory]
    [InlineData("password")]
    [InlineData("123456")]
    [InlineData("qwerty")]
    [InlineData("admin")]
    public void SecurityValidationExtensions_ShouldRejectWeakPasswords(string weakPassword)
    {
        // Arrange
        var validator = new TestPasswordValidator();
        var request = new TestPasswordRequest { Password = weakPassword };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
        _output.WriteLine($"Successfully rejected weak password: {weakPassword}");
    }

    [Theory]
    [InlineData("StrongP@ssw0rd123")]
    [InlineData("MySecure#Pass2024")]
    [InlineData("Complex$Password99")]
    public void SecurityValidationExtensions_ShouldAcceptStrongPasswords(string strongPassword)
    {
        // Arrange
        var validator = new TestPasswordValidator();
        var request = new TestPasswordRequest { Password = strongPassword };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
        _output.WriteLine($"Successfully accepted strong password: {strongPassword}");
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://malicious.com/file")]
    public void SecurityValidationExtensions_ShouldRejectUnsafeUrls(string unsafeUrl)
    {
        // Arrange
        var validator = new TestUrlValidator();
        var request = new TestUrlRequest { Url = unsafeUrl };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Url)
            .WithErrorMessage("URL must use HTTP or HTTPS protocol only");
        
        _output.WriteLine($"Successfully rejected unsafe URL: {unsafeUrl}");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:3000")]
    [InlineData("https://api.example.com/endpoint")]
    public void SecurityValidationExtensions_ShouldAcceptSafeUrls(string safeUrl)
    {
        // Arrange
        var validator = new TestUrlValidator();
        var request = new TestUrlRequest { Url = safeUrl };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Url);
        _output.WriteLine($"Successfully accepted safe URL: {safeUrl}");
    }

    [Theory]
    [InlineData("document.exe")]
    [InlineData("script.bat")]
    [InlineData("malware.scr")]
    [InlineData("virus.vbs")]
    public void SecurityValidationExtensions_ShouldRejectDangerousFileExtensions(string dangerousFileName)
    {
        // Arrange
        var validator = new TestFileValidator();
        var request = new TestFileRequest { FileName = dangerousFileName };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FileName)
            .WithErrorMessage("File type is not allowed for security reasons");
        
        _output.WriteLine($"Successfully rejected dangerous file: {dangerousFileName}");
    }

    // Test helper classes
    public class TestPasswordRequest
    {
        public string Password { get; set; } = string.Empty;
    }

    public class TestPasswordValidator : BaseValidator<TestPasswordRequest>
    {
        public TestPasswordValidator()
        {
            RuleFor(x => x.Password).StrongPassword();
        }
    }

    public class TestUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }

    public class TestUrlValidator : BaseValidator<TestUrlRequest>
    {
        public TestUrlValidator()
        {
            RuleFor(x => x.Url).SafeUrl();
        }
    }

    public class TestFileRequest
    {
        public string FileName { get; set; } = string.Empty;
    }

    public class TestFileValidator : BaseValidator<TestFileRequest>
    {
        public TestFileValidator()
        {
            RuleFor(x => x.FileName).SafeFileExtension();
        }
    }
}
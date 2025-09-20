using CleanArchitecture.Application.Common.Validators;
using FluentValidation;
using FluentValidation.TestHelper;
using Xunit;

namespace CleanArchitecture.Application.Tests.Security;

/// <summary>
/// Unit tests for security validation extensions
/// </summary>
public class SecurityValidationExtensionsTests
{
    [Theory]
    [InlineData("'; DROP TABLE Users; --")]
    [InlineData("' OR '1'='1")]
    [InlineData("'; DELETE FROM Users WHERE '1'='1'; --")]
    [InlineData("' UNION SELECT * FROM Users --")]
    [InlineData("'; INSERT INTO Users (Name, Email) VALUES ('Hacker', 'hack@test.com'); --")]
    [InlineData("' OR 1=1 --")]
    [InlineData("'; EXEC xp_cmdshell('dir'); --")]
    public void NoSqlInjection_ShouldDetectSqlInjectionPatterns(string maliciousInput)
    {
        // Arrange
        var validator = new TestValidator();
        var request = new TestRequest { Input = maliciousInput };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
            .WithErrorMessage("Input contains potentially dangerous SQL patterns");
    }

    [Theory]
    [InlineData("<script>alert('xss')</script>")]
    [InlineData("javascript:alert('xss')")]
    [InlineData("<iframe src='javascript:alert(1)'></iframe>")]
    [InlineData("<img onerror='alert(1)' src='x'>")]
    [InlineData("<svg onload='alert(1)'>")]
    [InlineData("vbscript:msgbox('xss')")]
    [InlineData("<object data='data:text/html,<script>alert(1)</script>'></object>")]
    public void NoXss_ShouldDetectXssPatterns(string maliciousInput)
    {
        // Arrange
        var validator = new TestXssValidator();
        var request = new TestRequest { Input = maliciousInput };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
            .WithErrorMessage("Input contains potentially dangerous script patterns");
    }

    [Theory]
    [InlineData("<div>Test</div>")]
    [InlineData("<span>Content</span>")]
    [InlineData("<p>Paragraph</p>")]
    [InlineData("<a href='#'>Link</a>")]
    public void NoHtmlTags_ShouldDetectHtmlTags(string htmlInput)
    {
        // Arrange
        var validator = new TestHtmlValidator();
        var request = new TestRequest { Input = htmlInput };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
            .WithErrorMessage("HTML tags are not allowed in this field");
    }

    [Theory]
    [InlineData("document.exe")]
    [InlineData("script.bat")]
    [InlineData("malware.scr")]
    [InlineData("virus.vbs")]
    [InlineData("trojan.cmd")]
    [InlineData("hack.ps1")]
    public void SafeFileExtension_ShouldRejectDangerousExtensions(string dangerousFileName)
    {
        // Arrange
        var validator = new TestFileValidator();
        var request = new TestRequest { Input = dangerousFileName };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
            .WithErrorMessage("File type is not allowed for security reasons");
    }

    [Theory]
    [InlineData("document.pdf")]
    [InlineData("image.jpg")]
    [InlineData("data.csv")]
    [InlineData("presentation.pptx")]
    public void SafeFileExtension_ShouldAllowSafeExtensions(string safeFileName)
    {
        // Arrange
        var validator = new TestFileValidator();
        var request = new TestRequest { Input = safeFileName };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Input);
    }

    [Theory]
    [InlineData("password")]
    [InlineData("123456")]
    [InlineData("qwerty")]
    [InlineData("admin")]
    [InlineData("letmein")]
    public void StrongPassword_ShouldRejectWeakPasswords(string weakPassword)
    {
        // Arrange
        var validator = new TestPasswordValidator();
        var request = new TestRequest { Input = weakPassword };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input);
    }

    [Theory]
    [InlineData("StrongP@ssw0rd123")]
    [InlineData("MySecure#Pass2024")]
    [InlineData("Complex$Phrase99")]
    public void StrongPassword_ShouldAcceptStrongPasswords(string strongPassword)
    {
        // Arrange
        var validator = new TestPasswordValidator();
        var request = new TestRequest { Input = strongPassword };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Input);
    }

    [Theory]
    [InlineData("javascript:alert('xss')")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("ftp://malicious.com/file")]
    public void SafeUrl_ShouldRejectUnsafeUrls(string unsafeUrl)
    {
        // Arrange
        var validator = new TestUrlValidator();
        var request = new TestRequest { Input = unsafeUrl };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Input)
            .WithErrorMessage("URL must use HTTP or HTTPS protocol only");
    }

    [Theory]
    [InlineData("https://example.com")]
    [InlineData("http://localhost:3000")]
    [InlineData("https://api.example.com/endpoint")]
    public void SafeUrl_ShouldAcceptSafeUrls(string safeUrl)
    {
        // Arrange
        var validator = new TestUrlValidator();
        var request = new TestRequest { Input = safeUrl };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Input);
    }

    [Theory]
    [InlineData("Normal text")]
    [InlineData("user@example.com")]
    [InlineData("123-456-7890")]
    public void SecurityValidations_ShouldAllowSafeContent(string safeInput)
    {
        // Arrange
        var validator = new TestValidator();
        var request = new TestRequest { Input = safeInput };

        // Act
        var result = validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Input);
    }

    // Test helper classes
    public class TestRequest
    {
        public string Input { get; set; } = string.Empty;
    }

    public class TestValidator : AbstractValidator<TestRequest>
    {
        public TestValidator()
        {
            RuleFor(x => x.Input).NoSqlInjection();
        }
    }

    public class TestXssValidator : AbstractValidator<TestRequest>
    {
        public TestXssValidator()
        {
            RuleFor(x => x.Input).NoXss();
        }
    }

    public class TestHtmlValidator : AbstractValidator<TestRequest>
    {
        public TestHtmlValidator()
        {
            RuleFor(x => x.Input).NoHtmlTags();
        }
    }

    public class TestFileValidator : AbstractValidator<TestRequest>
    {
        public TestFileValidator()
        {
            RuleFor(x => x.Input).SafeFileExtension();
        }
    }

    public class TestPasswordValidator : AbstractValidator<TestRequest>
    {
        public TestPasswordValidator()
        {
            RuleFor(x => x.Input).StrongPassword();
        }
    }

    public class TestUrlValidator : AbstractValidator<TestRequest>
    {
        public TestUrlValidator()
        {
            RuleFor(x => x.Input).SafeUrl();
        }
    }
}
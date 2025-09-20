using FluentValidation;
using System.Text.RegularExpressions;

namespace CleanArchitecture.Application.Common.Validators;

/// <summary>
/// Security-focused validation extensions for FluentValidation
/// </summary>
public static class SecurityValidationExtensions
{
    private static readonly Regex SqlInjectionPattern = new(
        @"(\b(ALTER|CREATE|DELETE|DROP|EXEC(UTE)?|INSERT( +INTO)?|MERGE|SELECT|UPDATE|UNION( +ALL)?)\b)|('(''|[^'])*')|(;|\*|%|--|/\*|\*/)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex XssPattern = new(
        @"<\s*script[^>]*>.*?<\s*/\s*script\s*>|javascript:|vbscript:|onload\s*=|onerror\s*=|onclick\s*=|onmouseover\s*=|<\s*iframe|<\s*object|<\s*embed|<\s*link|<\s*meta",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex HtmlTagPattern = new(
        @"<[^>]+>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly string[] DangerousFileExtensions = 
    {
        ".exe", ".bat", ".cmd", ".com", ".pif", ".scr", ".vbs", ".js", ".jar", ".ps1", ".sh"
    };

    /// <summary>
    /// Validates that input doesn't contain potential SQL injection patterns
    /// </summary>
    public static IRuleBuilderOptions<T, string> NoSqlInjection<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !SqlInjectionPattern.IsMatch(value))
            .WithMessage("Input contains potentially dangerous SQL patterns");
    }

    /// <summary>
    /// Validates that input doesn't contain XSS patterns
    /// </summary>
    public static IRuleBuilderOptions<T, string> NoXss<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !XssPattern.IsMatch(value))
            .WithMessage("Input contains potentially dangerous script patterns");
    }

    /// <summary>
    /// Validates that input doesn't contain HTML tags
    /// </summary>
    public static IRuleBuilderOptions<T, string> NoHtmlTags<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(value => string.IsNullOrEmpty(value) || !HtmlTagPattern.IsMatch(value))
            .WithMessage("HTML tags are not allowed in this field");
    }

    /// <summary>
    /// Validates file extensions for security
    /// </summary>
    public static IRuleBuilderOptions<T, string> SafeFileExtension<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(fileName => 
            {
                if (string.IsNullOrEmpty(fileName)) return true;
                
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                return !DangerousFileExtensions.Contains(extension);
            })
            .WithMessage("File type is not allowed for security reasons");
    }

    /// <summary>
    /// Validates that input contains only safe characters (alphanumeric, spaces, and basic punctuation)
    /// </summary>
    public static IRuleBuilderOptions<T, string> SafeCharactersOnly<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches(@"^[a-zA-Z0-9\s\-_.,!?@#$%&*()+=\[\]{}|\\:;""'<>\/]*$")
            .WithMessage("Input contains unsafe characters");
    }

    /// <summary>
    /// Validates password strength
    /// </summary>
    public static IRuleBuilderOptions<T, string> StrongPassword<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"\d")
            .WithMessage("Password must contain at least one digit")
            .Matches(@"[^\w\s]")
            .WithMessage("Password must contain at least one special character")
            .Must(password => !ContainsCommonPasswords(password))
            .WithMessage("Password is too common and not secure");
    }

    /// <summary>
    /// Validates that input doesn't exceed rate limiting thresholds
    /// </summary>
    public static IRuleBuilderOptions<T, string> WithinRateLimit<T>(this IRuleBuilder<T, string> ruleBuilder, int maxLength = 1000)
    {
        return ruleBuilder
            .MaximumLength(maxLength)
            .WithMessage($"Input exceeds maximum allowed length of {maxLength} characters");
    }

    /// <summary>
    /// Validates URL for security (no javascript:, data:, etc.)
    /// </summary>
    public static IRuleBuilderOptions<T, string> SafeUrl<T>(this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Must(url =>
            {
                if (string.IsNullOrEmpty(url)) return true;
                
                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                    return false;
                
                var scheme = uri.Scheme.ToLowerInvariant();
                return scheme == "http" || scheme == "https";
            })
            .WithMessage("URL must use HTTP or HTTPS protocol only");
    }

    private static bool ContainsCommonPasswords(string password)
    {
        var commonPasswords = new[]
        {
            "password", "123456", "password123", "admin", "qwerty", "letmein",
            "welcome", "monkey", "dragon", "master", "shadow", "superman"
        };

        return commonPasswords.Any(common => 
            password.ToLowerInvariant().Contains(common.ToLowerInvariant()));
    }
}
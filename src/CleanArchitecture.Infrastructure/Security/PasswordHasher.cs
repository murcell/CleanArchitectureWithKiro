using CleanArchitecture.Domain.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace CleanArchitecture.Infrastructure.Security;

/// <summary>
/// Implementation of password hashing using BCrypt
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12; // BCrypt work factor (cost)

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));

        if (string.IsNullOrWhiteSpace(providedPassword))
            throw new ArgumentException("Provided password cannot be null or empty", nameof(providedPassword));

        try
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
        catch
        {
            // If verification fails due to invalid hash format, return false
            return false;
        }
    }
}
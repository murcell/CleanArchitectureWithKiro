namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Interface for password hashing operations
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password using a secure hashing algorithm
    /// </summary>
    /// <param name="password">The plain text password to hash</param>
    /// <returns>The hashed password</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    /// <param name="hashedPassword">The hashed password</param>
    /// <param name="providedPassword">The plain text password to verify</param>
    /// <returns>True if the password matches the hash, false otherwise</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
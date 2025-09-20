namespace CleanArchitecture.Application.DTOs.Requests;

/// <summary>
/// Request DTO for updating an existing user
/// </summary>
public class UpdateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
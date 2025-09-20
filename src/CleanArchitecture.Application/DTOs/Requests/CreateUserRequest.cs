namespace CleanArchitecture.Application.DTOs.Requests;

/// <summary>
/// Request DTO for creating a new user
/// </summary>
public class CreateUserRequest
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
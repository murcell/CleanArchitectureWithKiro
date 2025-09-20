using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.DTOs.Authentication;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.Common.Mappings;

/// <summary>
/// AutoMapper profile for authentication-related mappings
/// </summary>
public class AuthenticationMappingProfile : Profile
{
    public AuthenticationMappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email.Value));

        CreateMap<ApiKey, ApiKeyDto>();
    }
}
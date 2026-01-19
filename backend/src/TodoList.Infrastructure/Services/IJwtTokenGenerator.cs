using Microsoft.AspNetCore.Identity;

namespace TodoList.Infrastructure.Services;

public interface IJwtTokenGenerator
{
    string GenerateToken(IdentityUser user);
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using DocHub.API.Services.Interfaces;
using DocHub.API.DTOs;

namespace DocHub.API.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ModuleAccessAttribute : Attribute, IAsyncAuthorizationFilter
{
    private readonly string _requiredModule;

    public ModuleAccessAttribute(string requiredModule)
    {
        _requiredModule = requiredModule;
    }

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        
        var userIdClaim = context.HttpContext.User.FindFirst("userid")?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            context.Result = new UnauthorizedObjectResult(new ApiResponse<object>
            {
                Success = false,
                Error = new ApiError
                {
                    Code = "UNAUTHORIZED",
                    Message = "Invalid user token"
                }
            });
            return;
        }

        var hasAccess = await authService.HasModuleAccessAsync(userId, _requiredModule);
        if (!hasAccess)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}

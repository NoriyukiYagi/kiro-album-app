using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace AlbumApp.Attributes;

public class AdminOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Check if user is authenticated
        if (!context.HttpContext.User.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new UnauthorizedObjectResult(new
            {
                Success = false,
                Error = "UNAUTHORIZED",
                Message = "Authentication required"
            });
            return;
        }

        // Check if user has admin role
        var hasAdminRole = context.HttpContext.User.HasClaim(ClaimTypes.Role, "Admin");
        
        if (!hasAdminRole)
        {
            context.Result = new ForbidResult();
            return;
        }
    }
}
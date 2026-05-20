using AiAdvisor.Infrastructure.Identity;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AiAdvisor.Web.Endpoints;

public class Users : IEndpointGroup
{
    public static void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapIdentityApi<ApplicationUser>();

        groupBuilder.MapPost(Logout, "logout").RequireAuthorization();
        groupBuilder.MapGet(Me, "me").RequireAuthorization();
    }

    [EndpointSummary("Log out")]
    [EndpointDescription("Logs out the current user by clearing the authentication cookie.")]
    public static async Task<Results<Ok, UnauthorizedHttpResult>> Logout(SignInManager<ApplicationUser> signInManager, [FromBody] object empty)
    {
        if (empty != null)
        {
            await signInManager.SignOutAsync();
            return TypedResults.Ok();
        }

        return TypedResults.Unauthorized();
    }

    [EndpointSummary("Get current user profile")]
    [EndpointDescription("Returns the first name and last name of the currently authenticated user.")]
    public static async Task<Ok<UserProfileDto>> Me(UserManager<ApplicationUser> userManager, HttpContext httpContext)
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        return TypedResults.Ok(new UserProfileDto(user?.FirstName, user?.LastName));
    }
}

public record UserProfileDto(string? FirstName, string? LastName);

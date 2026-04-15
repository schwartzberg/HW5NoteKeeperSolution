using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HW5NoteKeeperSolution.Middleware
{
    public class FirstLoginSeedingMiddleware
    {
        private readonly RequestDelegate _next;

        public FirstLoginSeedingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IUserNoteSeedService userNoteSeedService)
        {
            if (ShouldSeedForRequest(context))
            {
                string userRealmId = context.User.GetObjectIdentifier();
                if (string.IsNullOrWhiteSpace(userRealmId))
                {
                    throw new InvalidOperationException("The authenticated principal did not contain an object identifier claim.");
                }

                await userNoteSeedService.EnsureSeedDataAsync(userRealmId, context.RequestAborted);
            }

            await _next(context);
        }

        private static bool ShouldSeedForRequest(HttpContext context)
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            Endpoint? endpoint = context.GetEndpoint();
            return endpoint?.Metadata.GetMetadata<PageActionDescriptor>() != null
                || endpoint?.Metadata.GetMetadata<ControllerActionDescriptor>() != null;
        }
    }
}

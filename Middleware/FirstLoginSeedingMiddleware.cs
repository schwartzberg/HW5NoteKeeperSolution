using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HW5NoteKeeperSolution.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that triggers first-login seed data creation for each authenticated user.
    /// On the first page or controller request made by an authenticated principal, it calls
    /// <see cref="IUserNoteSeedService.EnsureSeedDataAsync"/> to create the default notes and attachments
    /// for that user if they do not yet exist.
    /// </summary>
    public class FirstLoginSeedingMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Initializes a new instance of <see cref="FirstLoginSeedingMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        public FirstLoginSeedingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        /// <summary>
        /// Invokes the middleware. Seeds data for the authenticated user (once per user lifetime)
        /// and then calls the next middleware.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="userNoteSeedService">The per-request seed service resolved from DI.</param>
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

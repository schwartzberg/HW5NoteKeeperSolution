using HW5NoteKeeperSolution.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HW5NoteKeeperSolution.Middleware
{
    /// <summary>
    /// ASP.NET Core middleware that triggers first-login seed data creation for each authenticated user.
    /// On the first page or controller request made by an authenticated principal, it calls
    /// <see cref="IUserNoteSeedService.EnsureSeedDataAsync"/> to create the default notes and attachments
    /// for that user if they do not yet exist.
    /// Seeding failures (e.g. invalid Azure OpenAI key) are logged and swallowed so that users
    /// can still reach the application; seeding will be retried on the next request.
    /// </summary>
    public class FirstLoginSeedingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<FirstLoginSeedingMiddleware> _logger;

        /// <summary>
        /// Initializes a new instance of <see cref="FirstLoginSeedingMiddleware"/>.
        /// </summary>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <param name="logger">Logger for seeding progress and error diagnostics.</param>
        public FirstLoginSeedingMiddleware(RequestDelegate next, ILogger<FirstLoginSeedingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        /// <summary>
        /// Invokes the middleware. Seeds data for the authenticated user (once per user lifetime)
        /// and then calls the next middleware. If seeding fails, the error is logged and the
        /// request continues so that the user can still access the application.
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

                try
                {
                    await userNoteSeedService.EnsureSeedDataAsync(userRealmId, context.RequestAborted);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex,
                        "First-login seeding failed for user {UserRealmId}. " +
                        "The user can still access the application; seeding will be retried on the next request.",
                        userRealmId);
                }
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
            return endpoint?.Metadata.GetMetadata<PageActionDescriptor>() != null;
        }
    }
}

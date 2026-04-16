using FluentAssertions;
using HW5NoteKeeper.Middleware;
using HW5NoteKeeper.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Security.Claims;
using Xunit;

namespace HW5NoteKeeper.Tests
{
    /// <summary>
    /// Unit tests for <see cref="FirstLoginSeedingMiddleware"/>.
    /// </summary>
    public class FirstLoginSeedingMiddlewareTests
    {
        private readonly Mock<IUserNoteSeedService> _seedServiceMock;

        public FirstLoginSeedingMiddlewareTests()
        {
            _seedServiceMock = new Mock<IUserNoteSeedService>();
            _seedServiceMock
                .Setup(s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        private static HttpContext MakeContext(bool authenticated, string? objectId = null, bool hasPageEndpoint = true)
        {
            DefaultHttpContext httpContext = new DefaultHttpContext();

            if (authenticated)
            {
                List<Claim> claims = [];
                if (objectId != null)
                {
                    claims.Add(new Claim(AzureADClaimTypes.ObjectId, objectId));
                }

                ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
                httpContext.User = new ClaimsPrincipal(identity);
            }

            if (hasPageEndpoint)
            {
                Endpoint endpoint = new Endpoint(
                    _ => Task.CompletedTask,
                    new EndpointMetadataCollection(new PageActionDescriptor()),
                    "TestPage");
                httpContext.SetEndpoint(endpoint);
            }

            return httpContext;
        }

        private FirstLoginSeedingMiddleware CreateMiddleware(RequestDelegate? next = null)
        {
            next ??= _ => Task.CompletedTask;
            return new FirstLoginSeedingMiddleware(next, NullLogger<FirstLoginSeedingMiddleware>.Instance);
        }

        [Fact]
        public async Task Middleware_UnauthenticatedRequest_DoesNotCallSeedService()
        {
            HttpContext context = MakeContext(authenticated: false);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            _seedServiceMock.Verify(
                s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Middleware_AuthenticatedWithObjectId_CallsSeedService()
        {
            string oid = Guid.NewGuid().ToString();
            HttpContext context = MakeContext(authenticated: true, objectId: oid);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            _seedServiceMock.Verify(
                s => s.EnsureSeedDataAsync(oid, It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task Middleware_AuthenticatedWithObjectId_PassesCorrectUserRealmId()
        {
            string oid = Guid.NewGuid().ToString();
            string? capturedId = null;
            _seedServiceMock
                .Setup(s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Callback<string, CancellationToken>((id, _) => capturedId = id)
                .Returns(Task.CompletedTask);

            HttpContext context = MakeContext(authenticated: true, objectId: oid);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            capturedId.Should().Be(oid);
        }

        [Fact]
        public async Task Middleware_AuthenticatedWithoutObjectId_Throws()
        {
            HttpContext context = MakeContext(authenticated: true, objectId: null);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.Invoking(m => m.InvokeAsync(context, _seedServiceMock.Object))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*object identifier*");
        }

        [Fact]
        public async Task Middleware_AuthenticatedButNoEndpoint_DoesNotCallSeedService()
        {
            HttpContext context = MakeContext(authenticated: true, objectId: Guid.NewGuid().ToString(), hasPageEndpoint: false);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            _seedServiceMock.Verify(
                s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task Middleware_AlwaysCallsNext()
        {
            bool nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            HttpContext context = MakeContext(authenticated: false);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware(next);

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Middleware_AuthenticatedWithObjectId_CallsNextAfterSeeding()
        {
            bool nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            string oid = Guid.NewGuid().ToString();
            HttpContext context = MakeContext(authenticated: true, objectId: oid);
            FirstLoginSeedingMiddleware middleware = CreateMiddleware(next);

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            nextCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Middleware_ControllerEndpoint_DoesNotCallSeedService()
        {
            string oid = Guid.NewGuid().ToString();
            DefaultHttpContext context = new DefaultHttpContext();
            List<Claim> claims = [new Claim(AzureADClaimTypes.ObjectId, oid)];
            context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

            Endpoint endpoint = new Endpoint(
                _ => Task.CompletedTask,
                new EndpointMetadataCollection(new ControllerActionDescriptor()),
                "TestController");
            context.SetEndpoint(endpoint);

            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.InvokeAsync(context, _seedServiceMock.Object);

            _seedServiceMock.Verify(
                s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        /// <summary>
        /// Verifies that when the seed service throws (e.g. Azure OpenAI 401), the middleware
        /// logs the error and still calls the next delegate so the user can access the app.
        /// </summary>
        [Fact]
        public async Task Middleware_SeedServiceThrows_LogsWarningAndCallsNext()
        {
            string oid = Guid.NewGuid().ToString();
            HttpContext context = MakeContext(authenticated: true, objectId: oid);

            Mock<IUserNoteSeedService> failingSeedService = new();
            failingSeedService
                .Setup(s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Azure OpenAI API key invalid"));

            bool nextCalled = false;
            RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };

            Mock<ILogger<FirstLoginSeedingMiddleware>> loggerMock = new();
            FirstLoginSeedingMiddleware middleware = new(next, loggerMock.Object);

            await middleware.InvokeAsync(context, failingSeedService.Object);

            nextCalled.Should().BeTrue("the request should proceed even when seeding fails");
            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("First-login seeding failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        /// <summary>
        /// Verifies that <see cref="OperationCanceledException"/> is NOT swallowed — the request
        /// should be cancelled rather than silently continuing with a cancelled token.
        /// </summary>
        [Fact]
        public async Task Middleware_SeedServiceThrowsOperationCanceled_Rethrows()
        {
            string oid = Guid.NewGuid().ToString();
            HttpContext context = MakeContext(authenticated: true, objectId: oid);

            Mock<IUserNoteSeedService> cancelledSeedService = new();
            cancelledSeedService
                .Setup(s => s.EnsureSeedDataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OperationCanceledException());

            FirstLoginSeedingMiddleware middleware = CreateMiddleware();

            await middleware.Invoking(m => m.InvokeAsync(context, cancelledSeedService.Object))
                .Should().ThrowAsync<OperationCanceledException>();
        }
    }
}

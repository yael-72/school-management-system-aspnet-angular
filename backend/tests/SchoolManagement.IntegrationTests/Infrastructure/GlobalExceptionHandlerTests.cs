using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SchoolManagement.Api.Infrastructure;
using SchoolManagement.Application.Common.Exceptions;

namespace SchoolManagement.IntegrationTests.Infrastructure;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext Context) Create(bool clientAborted)
    {
        var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
        var handler = new GlobalExceptionHandler(
            services.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        var context = new DefaultHttpContext { RequestServices = services };
        context.Response.Body = new MemoryStream();
        if (clientAborted)
        {
            var aborted = new CancellationTokenSource();
            aborted.Cancel();
            context.RequestAborted = aborted.Token;
        }
        return (handler, context);
    }

    [Fact]
    public async Task A_request_cancelled_by_the_client_is_not_a_server_error()
    {
        var (handler, context) = Create(clientAborted: true);

        var handled = await handler.TryHandleAsync(context, new OperationCanceledException(), CancellationToken.None);

        handled.Should().BeTrue();
        context.Response.StatusCode.Should().Be(GlobalExceptionHandler.StatusCodeClientClosedRequest);
        context.Response.Body.Length.Should().Be(0);
    }

    [Fact]
    public async Task An_unexpected_exception_is_a_500_without_internal_details()
    {
        var (handler, context) = Create(clientAborted: false);

        await handler.TryHandleAsync(context, new InvalidOperationException("SQL details"), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.Body.Position = 0;
        var body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        body.Should().Contain("An unexpected error occurred.").And.NotContain("SQL details");
    }

    [Fact]
    public async Task A_business_rule_violation_is_a_400_with_its_message()
    {
        var (handler, context) = Create(clientAborted: false);

        await handler.TryHandleAsync(context, new BusinessRuleException("Nope."), CancellationToken.None);

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }
}

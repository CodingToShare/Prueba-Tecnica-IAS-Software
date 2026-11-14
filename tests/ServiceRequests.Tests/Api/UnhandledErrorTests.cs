using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ServiceRequests.Core;

namespace ServiceRequests.Tests.Api;

/// <summary>
/// Regression for centralized 500 handling: an unexpected exception from a dependency becomes a generic
/// ProblemDetails response without internals (closes the Phase 02 open finding).
/// </summary>
public sealed class UnhandledErrorTests
{
    private const string SensitiveMessage =
        "Server=tcp:secret-host,1433;User Id=sa;Password=P@ssw0rd; SELECT * FROM dbo.ServiceRequests failed";

    public static TheoryData<string, string> Requests => new()
    {
        { "Testing", "GET" },
        { "Testing", "POST" },
        { "Development", "GET" },
        { "Development", "POST" },
    };

    [Theory]
    [MemberData(nameof(Requests))]
    public async Task DependencyThrows_Returns500ProblemDetailsWithoutInternals(string environment, string method)
    {
        var repository = new Mock<IServiceRequestRepository>();
        repository.Setup(r => r.ListAsync(It.IsAny<ServiceRequestStatus?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(SensitiveMessage));
        repository.Setup(r => r.FindByExternalReferenceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException(SensitiveMessage));

        await using var factory = new ApiFactory(
            @"Server=(localdb)\MSSQLLocalDB;Database=NotUsedByThisTest;Trusted_Connection=True",
            environment,
            services => services.AddScoped(_ => repository.Object));
        using var client = factory.CreateClient();

        var response = method == "GET"
            ? await client.GetAsync("/api/requests")
            : await client.PostAsJsonAsync("/api/requests",
                new { externalReference = "REQ-500", customerId = "C", serviceCode = "S", description = "d" });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("\"status\":500", body);
        foreach (var leak in new[] { "secret-host", "Password", "User Id", "SELECT", "InvalidOperationException",
                     "StackTrace", "   at ", "ServiceRequests.Core", "Moq" })
        {
            Assert.DoesNotContain(leak, body, StringComparison.OrdinalIgnoreCase);
        }
    }
}

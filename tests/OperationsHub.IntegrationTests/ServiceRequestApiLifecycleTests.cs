using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Persistence;
using OperationsHub.Web.Api;

namespace OperationsHub.IntegrationTests;

public sealed class ServiceRequestApiLifecycleTests : IClassFixture<OperationsHubWebApplicationFactory>
{
    private static readonly Guid RequestTypeId = Guid.Parse("30000000-0000-4000-8000-000000000001");
    private static readonly Guid DepartmentId = Guid.Parse("20000000-0000-4000-8000-000000000001");

    private const string RequesterEmail = "requester@operationshub.local";
    private const string ManagerEmail = "manager@operationshub.local";
    private const string RequesterId = "10000000-0000-4000-8000-000000000001";
    private const string TechnicianId = "10000000-0000-4000-8000-000000000002";
    private const string ManagerId = "10000000-0000-4000-8000-000000000003";
    private const string DemoPassword = "OperationsHub!2026";

    private readonly OperationsHubWebApplicationFactory factory;

    public ServiceRequestApiLifecycleTests(OperationsHubWebApplicationFactory factory) => this.factory = factory;

    [Fact]
    public async Task RequesterAndManagerCookieSessionsCompleteApiLifecycleWithPersistedHistory()
    {
        var requestId = Guid.Empty;
        using var requester = CreateClient();
        using var manager = CreateClient();

        try
        {
            await SignInAndConfigureAntiforgeryAsync(requester, RequesterEmail);
            var uniqueSuffix = Guid.NewGuid().ToString("N");
            var originalTitle = $"Lifecycle test {uniqueSuffix}";
            var updatedTitle = $"Updated lifecycle test {uniqueSuffix}";

            using var createResponse = await requester.PostAsJsonAsync(
                "/api/requests/",
                new CreateServiceRequestCommand(
                    originalTitle,
                    "Exercise the authenticated service-request API from creation through workflow history.",
                    RequestTypeId,
                    ServiceRequestPriority.Normal,
                    DepartmentId),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var created = await ReadDetailAsync(createResponse);
            requestId = created.Id;
            Assert.Equal(originalTitle, created.Title);
            Assert.Equal(ServiceRequestStatus.New, created.Status);
            Assert.Equal(RequesterId, created.RequesterId);
            Assert.Single(created.StatusHistory);

            using var initialGetResponse = await requester.GetAsync($"/api/requests/{requestId}", CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, initialGetResponse.StatusCode);
            var initial = await ReadDetailAsync(initialGetResponse);
            Assert.Equal(created.Version, initial.Version);

            using var updateResponse = await requester.PutAsJsonAsync(
                $"/api/requests/{requestId}",
                new UpdateServiceRequestCommand(
                    updatedTitle,
                    "Updated through the real HTTP API with optimistic concurrency.",
                    RequestTypeId,
                    ServiceRequestPriority.High,
                    DepartmentId,
                    initial.Version),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
            var updated = await ReadDetailAsync(updateResponse);
            Assert.Equal(updatedTitle, updated.Title);
            Assert.Equal(ServiceRequestPriority.High, updated.Priority);
            Assert.True(updated.Version > initial.Version);

            await SignInAndConfigureAntiforgeryAsync(manager, ManagerEmail);
            using var assignmentResponse = await manager.PostAsJsonAsync(
                $"/api/requests/{requestId}/assignments",
                new AssignServiceRequestCommand(TechnicianId, updated.Version),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, assignmentResponse.StatusCode);
            var assigned = await ReadDetailAsync(assignmentResponse);
            Assert.Equal(TechnicianId, assigned.AssigneeId);
            Assert.True(assigned.Version > updated.Version);

            using var statusResponse = await manager.PostAsJsonAsync(
                $"/api/requests/{requestId}/status",
                new ChangeServiceRequestStatusCommand(ServiceRequestStatus.InProgress, assigned.Version),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
            var inProgress = await ReadDetailAsync(statusResponse);
            Assert.Equal(ServiceRequestStatus.InProgress, inProgress.Status);
            Assert.True(inProgress.Version > assigned.Version);

            const string commentBody = "Manager confirmed assignment and started the workflow through the API.";
            using var commentResponse = await manager.PostAsJsonAsync(
                $"/api/requests/{requestId}/comments",
                new AddRequestCommentCommand(commentBody),
                CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, commentResponse.StatusCode);

            using var finalGetResponse = await manager.GetAsync($"/api/requests/{requestId}", CancellationToken.None);
            Assert.Equal(HttpStatusCode.OK, finalGetResponse.StatusCode);
            var final = await ReadDetailAsync(finalGetResponse);
            Assert.Equal(updatedTitle, final.Title);
            Assert.Equal(ServiceRequestPriority.High, final.Priority);
            Assert.Equal(ServiceRequestStatus.InProgress, final.Status);
            Assert.Equal(TechnicianId, final.AssigneeId);
            Assert.Contains(final.Assignments, assignment => assignment.AssigneeId == TechnicianId && assignment.AssignedById == ManagerId);
            Assert.Contains(final.Comments, comment => comment.AuthorId == ManagerId && comment.Body == commentBody);
            Assert.Contains(final.StatusHistory, history => history.Status == ServiceRequestStatus.New && history.ChangedById == RequesterId);
            Assert.Contains(final.StatusHistory, history => history.Status == ServiceRequestStatus.InProgress && history.ChangedById == ManagerId);

            await using var verificationScope = factory.Services.CreateAsyncScope();
            var database = verificationScope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
            var auditTypes = await database.AuditEvents
                .AsNoTracking()
                .Where(auditEvent => auditEvent.ServiceRequestId == requestId)
                .Select(auditEvent => auditEvent.EventType)
                .ToListAsync(CancellationToken.None);

            Assert.Contains("request-created", auditTypes);
            Assert.Contains("request-updated", auditTypes);
            Assert.Contains("request-assigned", auditTypes);
            Assert.Contains("status-changed", auditTypes);
            Assert.Contains("comment-added", auditTypes);
        }
        finally
        {
            if (requestId != Guid.Empty)
            {
                await DeleteRequestAsync(requestId);
            }
        }
    }

    private HttpClient CreateClient() => factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        BaseAddress = new Uri("https://localhost"),
    });

    private static async Task SignInAndConfigureAntiforgeryAsync(HttpClient client, string email)
    {
        using var signInPage = await client.GetAsync("/sign-in", CancellationToken.None);
        signInPage.EnsureSuccessStatusCode();
        var html = await signInPage.Content.ReadAsStringAsync(CancellationToken.None);
        var tokenMatch = Regex.Match(
            html,
            "name=\"__RequestVerificationToken\"[^>]*value=\"(?<token>[^\"]+)\"",
            RegexOptions.CultureInvariant);
        Assert.True(tokenMatch.Success, "The sign-in page did not contain an antiforgery token.");

        using var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["email"] = email,
            ["password"] = DemoPassword,
            ["returnUrl"] = "/",
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(tokenMatch.Groups["token"].Value),
        });
        using var signInResponse = await client.PostAsync("/sign-in", form, CancellationToken.None);
        Assert.Equal(HttpStatusCode.Redirect, signInResponse.StatusCode);
        Assert.Equal("/", signInResponse.Headers.Location?.OriginalString);

        using var tokenResponse = await client.GetAsync("/api/antiforgery", CancellationToken.None);
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        using var tokenDocument = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync(CancellationToken.None));
        var requestToken = tokenDocument.RootElement.GetProperty("requestToken").GetString();
        var headerName = tokenDocument.RootElement.GetProperty("headerName").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));
        Assert.Equal(AntiforgeryEndpointConventionExtensions.HeaderName, headerName);
        client.DefaultRequestHeaders.Add(headerName!, requestToken);
    }

    private static async Task<ServiceRequestDetailDto> ReadDetailAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<ServiceRequestDetailDto>(cancellationToken: CancellationToken.None)
            ?? throw new Xunit.Sdk.XunitException("The API response did not contain a service-request detail document.");

    private async Task DeleteRequestAsync(Guid requestId)
    {
        await using var cleanupScope = factory.Services.CreateAsyncScope();
        var database = cleanupScope.ServiceProvider.GetRequiredService<OperationsHubDbContext>();
        await database.AuditEvents.Where(item => item.ServiceRequestId == requestId).ExecuteDeleteAsync(CancellationToken.None);
        await database.RequestComments.Where(item => item.ServiceRequestId == requestId).ExecuteDeleteAsync(CancellationToken.None);
        await database.RequestStatusHistories.Where(item => item.ServiceRequestId == requestId).ExecuteDeleteAsync(CancellationToken.None);
        await database.RequestAssignments.Where(item => item.ServiceRequestId == requestId).ExecuteDeleteAsync(CancellationToken.None);
        await database.ServiceRequests.Where(item => item.Id == requestId).ExecuteDeleteAsync(CancellationToken.None);
    }
}

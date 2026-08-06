using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Enums;
using OperationsHub.Infrastructure.Requests;

namespace OperationsHub.IntegrationTests;

public sealed class OpenAiRequestClassificationAdvisorTests
{
    [Fact]
    public async Task ClassifyAsyncParsesAValidStructuredResponseWithoutPersistingProviderInput()
    {
        var requestTypeId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                $$"""
                {
                  "output": [
                    {
                      "content": [
                        {
                          "type": "output_text",
                          "text": "{\"requestTypeId\":\"{{requestTypeId}}\",\"priority\":\"High\",\"summary\":\"Restore payroll access\"}"
                        }
                      ]
                    }
                  ]
                }
                """,
                Encoding.UTF8,
                "application/json"),
        });
        using var client = new HttpClient(handler);
        var advisor = new OpenAiRequestClassificationAdvisor(
            client,
            Options.Create(new OpenAiRequestClassificationOptions { ApiKey = "test-key", Model = "test-model" }));
        var context = new RequestClassificationContext(
            "Cannot sign in",
            "Payroll access is blocked.",
            [new RequestClassificationRequestType(requestTypeId, "Access request", null)]);

        var advice = await advisor.ClassifyAsync(context, CancellationToken.None);

        Assert.NotNull(advice);
        Assert.Equal(requestTypeId, advice.SuggestedRequestTypeId);
        Assert.Equal(ServiceRequestPriority.High, advice.SuggestedPriority);
        Assert.Equal("Restore payroll access", advice.Summary);
        Assert.Contains("\"store\":false", handler.RequestBody, StringComparison.Ordinal);
        Assert.Equal("Bearer", handler.AuthorizationScheme);
    }

    [Fact]
    public async Task ClassifyAsyncReturnsNoAdviceWhenProviderRejectsTheRequest()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.TooManyRequests));
        using var client = new HttpClient(handler);
        var advisor = new OpenAiRequestClassificationAdvisor(
            client,
            Options.Create(new OpenAiRequestClassificationOptions { ApiKey = "test-key" }));

        var advice = await advisor.ClassifyAsync(
            new RequestClassificationContext("Title", "Description", []),
            CancellationToken.None);

        Assert.Null(advice);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> responseFactory;

        public RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) =>
            this.responseFactory = responseFactory;

        public string? AuthorizationScheme { get; private set; }

        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            AuthorizationScheme = request.Headers.Authorization?.Scheme;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            return responseFactory(request);
        }
    }
}

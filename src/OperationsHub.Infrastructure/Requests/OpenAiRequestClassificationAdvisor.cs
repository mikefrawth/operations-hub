using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Enums;

namespace OperationsHub.Infrastructure.Requests;

public sealed class OpenAiRequestClassificationOptions
{
    public const string SectionName = "RequestClassification:OpenAi";

    public string? ApiKey { get; set; }

    public string Model { get; set; } = "gpt-5-mini";

    public int TimeoutSeconds { get; set; } = 10;
}

public sealed class OpenAiRequestClassificationAdvisor : IOptionalRequestClassificationAdvisor
{
    private static readonly Uri ResponsesEndpoint = new("https://api.openai.com/v1/responses");
    private static readonly string[] PriorityNames = ["Low", "Normal", "High", "Critical"];
    private static readonly string[] RequiredResponseProperties = ["requestTypeId", "priority", "summary"];
    private readonly HttpClient httpClient;
    private readonly OpenAiRequestClassificationOptions options;

    public OpenAiRequestClassificationAdvisor(
        HttpClient httpClient,
        IOptions<OpenAiRequestClassificationOptions> options)
    {
        this.httpClient = httpClient;
        this.options = options.Value;
    }

    public async Task<RequestClassificationAdvice?> ClassifyAsync(RequestClassificationContext context, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ResponsesEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                model = options.Model,
                store = false,
                input = new[]
                {
                    new { role = "developer", content = "Classify internal service requests. Return an advisory only. Choose a request type ID only from the supplied catalog and do not infer urgency from user preference alone." },
                    new { role = "user", content = BuildPrompt(context) },
                },
                text = new
                {
                    format = new
                    {
                        type = "json_schema",
                        name = "request_classification",
                        strict = true,
                        schema = new
                        {
                            type = "object",
                            additionalProperties = false,
                            properties = new
                            {
                                requestTypeId = new { type = "string" },
                                priority = new { type = "string", @enum = PriorityNames },
                                summary = new { type = "string" },
                            },
                            required = RequiredResponseProperties,
                        },
                    },
                },
            }), Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(responseStream, cancellationToken: cancellationToken);
        var outputText = GetOutputText(document.RootElement);
        if (string.IsNullOrWhiteSpace(outputText))
        {
            return null;
        }

        using var adviceDocument = JsonDocument.Parse(outputText);
        var root = adviceDocument.RootElement;
        var requestTypeId = root.GetProperty("requestTypeId").GetString();
        var priority = root.GetProperty("priority").GetString();
        var summary = root.GetProperty("summary").GetString();
        Guid? suggestedRequestTypeId = null;
        if (!string.IsNullOrWhiteSpace(requestTypeId))
        {
            if (!Guid.TryParse(requestTypeId, out var parsedRequestTypeId))
            {
                return null;
            }

            suggestedRequestTypeId = parsedRequestTypeId;
        }

        return Enum.TryParse<ServiceRequestPriority>(priority, ignoreCase: false, out var parsedPriority) &&
            !string.IsNullOrWhiteSpace(summary)
            ? new RequestClassificationAdvice(suggestedRequestTypeId, parsedPriority, summary)
            : null;
    }

    private static string BuildPrompt(RequestClassificationContext context) => JsonSerializer.Serialize(new
    {
        title = context.Title,
        description = context.Description,
        requestTypes = context.RequestTypes.Select(requestType => new { id = requestType.Id, name = requestType.Name, description = requestType.Description }),
        instructions = "Return a concise neutral summary (280 characters maximum). If no catalog category fits, return an empty requestTypeId.",
    });

    private static string? GetOutputText(JsonElement root)
    {
        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var outputItem in output.EnumerateArray())
        {
            if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in content.EnumerateArray())
            {
                if (contentItem.TryGetProperty("type", out var type) && type.GetString() == "output_text" &&
                    contentItem.TryGetProperty("text", out var text))
                {
                    return text.GetString();
                }
            }
        }

        return null;
    }
}

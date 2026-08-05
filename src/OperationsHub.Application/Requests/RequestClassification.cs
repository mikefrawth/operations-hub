using OperationsHub.Application.ReferenceData;
using OperationsHub.Domain.Entities;
using OperationsHub.Domain.Enums;

namespace OperationsHub.Application.Requests;

/// <summary>Provides non-binding category, priority, and summary suggestions for requesters.</summary>
public interface IRequestClassificationService
{
    public Task<RequestOperationResult<RequestClassificationSuggestion>> ClassifyAsync(
        RequestActor actor,
        RequestClassificationCommand command,
        CancellationToken cancellationToken);
}

public sealed record RequestClassificationCommand(string? Title, string? Description);

public sealed record RequestClassificationSuggestion(
    Guid? SuggestedRequestTypeId,
    ServiceRequestPriority SuggestedPriority,
    string Summary,
    RequestClassificationSource Source);

public enum RequestClassificationSource
{
    Deterministic,
    Ai,
}

/// <summary>Optional infrastructure adapter for an external classifier. Returning <see langword="null"/> preserves local behavior.</summary>
public interface IOptionalRequestClassificationAdvisor
{
    public Task<RequestClassificationAdvice?> ClassifyAsync(RequestClassificationContext context, CancellationToken cancellationToken);
}

public sealed record RequestClassificationContext(
    string Title,
    string Description,
    IReadOnlyList<RequestClassificationRequestType> RequestTypes);

public sealed record RequestClassificationRequestType(Guid Id, string Name, string? Description);

public sealed record RequestClassificationAdvice(Guid? SuggestedRequestTypeId, ServiceRequestPriority SuggestedPriority, string Summary);

public sealed class RequestClassificationService : IRequestClassificationService
{
    private const int SummaryMaximumLength = 280;
    private readonly IReferenceDataStore referenceDataStore;
    private readonly IOptionalRequestClassificationAdvisor optionalAdvisor;

    public RequestClassificationService(
        IReferenceDataStore referenceDataStore,
        IOptionalRequestClassificationAdvisor optionalAdvisor)
    {
        this.referenceDataStore = referenceDataStore;
        this.optionalAdvisor = optionalAdvisor;
    }

    public async Task<RequestOperationResult<RequestClassificationSuggestion>> ClassifyAsync(
        RequestActor actor,
        RequestClassificationCommand command,
        CancellationToken cancellationToken)
    {
        if (actor.Role != RequestActorRole.Requester)
        {
            return Forbidden();
        }

        var title = Normalize(command.Title);
        var description = Normalize(command.Description);
        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
        {
            return Validation("request", "Enter a title or description before requesting a suggestion.");
        }

        var requestTypes = (await referenceDataStore.GetRequestTypesAsync(true, cancellationToken))
            .Select(requestType => new RequestClassificationRequestType(requestType.Id, requestType.Name, requestType.Description))
            .ToList();
        var context = new RequestClassificationContext(title, description, requestTypes);
        var deterministic = CreateDeterministicSuggestion(context);

        try
        {
            var advice = await optionalAdvisor.ClassifyAsync(context, cancellationToken);
            if (IsValidAdvice(advice, requestTypes))
            {
                return Success(new RequestClassificationSuggestion(
                    advice!.SuggestedRequestTypeId,
                    advice.SuggestedPriority,
                    TrimSummary(advice.Summary),
                    RequestClassificationSource.Ai));
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // External advisory failures must not prevent a requester from receiving the local suggestion.
        }

        return Success(deterministic);
    }

    private static RequestClassificationSuggestion CreateDeterministicSuggestion(RequestClassificationContext context)
    {
        var text = $"{context.Title} {context.Description}".ToLowerInvariant();
        var suggestedType = context.RequestTypes
            .Select(requestType => new { RequestType = requestType, Score = Score(requestType, text) })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.RequestType.Name, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.RequestType)
            .FirstOrDefault();

        return new RequestClassificationSuggestion(
            suggestedType?.Id,
            DeterminePriority(text),
            BuildSummary(context.Title, context.Description),
            RequestClassificationSource.Deterministic);
    }

    private static int Score(RequestClassificationRequestType requestType, string text)
    {
        var categoryText = $"{requestType.Name} {requestType.Description}".ToLowerInvariant();
        var score = SharedTokenScore(categoryText, text);

        if (ContainsAny(text, "access", "account", "permission", "login", "sign in", "locked out", "password", "vpn") &&
            ContainsAny(categoryText, "access", "account", "identity", "permission"))
        {
            score += 5;
        }

        if (ContainsAny(text, "office", "door", "desk", "room", "building", "light", "heating") &&
            ContainsAny(categoryText, "facility", "workplace", "building", "office"))
        {
            score += 5;
        }

        return score;
    }

    private static int SharedTokenScore(string categoryText, string text) => categoryText
        .Split([' ', ',', '.', ';', ':', '-', '/', '(', ')'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(token => token.Length >= 4)
        .Distinct(StringComparer.Ordinal)
        .Count(token => text.Contains(token, StringComparison.Ordinal));

    private static ServiceRequestPriority DeterminePriority(string text)
    {
        if (ContainsAny(text, "security incident", "data breach", "data loss", "production down", "company-wide", "all users", "emergency"))
        {
            return ServiceRequestPriority.Critical;
        }

        if (ContainsAny(text, "outage", "locked out", "cannot", "can't", "unable", "blocked", "urgent", "not working"))
        {
            return ServiceRequestPriority.High;
        }

        return ContainsAny(text, "how to", "question", "information", "cosmetic", "minor")
            ? ServiceRequestPriority.Low
            : ServiceRequestPriority.Normal;
    }

    private static string BuildSummary(string title, string description)
    {
        var source = string.Join(" ", new[] { title, description }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var firstSentence = source.Split(['.', '!', '?'], 2, StringSplitOptions.TrimEntries)[0];
        return TrimSummary(firstSentence);
    }

    private static bool IsValidAdvice(RequestClassificationAdvice? advice, IReadOnlyList<RequestClassificationRequestType> requestTypes) =>
        advice is not null &&
        (!advice.SuggestedRequestTypeId.HasValue || requestTypes.Any(type => type.Id == advice.SuggestedRequestTypeId.Value)) &&
        Enum.IsDefined(advice.SuggestedPriority) &&
        !string.IsNullOrWhiteSpace(advice.Summary);

    private static bool ContainsAny(string source, params string[] values) => values.Any(value => source.Contains(value, StringComparison.Ordinal));

    private static string Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? string.Empty : string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string TrimSummary(string value) => value.Length <= SummaryMaximumLength ? value : $"{value[..(SummaryMaximumLength - 1)].TrimEnd()}…";

    private static RequestOperationResult<RequestClassificationSuggestion> Success(RequestClassificationSuggestion value) =>
        new(RequestOperationStatus.Success, value, new Dictionary<string, string[]>());

    private static RequestOperationResult<RequestClassificationSuggestion> Validation(string field, string message) =>
        new(RequestOperationStatus.ValidationFailed, default, new Dictionary<string, string[]> { [field] = [message] });

    private static RequestOperationResult<RequestClassificationSuggestion> Forbidden() =>
        new(RequestOperationStatus.Forbidden, default, new Dictionary<string, string[]>());
}

using OperationsHub.Domain.Enums;

namespace OperationsHub.Web.Components;

internal static class ServiceRequestDisplay
{
    internal static string Status(ServiceRequestStatus status) => status switch
    {
        ServiceRequestStatus.InProgress => "In progress",
        ServiceRequestStatus.OnHold => "On hold",
        _ => status.ToString(),
    };
}

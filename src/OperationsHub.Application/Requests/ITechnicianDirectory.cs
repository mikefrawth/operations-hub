namespace OperationsHub.Application.Requests;

public interface ITechnicianDirectory
{
    public Task<IReadOnlyList<TechnicianDto>> GetActiveTechniciansAsync(CancellationToken cancellationToken);
}

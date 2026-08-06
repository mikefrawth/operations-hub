using Microsoft.EntityFrameworkCore;
using OperationsHub.Application.Requests;
using OperationsHub.Domain.Entities;
using OperationsHub.Infrastructure.Persistence;
using System.Data.Common;

namespace OperationsHub.Infrastructure.Requests;

public sealed class EntityFrameworkServiceRequestStore : IServiceRequestStore
{
    private readonly OperationsHubDbContext database;

    public EntityFrameworkServiceRequestStore(OperationsHubDbContext database) => this.database = database;

    public Task<bool> RequestTypeIsActiveAsync(Guid id, CancellationToken cancellationToken) => database.RequestTypes.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<bool> DepartmentIsActiveAsync(Guid id, CancellationToken cancellationToken) => database.Departments.AnyAsync(x => x.Id == id && x.IsActive, cancellationToken);
    public Task<ServiceRequest?> FindAsync(Guid id, CancellationToken cancellationToken) => database.ServiceRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    public Task RefreshAsync(ServiceRequest request, CancellationToken cancellationToken) => database.Entry(request).ReloadAsync(cancellationToken);
    public async Task<IReadOnlyList<RequestAssignment>> GetAssignmentsAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestAssignments.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.AssignedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<RequestComment>> GetCommentsAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestComments.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<RequestStatusHistory>> GetStatusHistoryAsync(Guid requestId, CancellationToken cancellationToken) => await database.RequestStatusHistories.AsNoTracking().Where(x => x.ServiceRequestId == requestId).OrderBy(x => x.ChangedAtUtc).ToListAsync(cancellationToken);

    public async Task<PagedResult<ServiceRequest>> SearchAsync(ServiceRequestSearchQuery query, string? requesterId, string? assigneeId, CancellationToken cancellationToken)
    {
        var requests = database.ServiceRequests.AsNoTracking().AsQueryable();
        if (requesterId is not null) requests = requests.Where(x => x.RequesterId == requesterId);
        if (assigneeId is not null) requests = requests.Where(x => x.AssigneeId == assigneeId);
        if (query.Status.HasValue) requests = requests.Where(x => x.Status == query.Status.Value);
        if (query.Priority.HasValue) requests = requests.Where(x => x.Priority == query.Priority.Value);
        if (query.Search is not null) requests = requests.Where(x => x.RequestNumber.Contains(query.Search) || x.Title.Contains(query.Search));
        var total = await requests.CountAsync(cancellationToken);
        var offset = checked((query.Page - 1) * query.PageSize);
        var items = await requests.OrderByDescending(x => x.UpdatedAtUtc).ThenBy(x => x.RequestNumber).Skip(offset).Take(query.PageSize).ToListAsync(cancellationToken);
        return new PagedResult<ServiceRequest>(items, query.Page, query.PageSize, total);
    }

    public async Task<PagedResult<OpenRequestSummaryDto>> GetOpenRequestSummariesAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        const string countSql = "SELECT COUNT(*) FROM vw_open_request_summary";
        const string pageSql = """
            SELECT id, request_number, title, status, priority, requester_id, assignee_id, created_at_utc, updated_at_utc, age_seconds
            FROM vw_open_request_summary
            ORDER BY updated_at_utc DESC, request_number
            LIMIT @pageSize OFFSET @offset
            """;
        var connection = database.Database.GetDbConnection();
        await database.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var countCommand = CreateCommand(connection, countSql);
            var total = Convert.ToInt32(await countCommand.ExecuteScalarAsync(cancellationToken), System.Globalization.CultureInfo.InvariantCulture);

            await using var command = CreateCommand(connection, pageSql);
            AddParameter(command, "@pageSize", pageSize);
            AddParameter(command, "@offset", checked((page - 1) * pageSize));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var summaries = new List<OpenRequestSummaryDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                summaries.Add(new OpenRequestSummaryDto(
                    reader.GetFieldValue<Guid>(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    (Domain.Enums.ServiceRequestStatus)reader.GetInt32(3),
                    (Domain.Enums.ServiceRequestPriority)reader.GetInt32(4),
                    reader.GetString(5),
                    reader.IsDBNull(6) ? null : reader.GetString(6),
                    AsUtc(reader.GetDateTime(7)),
                    AsUtc(reader.GetDateTime(8)),
                    TimeSpan.FromSeconds(reader.GetInt64(9))));
            }

            return new PagedResult<OpenRequestSummaryDto>(summaries, page, pageSize, total);
        }
        finally
        {
            await database.Database.CloseConnectionAsync();
        }
    }

    public async Task<ProcedureAssignmentResult> AssignUsingProcedureAsync(Guid requestId, string assigneeId, string actorId, uint expectedVersion, DateTimeOffset assignedAtUtc, CancellationToken cancellationToken)
    {
        const string sql = "CALL sp_assign_request(@requestId, @assigneeId, @actorId, @expectedVersion, @assignedAtUtc)";
        var connection = database.Database.GetDbConnection();
        await database.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await using var command = CreateCommand(connection, sql);
            AddParameter(command, "@requestId", requestId.ToString());
            AddParameter(command, "@assigneeId", assigneeId);
            AddParameter(command, "@actorId", actorId);
            AddParameter(command, "@expectedVersion", expectedVersion);
            AddParameter(command, "@assignedAtUtc", assignedAtUtc.UtcDateTime);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Assignment procedure did not return an outcome.");
            var status = reader.GetString(0) switch
            {
                "success" => ProcedureAssignmentStatus.Success,
                "not_found" => ProcedureAssignmentStatus.NotFound,
                "conflict" => ProcedureAssignmentStatus.Conflict,
                "closed" => ProcedureAssignmentStatus.Closed,
                "invalid_assignee" => ProcedureAssignmentStatus.InvalidAssignee,
                var outcome => throw new InvalidOperationException($"Unexpected assignment procedure outcome '{outcome}'."),
            };
            return new ProcedureAssignmentResult(status, reader.IsDBNull(1) ? null : reader.GetFieldValue<uint>(1));
        }
        finally
        {
            await database.Database.CloseConnectionAsync();
        }
    }

    public void Add(ServiceRequest request) => database.ServiceRequests.Add(request);
    public void Add(RequestAssignment assignment) => database.RequestAssignments.Add(assignment);
    public void Add(RequestComment comment) => database.RequestComments.Add(comment);
    public void Add(RequestStatusHistory history) => database.RequestStatusHistories.Add(history);
    public void Add(AuditEvent auditEvent) => database.AuditEvents.Add(auditEvent);
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new RequestStoreConcurrencyException(exception);
        }
    }

    private static DbCommand CreateCommand(DbConnection connection, string commandText)
    {
        var command = connection.CreateCommand();
        command.CommandText = commandText;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}

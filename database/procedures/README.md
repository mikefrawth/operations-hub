# Stored procedures

Stored-procedure definitions belong here. [sp_assign_request.sql](sp_assign_request.sql) atomically updates an assignment, increments its expected-version token, and appends assignment/audit history. Its migration-owned deployment definition is [20260729113818_AddRequestReportingAndAssignmentProcedure.cs](../../src/OperationsHub.Infrastructure/Persistence/Migrations/20260729113818_AddRequestReportingAndAssignmentProcedure.cs).

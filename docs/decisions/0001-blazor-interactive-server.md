# ADR 0001: Blazor Web App with Interactive Server

- Status: Accepted
- Date: 2026-07-28

## Context

OperationsHub is an authenticated internal application with form-heavy workflows, server-owned authorization, and a portfolio requirement to demonstrate Blazor.

## Decision

Use a .NET 10 Blazor Web App with global Interactive Server rendering. Keep business operations outside Razor components and use Bootstrap for the MVP.

## Consequences

The browser receives a small initial payload and security/data access stay centralized. Each active user requires a server circuit, transient connectivity affects interaction, and multi-instance deployment must account for circuit affinity or an appropriate scale-out design.

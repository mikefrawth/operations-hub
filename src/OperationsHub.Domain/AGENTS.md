# Domain layer guide

## Responsibility

This project owns entities, value objects that earn their complexity, enums, domain rules, and domain exceptions.

## Dependencies and placement

- It must not reference any other OperationsHub project.
- Keep it independent of EF Core, ASP.NET Core, Identity, MySQL, logging, serialization, and UI concerns.
- Model state changes through methods that enforce invariants rather than unrestricted public setters.
- Store UTC instants and model relationships with identifiers without persistence-specific attributes.
- Add domain events only when an actual in-process coordination problem exists.

## Conventions and testing

- Use singular entity names and intention-revealing methods.
- Keep enum values stable once persisted.
- Explain only non-obvious business rules in comments.
- Unit-test allowed and rejected state transitions and other invariants directly.

Avoid anemic entities with rules scattered elsewhere, framework annotations, service dependencies, cascade-delete assumptions, and speculative value objects.

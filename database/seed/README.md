# Database seed scripts

Development-only SQL seed helpers belong here when EF/Identity seeding is not the appropriate owner. Never add production data or reusable production credentials.

Current demo identities are created through the environment-guarded Development initializer in Infrastructure, not through EF model seed data or SQL scripts. Stable roles, departments, and request types remain EF model seed data.

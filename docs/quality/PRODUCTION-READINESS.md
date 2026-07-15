# Production-readiness assessment

BorrowBox v1.0 is course-complete, not production-ready for real student data.

## Ready as a teaching system

- Business rules have focused regression tests.
- HTTP errors use Problem Details.
- CI enforces restore locks, formatting, Release build, tests, and dependency audit.
- Health endpoints and correlation IDs support basic diagnosis.
- A SQLite adapter demonstrates a durable boundary.

## Required before real deployment

- Replace the forgeable demo authentication handler with validated institutional identity.
- Select durable persistence for every aggregate, generate reviewed EF migrations, and test backup/restore.
- Make multi-record workflows transactional, including equipment plus loan/reservation history.
- Add rate limits, request-size limits, secret management, TLS deployment, security headers, and audit-log retention.
- Define SLOs, metrics, alerts, log redaction, ownership, disaster recovery, and privacy retention/deletion rules.
- Threat-model the actual hosting environment and run load, penetration, migration, and recovery tests.

The `/health/ready` response deliberately reports the non-durable default so a demo cannot be mistaken for a production configuration.

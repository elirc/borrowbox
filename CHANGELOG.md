# Changelog

## 1.0.0 - 2026-07-15

- Completed the fifteen-sprint equipment-lending curriculum.
- Added reservations, background expiry, inspections, lending limits, authorization concepts, JSON/CSV reporting, SQLite persistence adapter, health endpoints, and correlation IDs.
- Added deterministic regressions for double returns, clock use, and competing checkout.
- Added testing, threat-model, over-engineering, integration, operations, and production-readiness guides.

Known limit: the API selects non-durable in-memory stores and forgeable demo-header authentication by default. It is a teaching release, not a production deployment.

## 0.5.0 - 2026-07-15

- Added equipment CRUD/search, borrowers, checkout, and return foundations.

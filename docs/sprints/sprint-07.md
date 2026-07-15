# Sprint 7: SQLite persistence

## Goal

Persist equipment with Entity Framework Core and SQLite while keeping business rules in `BorrowBox.Core`.

## What to learn

- `BorrowBoxDbContext` describes the database-facing model.
- `EquipmentRow` is a storage shape; `EquipmentItem` remains the business object.
- `SqliteEquipmentStore` implements the same small interface as the in-memory store.
- `001_initial.sql` makes the first schema change explicit and reviewable.
- The integration test opens a real in-memory SQLite database and verifies data through a fresh context.

The API still selects the in-memory adapter by default. This lets a learner compare adapters before adding environment-based configuration later. In a production application, use generated EF migrations and run them as a controlled deployment step.

## Definition of done

- Equipment can be inserted, read, listed, and updated through SQLite.
- Asset tags are unique at the database boundary.
- A real SQLite integration test passes in CI.

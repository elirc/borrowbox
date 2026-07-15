# Sprint 10: damage inspections

## Goal

Capture damage reports and their resolution as history instead of overwriting a status with no explanation.

An inspection has facts (damage and timestamps) plus a guarded completion. The outcome drives the equipment state:

- `Passed` returns equipment to availability.
- `Repair` keeps equipment in inspection.
- `Retired` permanently removes it from circulation.

The parameterized test is the key lesson: one behavior table covers every outcome while keeping each case readable.

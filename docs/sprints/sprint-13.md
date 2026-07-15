# Sprint 13: reporting and CSV

## Goal

Build read-focused output without leaking storage objects or duplicating business workflows.

The inventory report projects equipment into a JSON summary and a CSV export. The CSV test covers commas and quotes because exports often fail at escaping rather than at the happy path.

## Design lesson

Reporting starts as ordinary code over the existing store. Add a separate read database or analytics service only when measured scale or ownership needs justify it.

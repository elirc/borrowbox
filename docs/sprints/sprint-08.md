# Sprint 8: reservations

## Goal

Add a small reservation workflow and practice coordinating related business objects.

## State transitions

`Active -> Collected`, `Active -> Cancelled`, or `Active -> Expired`.

Creating a reservation moves equipment from `Available` to `Reserved`. Cancelling moves it back. The service coordinates these objects; each object guards its own valid states.

## Review prompts

- Why is an enum clearer than several booleans?
- What should happen if saving the equipment succeeds but saving the reservation fails?
- Which race condition still exists when two callers reserve at once?

The last two questions prepare the transaction and concurrency work in Sprint 14.

# Sprint 9: expiring reservations

## Goal

Learn how a hosted service performs periodic business work without putting that work in an HTTP endpoint.

`ReservationExpiryWorker` only schedules the work. `ReservationExpiryService` owns the use case, so it can be tested without waiting for a timer.

## Operational questions

- What happens when two application instances run the worker?
- How should a failure be retried and observed?
- Why can reading the machine clock directly make a test difficult?

The implementation is intentionally small. The clock and concurrent-worker risks become concrete counterexamples in Sprint 14.

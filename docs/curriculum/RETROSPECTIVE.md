# Course retrospective

## Progression

The course began with one class, one store, and one endpoint slice. Each later concept answered a problem already visible: SQLite answered persistence, policies answered lending limits, an injected clock answered nondeterministic time, and atomic checkout answered a reproduced race.

## What a learner should now be able to do

- Trace an HTTP request through a use case into an adapter.
- Place business rules in objects or services rather than controllers.
- choose unit, integration, or API tests based on the boundary being proved.
- Read a CI failure, create a regression test, and make a scoped repair.
- Explain 401 versus 403, persistence versus memory, and liveness versus readiness.
- Push back on speculative architecture while naming evidence that would justify it.

## Suggested capstone

Choose one production-readiness gap, write an architecture decision record, implement the smallest vertical slice, and demonstrate it with tests and an updated runbook. Good options are durable loan persistence, idempotent checkout, or real token validation in a local identity test environment.

# Over-engineering review

BorrowBox is intentionally a teaching monolith. Its three production projects separate HTTP, business rules, and adapters, but they deploy as one application. That separation makes changes and integrations easier without introducing network boundaries.

## Complexity that earns its place

- Store interfaces allow an in-memory teaching default and a real SQLite adapter.
- Domain methods guard state transitions close to the data they protect.
- `IClock` exists because Sprint 14 demonstrates a real time-dependent defect.
- The atomic checkout operation exists because a deterministic competing-request test proves the race.
- API authentication/authorization uses framework policies so a real identity provider can replace the demo handler.

## Complexity deliberately avoided

- No microservices, message broker, Kubernetes, CQRS, MediatR, AutoMapper, generic repository, unit-of-work framework, or universal result type.
- No base class shared by unrelated domain objects.
- No event-sourcing system for a small equipment catalog.
- No abstract CSV framework or reporting database.
- No production identity simulation disguised as secure authentication.

## Areas that may look over-engineered

Fifteen PRs and sixty issues are more process than this small product needs. They exist to create repeated practice with scope, review, CI, milestones, and releases. A real team could group this amount of code into fewer delivery slices.

Sprint 1's service and store interface are slightly more structure than one create/get endpoint requires. They become justified by later adapters and workflows; if the course ended at Sprint 1, plain endpoint-local code would be reasonable.

Separate row and domain models add mapping. Here that cost keeps EF concerns out of business rules and demonstrates an integration seam. For a disposable CRUD tool with no rules, using EF entities directly could be simpler.

## Change triggers

Add architecture only after evidence:

- Add transactions when a use case must atomically change multiple durable records.
- Add an outbox/message broker when a required external side effect needs reliable asynchronous delivery.
- Split a service when independent ownership, scale, or release cadence outweighs network and operational costs.
- Add caching after measurements identify a read bottleneck and invalidation behavior is understood.

The default answer is readable code and one more focused test, not another framework.

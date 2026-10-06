# Learner guide

BorrowBox is an equipment-lending teaching monolith: three .NET projects
(`src/BorrowBox.Api` → HTTP, `src/BorrowBox.Core` → business rules,
`src/BorrowBox.Infrastructure` → adapters) built over fifteen reviewed
sprints, with a full upskilling course layered on top. This page is the map;
everything it names exists in this repo today.

## What to read, in order

1. **The sprints** — `docs/sprints/sprint-01.md` … `sprint-15.md`, one per
   merged milestone, paired with the PR that delivered it. This is the build
   story: read a sprint doc, then its commits, in order.
2. **The course** — `astraupskill/README.md` is the entry point:
   `01-CODEBASE-MAP` → `02-CONCEPTS` → `03-WORKED-CHANGE` →
   `04-TESTING-AND-DEBUGGING` → `05-PRACTICE` (exercises with solutions in
   `06`), then `07-TRACE-LAB`.
3. **The advanced track** — `astraupskill/advanced/` (contracts, state
   tracing, failure labs), then the 22-chapter
   `astraupskill/advanced/extended-course/` when you want depth per domain
   concept (reservations, time rules, SQLite boundaries, fault injection).
4. **The judgement docs** — `OEReadMe.md` (what complexity earns its place),
   `TESTREADME.md` (why each test layer exists),
   `docs/quality/BUG-HARVEST.md`, `docs/security/THREAT-MODEL.md`, and
   `docs/curriculum/RETROSPECTIVE.md`.

## The per-sprint ritual

For each sprint:

1. Read the milestone and issues before opening code.
2. Read commits in order: model, service/storage, API, tests, documentation.
3. Run the focused test and change one rule to see a useful failure.
4. Follow one request from JSON through the service into the model and back.
5. Explain the code in plain language before adding an abstraction.
6. Read review comments and state what you would change differently.

Ask: what fact must remain true, which class owns it, and what is the
simplest test that proves it?

## Running the proof

```powershell
dotnet restore BorrowBox.slnx
dotnet test BorrowBox.slnx --configuration Release --no-restore
```

`astraupskill/VERIFICATION.md` records the authoritative counts (29 tests:
6 Core, 19 Infrastructure, 4 API, with TRX evidence beside it) **and the
limits** — what the competition test does and does not establish. Read that
file before trusting any claim in the course, and re-run the suite before
trusting the file. Pick the cheapest test layer that can prove a behaviour
(`TESTREADME.md` has the layer table); step 3 of the ritual means, concretely:

```powershell
dotnet test tests/BorrowBox.Infrastructure.Tests/BorrowBox.Infrastructure.Tests.csproj --configuration Release --no-restore --filter "FullyQualifiedName~Reservations|FullyQualifiedName~Loans"
```

then change one guard in `src/BorrowBox.Core` and watch which named
behaviour fails.

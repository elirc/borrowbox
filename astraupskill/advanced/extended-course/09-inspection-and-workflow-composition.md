# 09. Inspection and workflow composition

Inspection is where the workbook's earlier validation lesson meets a different part of the actual application. Reservation and checkout construct fallible candidates before their equipment effects. InspectionService currently does not always use that ordering. This chapter examines the difference without changing the application, then asks you to design a compatible repair in a disposable copy.

Read [Inspection and InspectionService](../../../src/BorrowBox.Core/Inspections/Inspection.cs), [the inspection store](../../../src/BorrowBox.Infrastructure/Inspections/InMemoryInspectionStore.cs), and [inspection tests](../../../tests/BorrowBox.Infrastructure.Tests/Inspections/InspectionTests.cs). The defects discussed here are concrete current-behavior observations. Proposed repairs, new guards, and additional lifecycle policies remain exercises rather than claims about shipped functionality.

## Opening an inspection changes equipment first

ReportDamageAsync loads equipment or throws equipment.not_found. It immediately calls equipment.MarkForInspection. Only afterward does it construct Inspection with a new ID, the equipment ID, the supplied damage description, and DateTimeOffset.UtcNow. It then saves equipment and adds the inspection. The constructor rejects blank damage and trims valid descriptions.

That ordering creates a reproducible validation side effect. Begin with Available equipment and submit a blank damage description. MarkForInspection changes status to Inspection. The Inspection constructor then throws inspection.damage_required. No inspection is added, yet the equipment remains in Inspection through the default live reference. This differs from the corrected reservation ordering studied earlier.

An existing happy-path test does not expose this case. It reports nonblank damage, completes with each supported outcome, and checks equipment status. Those assertions establish useful behavior for valid input. They do not establish that invalid damage preserves equipment. A new characterization observation can make the gap visible without pretending the original test promised more than it did.

## Inspection has its own local invariants

Inspection requires nonempty IDs and a nonblank damage description. It stores OpenedAt in UTC. IsOpen is derived from CompletedAt being null. Complete rejects an already closed inspection and blank resolution notes before setting Outcome, trimmed ResolutionNotes, and CompletedAt. It does not compare completedAt to OpenedAt, and it does not validate that an enum value is one of the named outcomes.

These omissions matter when designing tests. A direct call can complete with a timestamp earlier than opening because no temporal guard prohibits it. A cast integer outside the named InspectionOutcome values can be assigned to Outcome through the method. Whether an HTTP binder admits a particular representation is a separate API question; the domain method itself accepts the supplied enum value without a membership check.

Do not assume that every object with an opened and completed timestamp enforces chronological order. Loan.Return explicitly checks its analogous ordering. Inspection.Complete does not. The comparison is useful precisely because it is grounded in two actual methods. A proposed common temporal policy needs explicit implementation and test coverage in each relevant workflow.

## Outcome and equipment transition are separate steps

CompleteAsync loads inspection and equipment, then calls inspection.Complete before applying equipment transitions. Passed invokes PassInspection; Retired invokes RetireAfterInspection; Repair invokes neither of those methods. Equipment is saved afterward. With normal setup, Passed makes equipment Available, Retired makes it Retired, and Repair leaves it in Inspection while closing the inspection record.

The Repair outcome is worth reading carefully. A closed inspection plus equipment remaining in Inspection is not inherently an implementation error; it can mean the inspection decision identified repair work. However, the current code does not create a repair job, open a follow-up inspection, or track a repair lifecycle. If a product requires those features, they are additional workflows. Do not infer them from the enum name alone.

The existing theory covers the three named outcomes after ReportDamageAsync has put equipment into Inspection. It does not test completion when equipment has changed to another state. That mismatch case reveals another effect-order boundary: inspection.Complete can close the record before PassInspection or RetireAfterInspection throws because the equipment is not in Inspection.

## Worked mismatch trace

Construct an open inspection I referring to equipment E, but make E Available. This inconsistent setup is deliberate: it represents imported data, a prior partial operation, or another transition. Request Passed with nonblank notes. Inspection.Complete sets I.Outcome, notes, and CompletedAt. Equipment.PassInspection then throws equipment.not_in_inspection. No equipment save follows, but the live inspection object is already closed.

Now retry the same completion. The inspection's Complete method rejects inspection.closed before any equipment transition. The first request failed from the caller's perspective, but it consumed the inspection's open state. A retry cannot be interpreted simply as repeating an unperformed operation. This is another reason to test postconditions after exceptions and to distinguish a domain error from a clean rejection before all effects.

A proposed repair could validate the equipment transition before mutating Inspection. Merely calling PassInspection first moves the side-effect risk to equipment if later completion validation fails. The better local ordering is to validate all predictable preconditions before either mutation, or to introduce a domain operation that can validate the combined transition before applying it. Persistence and concurrency still require their own commit boundary.

## A compatible disposable repair

For blank damage, the smallest compatible ordering repair is to construct the Inspection candidate before MarkForInspection. This preserves the public method signature and the current successful result while preventing the known constructor rejection from changing equipment. Test both invalid damage and a Retired item so that you understand the resulting guard precedence. Do not edit the original project as part of this workbook exercise.

For completion, a careful experiment might add a validation method or a combined decision object inside the copied domain source. The validation should check open status, notes, named outcome policy if adopted, chronology if adopted, and the necessary equipment state for the selected outcome before mutations. Clearly identify which checks preserve existing rules and which introduce proposed rules. A repair can accidentally change product behavior if those categories are mixed.

Do not validate by performing the mutation on the real object and then rolling it back. That exposes intermediate state and creates another failure path. A pure decision step can return a planned transition or simply reject before effects, but it must still be protected against concurrent changes between decision and application if the final design promises concurrent correctness.

## Time injection is a separate improvement

InspectionService reads DateTimeOffset.UtcNow directly when opening and completing. Unlike checkout and reservation services, its constructor does not accept IClock. This makes precise service-level temporal tests harder. You can still test Inspection directly with fixed timestamps because its constructor and Complete method accept explicit instants.

A proposed service change can inject IClock while preserving an optional default for compatible callers, or require explicit composition changes. Choose deliberately. Adding optional dependencies can ease an exercise but may hide forgotten dependency registration in a larger application. The objective is not to enforce one style; it is to make the chosen clock source observable and testable without changing the machine clock.

This clock improvement does not fix the mutation ordering by itself. A deterministic invalid operation can still leave state behind. Keep testability improvements and behavior repairs separate in your change explanation so that a reviewer can assess each effect and its evidence independently.

## Guided lab: two failure observations

Use the new [inspection observation lab](lab/README.md) in a fresh disposable output. It extends the source-copy approach without changing application files. Predict the blank-damage and mismatched-completion states before running. The checks are labeled observations because they are expected to detect current limitations, not desired production acceptance criteria.

After recording the baseline, implement the blank-damage ordering repair only in your generated copy. Change its characterization assertion into a proposed regression assertion expecting Available equipment. Explain the name change and why a passing limitation observation should not be retained as a desired contract after the limitation is fixed. Then attempt the completion repair with separate tests for invalid notes and mismatched equipment.

## Independent exercises

**E09.1: Four-stage ledger.** For valid report, blank damage, failed equipment save, and failed inspection add, record equipment status and inspection existence after each stage. Distinguish local candidate construction from store insertion and state which adapter ownership assumptions your ledger uses.

**E09.2: Repair outcome policy.** Specify what should happen after an inspection concludes Repair. Decide whether the item remains unavailable, whether another work record is required, and how completion of repair becomes observable. Design the feature without claiming that a closed inspection already tracks the repair job.

**E09.3: Completion preconditions.** Propose a validation plan for a Passed result with an early timestamp, blank notes, and mismatched equipment. Separate current checks from new policy. Explain how your plan avoids both inspection-first and equipment-first partial validation effects, and what remains unsolved under concurrent changes.

## Review checkpoint

A strong answer does not merely say "validate earlier." It identifies which validations can be pure, which state they depend on, when they are observed, and which effects follow. It also recognizes that a local ordering repair can be valuable even though it does not establish durable atomicity. Inspection gives you a second concrete workflow to test whether you can apply that reasoning without copying a previous answer mechanically.

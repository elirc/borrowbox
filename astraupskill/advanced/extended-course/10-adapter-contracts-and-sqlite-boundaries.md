# 10. Adapter contracts and SQLite boundaries

An interface lets a service call different implementations through the same method names. It does not guarantee that those implementations have identical ownership, uniqueness, durability, or concurrency behavior. BorrowBox's equipment adapters provide an excellent comparison because both implement IEquipmentStore while making several materially different choices.

Read [IEquipmentStore](../../../src/BorrowBox.Core/Equipment/Equipment.cs), [InMemoryEquipmentStore](../../../src/BorrowBox.Infrastructure/Equipment/InMemoryEquipmentStore.cs), [SqliteEquipmentStore](../../../src/BorrowBox.Infrastructure/Equipment/SqliteEquipmentStore.cs), [BorrowBoxDbContext](../../../src/BorrowBox.Infrastructure/Data/BorrowBoxDbContext.cs), and [the SQLite adapter test](../../../tests/BorrowBox.Infrastructure.Tests/Equipment/SqliteEquipmentStoreTests.cs). This chapter produces a contract suite design, not an instruction to replace the default adapter registration.

## Separate declared shape from behavioral contract

IEquipmentStore declares AddAsync, GetAsync, ListAsync, SaveAsync, and TryCheckOutAsync. The signatures expose cancellation tokens and nullable lookup/claim results. They do not explicitly state whether returned objects are live, whether duplicate asset tags are accepted, whether SaveAsync can insert, or whether collection order is stable. Services and tests can accidentally depend on those unstated behaviors.

Start a contract table with three columns: required by the application, guaranteed by a particular adapter, and currently unspecified. For example, GetAsync returning null for a missing ID is visible in both adapters. Live-reference identity is specific to the in-memory implementation. Asset-tag uniqueness is enforced by the SQLite model's unique index but not by the in-memory dictionary, which is keyed by equipment ID. These differences should be explicit before a deployment switches adapters.

An adapter mismatch is not automatically a defect in both implementations. The interface may need a stronger contract, or the application may intentionally accept different teaching capabilities. The engineering task is to identify which differences affect business behavior and choose a documented boundary. Tests should enforce the chosen common contract without pretending that every implementation detail must be identical.

## Round trips reconstruct domain state

The SQLite adapter maps EquipmentItem to EquipmentRow for AddAsync. GetAsync and ListAsync use AsNoTracking and reconstruct new EquipmentItem instances. Reconstruction passes the stored ID, tag, name, category, and parsed status to the constructor. The constructor therefore normalizes strings again and validates required fields during loading.

This has useful and risky consequences. Invalid persisted strings can cause a load-time domain exception rather than silently creating an invalid object. An unparseable status can fail during Enum.Parse. However, the constructor is not a schema migration mechanism: it cannot invent a meaning for a removed enum name or reconcile corrupt relationships. A durable design needs an explicit compatibility policy for stored representations.

The status is stored as the enum's name string. Renaming a code symbol can therefore become a data compatibility change. A proposed migration must translate existing rows or preserve a stable storage representation. Merely updating C# references and seeing compilation succeed does not prove old data remains readable. This is a concrete example of why a storage contract outlives a convenient implementation name.

## Save is not the same as add

SqliteEquipmentStore.SaveAsync loads a row using SingleAsync for the item's ID, changes Name, Category, and Status, and calls SaveChangesAsync. It does not update AssetTag, consistent with the domain property being immutable after construction. If no row exists, this path does not silently insert a new one. The in-memory SaveAsync assigns the dictionary entry and can create or replace that key.

That difference affects a test that calls SaveAsync on a never-added item. It succeeds as an insertion-like assignment in memory but fails in the SQLite path. Decide whether such a call is allowed by the application contract. If it is forbidden, both adapters could enforce the precondition or tests could avoid relying on unspecified behavior. If an upsert is required, name and define it explicitly rather than making Save mean different things accidentally.

AddAsync also differs on identity conflicts. The in-memory assignment replaces an existing value with the same GUID. SQLite tracks an added row and relies on database constraints during saving. The appropriate domain response to duplicate identity or duplicate asset tag is not currently expressed as one common rule in IEquipmentStore. A production-facing service may need to translate expected constraint conflicts into a stable application result.

## Unique index evidence is narrow but real

BorrowBoxDbContext defines a unique index on AssetTag. This is stronger evidence for SQLite-backed uniqueness than constructor normalization alone. To test the contract, create two items with different GUIDs whose input tags normalize to the same stored tag, then attempt both inserts through the actual adapter. Observe the database constraint failure and verify the first row remains intact.

Do not claim that a HasMaxLength configuration automatically proves every database provider rejects overlong strings in the same way. The model records intended limits, but enforcement depends on provider behavior and schema. If length enforcement matters, use a domain validation rule or a provider-specific test establishing the actual result. This is a place where a model annotation should not be promoted into a universal storage guarantee.

The in-memory adapter does not share the unique-index enforcement. A business workflow tested only there can accept duplicates that a SQLite deployment later rejects. That rejection might occur after another side effect if the service crosses multiple stores. Adapter substitution therefore has workflow consequences beyond performance or restart durability.

## Conditional update and ordinary save differ

TryCheckOutAsync in SQLite filters the update by ID and Available status. SaveAsync writes the supplied status after loading the row, without an expected version or expected old status in its update contract. The conditional checkout operation expresses a narrower concurrency decision than a general save of a detached object's state.

Consider two detached reads of the same item. One caller changes details while another changes lifecycle state. If each later saves a full set of mutable fields from its stale object, one save can overwrite fields changed by the other unless the storage layer detects a conflict. The current SaveAsync assigns Name, Category, and Status together and the row model has no explicit concurrency token. This motivates a proposed version check or more narrowly scoped commands.

The chapter does not assert a specific race result under every database schedule. It identifies the fields written and the absence of an explicit expected-version contract in this code. A deterministic integration experiment would need separate contexts, controlled read/write order, and final verification through a fresh context. That is stronger evidence than two unsynchronized tasks that sometimes happen to overlap.

## A useful adapter contract suite

Design common cases for adding a valid item, looking up missing and present IDs, saving changed details, listing stored items, and losing a checkout claim when the item is unavailable. Add implementation-specific cases for in-memory alias behavior and SQLite uniqueness, reconstruction, and context round trips. Keeping these groups separate prevents a common suite from requiring live references in every adapter.

Every persistence assertion should state the observation boundary. A same-object assertion checks mutation. A new lookup through the same context may still be influenced by tracking in some designs. A separate context checks a stronger round trip. A new process with a file-backed database checks another boundary. Choose the smallest boundary that distinguishes the defect you are targeting.

The existing SQLite test uses one open in-memory connection across two contexts. It successfully demonstrates saved values reloaded across contexts. It does not use a disk database or restart the process. Preserve that precise description when citing it in a review. A test can be valuable without being the strongest conceivable persistence test.

## Guided investigation: a substitution checklist

Before proposing SQLite as the default, list every store used by lending workflows. Only equipment has this SQLite adapter in the current inspected path. Switching equipment alone leaves borrowers, loans, reservations, and inspections in memory unless additional work is done. A durable equipment row does not make a durable lending system; after restart, equipment state could remain while related in-memory records disappear.

Write a migration plan that first defines which records become durable together. Include schema creation, existing data interpretation, dependency lifetimes, exception mapping, and a test proving the intended restart boundary. Do not execute the plan in the original project for this exercise. The deliverable is a reviewable proposal identifying the missing components before changing one registration line.

## Independent exercises

**E10.1: Contract table.** Compare both adapters on object identity, missing lookup, save of a missing item, duplicate ID, duplicate normalized asset tag, and checkout contention. Mark each row common, adapter-specific, or unspecified at the interface level. Cite the actual implementing statement.

**E10.2: Stale save experiment.** Design a two-context sequence that can expose an overwritten status or detail field. Specify read order, local mutations, save order, and a fresh final observation. Propose a version-based alternative and the expected conflict result without claiming the current model already has it.

**E10.3: Storage evolution.** Plan a rename of one equipment status while preserving existing stored rows. Include old-reader/new-writer compatibility, migration validation, and rollback implications. Explain why a C# enum rename is more than a compilation concern when names are persisted.

## Review checkpoint

Your contract suite should help a future adapter author understand what must be preserved and what can differ. Your substitution plan should identify the whole lending relationship rather than treating a database-backed equipment table as a complete persistence solution. Those two artifacts make the interface a deliberate engineering boundary instead of a shared list of method signatures.

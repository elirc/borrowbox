# 4. Testing and debugging

Use a fixed clock for time boundaries. A `FixedClock` record implementing `IClock` is enough:

```csharp
private sealed record FixedClock(DateTimeOffset UtcNow) : IClock;
```

Place it in the test class or shared test fixture. The production container still resolves `SystemClock`. Tests should set the clock before constructing services, then pass expiry or due dates relative to that fixed value. This makes before, equal, and after cases deterministic.

For invalid reservation, seed Available equipment and an active borrower, call `ReserveAsync` with an expiry before the fixed clock, and compare status and reservation count. For invalid checkout, seed Available equipment and borrower, call with a due date before the fixed day, and compare status and active loans. For return-before-checkout, seed CheckedOut equipment and an active loan, call with `checkedOutAt.AddMinutes(-1)`, and assert `loan.IsActive`, `ReturnedAt == null`, and CheckedOut equipment.

Test equality explicitly. `expiresAt == clock.UtcNow` is invalid because the constructor requires strictly after reservation time. `dueOn == checkout date` is valid because the loan constructor rejects only dates before checkout. Expiry uses `ExpiresAt <= now`, so equality expires. The existing `ReservationExpiryTests.ExpireDue_UsesInjectedUtcClock` demonstrates a future fixed time; add equal-time coverage when extending the suite.

Repeated operations should fail before a second mutation. `Reservation.Cancel` and `Expire` require Active. `Loan.Return` rejects a second call. A mismatched equipment transition should be tested separately: an active reservation paired with Available equipment cannot be cancelled by the service, and an active loan paired with Available equipment cannot be returned. Compare both entities after each rejection.

When debugging a failure, inspect the first mutating call in the source. If it appears before candidate construction, move construction earlier. If a test uses `DateTimeOffset.UtcNow`, replace it with the fixed clock. If a test passes while the list is empty, assert a known entity exists before using `First`. If a concurrency test creates different service instances, verify they share the same store and any proposed coordinator. The existing store lock protects only one equipment item.

These tests run against in-memory references. They prove domain and service ordering, not a disk transaction. A cancellation after equipment `SaveAsync` but before reservation persistence is outside the current contract. Keep that limitation in evidence and avoid adding a claim that “the lock rolled back” the write.

For the positive path, assert more than “no exception.” A successful reservation should be Active, point to the requested equipment and borrower, and leave equipment Reserved. A successful checkout should be Active, point to the same IDs, and leave equipment CheckedOut. A successful return should make `IsActive` false and equipment Available. Positive assertions ensure a repair did not accidentally reject every operation while making rejection tests pass.

For cancellation, compare both statuses around a deliberately mismatched store state. An active reservation paired with Available equipment should throw `equipment.not_reserved` before `reservation.Status` changes. For return, an active loan paired with Available equipment should throw `equipment.not_checked_out` before `ReturnedAt` changes. These cases are stronger than calling domain methods alone because they verify service ordering across two live references.

The API layer has its own finite boundary. The exception handler maps `BorrowBoxException` to Problem Details, while authentication and authorization run before the endpoint. A future HTTP test should bind to loopback, await readiness with a deadline, set a request timeout longer than startup latency, and kill the process in `finally`. This project’s focused changes can be verified without a listener, so avoid an unbounded background-worker test.

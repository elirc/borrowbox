# 7. Trace lab

Trace a reservation request through the real API:

```http
POST /api/v1/reservations
X-Demo-Role: Student
Content-Type: application/json

{"equipmentId":"<equipment-guid>","borrowerId":"<borrower-guid>","expiresAt":"2026-01-02T09:00:00Z"}
```

The endpoint binds `CreateReservationRequest` and requires an authenticated demo identity; both Student and Staff satisfy this endpoint's default policy. `ReservationService` loads equipment and borrower, checks borrower activity, obtains `IClock.UtcNow`, and constructs `Reservation`. With the fixed example clock at 10:00, the constructor throws `reservation.expiry_invalid` before `EquipmentItem.Reserve`. `BorrowBoxExceptionHandler` converts the exception to Problem Details. Equipment remains Available and the reservation store has no entry. A valid expiry constructs first, then reserves, saves equipment, and adds the reservation.

Trace checkout similarly. `POST /api/v1/loans` binds `CheckOutRequest`; the service checks references and borrower limit, captures one clock value, constructs `Loan`, and calls the equipment store’s locked `TryCheckOutAsync`. If the item is unavailable, no loan is added. If the due date is invalid, equipment was never checked out. A successful result returns a Created response with the loan DTO.

Trace return. The endpoint supplies only the loan ID; `ReturnService` uses its injected clock overload. It loads the loan, rejects an already returned loan, then loads equipment, checks CheckedOut state, and calls `Loan.Return(now)`. A repeated return or a clock before checkout becomes a Problem Details error while both objects retain their prior state. A valid return sets `ReturnedAt`, calls equipment `Return`, and returns an active flag of false.

Trace expiry worker. `ReservationExpiryWorker` wakes once per minute, creates a scope, resolves `ReservationExpiryService`, and calls `ExpireDueAsync`. The service captures its clock once, lists reservations, selects Active entries with `ExpiresAt <= now`, and requires Reserved equipment. A missing or mismatched item is skipped; a valid pair transitions reservation to Expired, releases equipment, saves equipment, and increments the count. The worker logs only positive counts.

The trace stops at in-memory stores. There is no database transaction across equipment and reservation or loan records. `TryCheckOutAsync` locks one equipment item, but borrower-limit reads across distinct items are not coordinated. A future worker or command needs shared coordination and recovery semantics; this change documents that boundary without pretending to implement it.

Compare this trace with a rejected operation. In both reservation and checkout, the service performs lookups first because missing references have no entity to mutate. It then checks borrower activity and loan limits. The candidate constructor validates input before an equipment state transition; the equipment operation can still reject an unavailable item. This ordering means a malformed date produces a domain error without needing a compensating `ReleaseReservation` or `Return`. Compensation is still required for failures after persistence begins, which is why the durable boundary remains an open design concern.

These reservation and loan endpoints use the default authenticated-user policy. The named Staff policy is applied separately to selected administrative endpoints, such as equipment retirement. It does not identify the borrower as the current authenticated actor, and it does not provide tenant scoping. The request examples use placeholder GUIDs because seeded IDs are implementation details. A real endpoint would validate that the actor may borrow for the borrower ID, include an idempotency key, and return a conflict for a duplicate operation. Those requirements are deliberately outside this bounded lifecycle repair.

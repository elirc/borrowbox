# 11. DTO, endpoint, and error contracts

An API contract is the behavior a caller can observe across the request boundary. It includes routes, accepted fields, authorization, response shapes, status codes, and error details. It is related to the domain model but should not be inferred from it. BorrowBox demonstrates both explicit response records and direct serialization of domain objects, which makes the tradeoffs concrete.

Read [equipment endpoints](../../../src/BorrowBox.Api/Equipment/EquipmentEndpoints.cs), [loan endpoints](../../../src/BorrowBox.Api/Loans/LoanEndpoints.cs), [reservation endpoints](../../../src/BorrowBox.Api/Reservations/ReservationEndpoints.cs), [inspection endpoints](../../../src/BorrowBox.Api/Inspections/InspectionEndpoints.cs), and [the exception handler](../../../src/BorrowBox.Api/Errors/BorrowBoxExceptionHandler.cs). This chapter asks for a contract inventory and a feature design, not an unreviewed change to existing routes.

## A request record is a boundary shape

CheckOutRequest carries EquipmentId, BorrowerId, and DueOn. It does not carry CheckedOutAt; the service chooses that from its clock. The return endpoint accepts an ID in the route and calls the clock-based ReturnAsync overload rather than accepting a client-supplied return timestamp. This is a concrete boundary decision: callers cannot use that route to choose arbitrary return instants through the request body defined here.

CreateReservationRequest carries EquipmentId, BorrowerId, and ExpiresAt. The service supplies ReservedAt. Equipment creation accepts AssetTag, Name, and Category, while update accepts only Name and Category. These shapes limit which fields callers can propose through each operation. They also document distinctions such as immutable asset tags and server-selected observation times.

Domain constructors still validate business rules after binding. A syntactically valid JSON GUID can identify a nonexistent record. A valid date string can violate the due-date rule. Conversely, malformed input can fail before the service runs. A contract test should distinguish binding failure, domain rejection, authorization rejection, and infrastructure failure rather than expecting all invalid requests to reach one exception handler.

## A response is a chosen projection

LoanResponse includes IDs, CheckedOutAt, DueOn, and IsActive. It does not include ReturnedAt. The return endpoint therefore reports that the loan is inactive without exposing the accepted return timestamp in this response shape. If a receipt feature needs that timestamp, adding it is an API design decision with compatibility and test implications.

ReservationResponse exposes status as a string generated with ToString. EquipmentResponse does the same for equipment status. This avoids serializing the entire domain instance, but it still ties the visible string to enum naming unless a stable mapping is introduced. A future enum rename can affect both storage and response contracts; those consumers may have different migration needs.

Inspection endpoints return Inspection directly. Its public properties therefore become part of the serialized shape under the application's serialization configuration. Adding a public domain property later can affect the response even if the endpoint code is unchanged. A proposed InspectionResponse can make that projection explicit, but the change should be reviewed against current clients and tests rather than assumed harmless.

## Created locations need a route audit

Checkout returns Created with a location formatted as /api/v1/loans/{id}. Reservation creation similarly emits /api/v1/reservations/{id}, and inspection creation emits an inspection location. In the endpoint files examined here, creation and action routes exist, but corresponding item GET routes are not mapped there. A location string is not proof that the API supports fetching that resource at that address.

The exercise is to audit the complete route map before promising navigability. If no matching GET exists elsewhere, a proposed resource-read feature should define its response and authorization. Alternatively, the contract can document that the location is an identifier URI without a supported read operation, though that may be less convenient for clients. The important point is to test the caller's intended next action rather than treating a formatted URI as an implemented endpoint.

Equipment has both collection and item GET mappings. Its creation location can therefore be checked against a visible read route. This contrast provides a useful small contract test: create an item, inspect the Location header, and follow it with the permissions required by the read route. Do not extrapolate that successful equipment behavior to every other resource.

## Error mapping is currently broad

BorrowBoxExceptionHandler handles BorrowBoxException and sets status 422. Its problem details include a title, the exception message, the rule code, and the request TraceIdentifier. It returns false for other exception types, leaving their handling to the wider middleware behavior. This handler does not map each domain code to a different HTTP status.

That means a service-thrown equipment.not_found can become a 422 domain response, while an equipment GET endpoint directly returns NotFound when its service lookup yields null. The same English idea of missing equipment can appear through different response paths. A client should follow the documented operation contract rather than assume all missing-record situations produce the same status automatically.

A proposed error taxonomy could separate not found, conflict, validation, and infrastructure failure. Before implementing it, inventory existing clients and tests that rely on 422 and the code extension. Preserve stable machine-readable codes where useful. Human-readable messages can explain the event, but clients should not parse sentences to determine whether a retry is appropriate.

## Worked client trace after a partial failure

Imagine checkout changes equipment and then loan insertion throws. The domain-specific handler does not handle the injected infrastructure exception because it is not BorrowBoxException. The caller receives a failure according to the surrounding error middleware, but the exact response body is not established by this handler alone. The equipment side effect can remain regardless of how friendly the transport error becomes.

The client retries the same request. The service may now reject equipment.not_available, which is a BorrowBoxException handled as 422. Without a receipt contract, the client sees two failures with different categories and cannot tell whether the first request owns the current equipment state. This is why a consistent error envelope is helpful but insufficient for recovery. Outcome identity must be designed alongside error presentation.

For your API proposal, include a queryable operation result or another explicit resolution path. Do not suggest that the client repeatedly retries every 422 until success. Some rule failures are stable, some represent changing business state, and some may reflect a previous ambiguous operation. Retry policy belongs to the operation semantics, not merely to the status-code family.

## Mapping time and state deliberately

DTO mapping can provide a stable snapshot of selected values at mapping time, but it does not create a transaction around the underlying reads. Equipment search obtains live domain references from the in-memory store, then maps them. Concurrent mutation can affect which values are observed before projection. A response record is immutable as a record value, yet its source observations may still span changing state.

For a single loan response, IDs and dates are immutable properties while IsActive derives from a mutable return timestamp. If another operation changes the loan before mapping reads IsActive, the response can reflect that later state. Whether this is acceptable depends on the desired operation receipt semantics. A proposed completed checkout receipt may need to represent the committed checkout event rather than the entity's current lifecycle state at an arbitrary later read.

This distinction helps explain why event receipts and resource representations are often different designs. A receipt answers what this operation did. A resource response answers what the resource currently looks like under a stated consistency model. BorrowBox currently returns projections of the entity after service completion; richer receipt semantics are a future feature.

## Guided investigation: build the inventory

For each inspected endpoint, record method, route, request fields, service call, authorization requirement, success response, and error path. Include the absence of ReturnedAt from LoanResponse and the direct Inspection serialization. Then select one consumer story: a student checks out equipment and later needs to verify return. Identify which current response fields and routes support that story and which are missing.

Design a proposed GET loan endpoint only on paper or in a disposable copy. Specify who may read it, how the caller's identity relates to BorrowerId, what happens for missing IDs, and whether ReturnedAt is included. A route implementation that merely returns the store object would leave important privacy and ownership questions unanswered.

## Independent exercises

**E11.1: Contract matrix.** Compare equipment lookup, checkout, return, reservation creation, and inspection completion. Include success status, response fields, and how a missing related entity reaches the response. Mark framework-binding outcomes as requiring separate request tests if you have not executed them.

**E11.2: Inspection projection.** Propose an explicit inspection response record and migration strategy. Decide which properties belong in the public contract and how future domain additions remain private by default. Give one test that catches an accidental response-field addition and one that catches an omitted required value.

**E11.3: Read-after-create story.** Audit one Created location against mapped routes. Design the missing caller journey if necessary, including authorization and a stable response. Explain why a valid-looking URI and a passing creation test do not establish a usable follow-up read.

## Review checkpoint

Your inventory should let a frontend developer build a client without reading domain classes. Your proposal should distinguish resource state, operation receipt, and error information. Those distinctions prevent a convenient object serialization from becoming an accidental long-term contract and make later authorization review much more concrete.

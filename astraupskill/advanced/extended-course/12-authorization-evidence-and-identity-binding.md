# 12. Authorization evidence and identity binding

BorrowBox includes a deliberately simple demo authentication scheme. Understanding it accurately is part of learning security boundaries: a role header can exercise authorization behavior in a teaching environment without establishing a trustworthy production identity. This chapter asks you to read the actual policy wiring, identify what the tests prove, and design a stronger identity-binding proposal without claiming it already exists.

Read [DemoHeaderAuthenticationHandler](../../../src/BorrowBox.Api/Security/DemoHeaderAuthenticationHandler.cs), [Program](../../../src/BorrowBox.Api/Program.cs), [authorization tests](../../../tests/BorrowBox.Api.Tests/Security/AuthorizationTests.cs), and the endpoint files from chapter eleven. All experiments should use invented data and the local teaching host. Do not apply these headers to unrelated services or treat them as real account credentials.

## What the demo handler actually trusts

The handler looks for X-Demo-Role. If the header is absent, it returns no authentication result. If its value is neither Student nor Staff, it returns a failed authentication result. For either accepted value, it creates a principal with name demo-user and the supplied role, using the DemoHeader scheme. It does not verify a password, signed token, external issuer, or student enrollment record.

The consequence is straightforward: a caller who can send the accepted header can choose the demo role in this teaching configuration. This is intentional scaffolding for exercising policy behavior, not evidence that the caller is a real staff member. A production identity proposal must replace that trust mechanism and define how claims are authenticated, validated, and mapped to application permissions.

Do not overstate the demo's purpose in either direction. It is useful for deterministic endpoint tests and learning the distinction between unauthenticated and insufficiently authorized requests. It is insufficient for a hostile-client deployment. Both statements can be true, and stating the boundary clearly is better than treating every passing authorization test as a complete security assessment.

## Policy placement is visible in routes

Program configures a StaffOnly policy requiring the Staff role. Equipment creation, update, and retirement require that policy. Inspection reporting and completion also require it. Equipment reads are mapped without those authorization requirements in the inspected endpoint file. Loan and reservation commands call RequireAuthorization without naming StaffOnly, so they require an authenticated principal under the configured policy behavior rather than the specific staff role.

This route inventory establishes where authorization is requested. It does not establish borrower ownership checks. Checkout accepts a BorrowerId supplied in the request, and the service verifies that borrower exists and is active. The service does not receive the request principal or compare its identity to the borrower. Reservation creation has the same separation. A valid demo Student principal is not automatically bound to a particular Borrower record.

Therefore, the statement "students can only borrow for themselves" is unsupported by these paths. If that is the desired product rule, it needs an identity-to-borrower relationship and an enforcement point. A staff-on-behalf-of-student workflow may need a different permission. Do not infer either rule from the role name or from the presence of a BorrowerId field.

## What the existing tests establish

The authorization tests send DELETE to a generated equipment ID. With no identity header, the response is Unauthorized. With Student role, it is Forbidden. These tests are useful because the generated ID need not refer to real equipment: authorization rejects before the retirement business operation can determine whether the item exists.

They establish two negative cases for that route under the test host. They do not show that Staff succeeds on a valid item, that every write route is protected, that borrower ownership is enforced, or that the identity mechanism resists forgery. A complete claim ledger should list those gaps rather than use the plural phrase "authorization tests pass" as a substitute for a policy matrix.

A positive test should create or seed suitable equipment and authenticate as Staff, then verify both response and state. A policy-coverage test should enumerate important write routes and their required roles. A borrower-ownership test requires the proposed identity mapping to exist first. Each test addresses a separate obligation, and a failure should tell you which boundary is wrong.

## Worked scenario: role versus resource ownership

Imagine demo Student principal P submits checkout for borrower B, whose GUID came from a fixture. The route requires authentication, so P satisfies that route-level gate. CheckoutService loads B, verifies B is active, checks B's loan count, and proceeds according to equipment availability and due-date rules. No step in that sequence demonstrates that P represents B.

Now propose a production policy: students may check out only for their own borrower record, while staff may check out for any active borrower at the desk. The policy requires a verified subject identifier on P, a reliable mapping to a borrower, and an authorization decision over both actor and target. Simply changing RequireAuthorization to StaffOnly would remove the student feature rather than implement self-service ownership.

A useful boundary design resolves the acting identity near the application layer and passes an explicit actor context or authorized command to the workflow. The exact architecture is open to discussion, but the decision must be reached by every relevant entry point. A background job or internal command should not bypass a business authorization rule merely because it does not use the HTTP endpoint.

## Avoid trusting client-selected authority

In a proposed production DTO, the client can supply the target borrower ID when the feature needs it, but it should not supply a trusted Staff flag or an actor ID that the server accepts without verification. Actor identity comes from the authenticated principal. Target identity comes from the requested operation and is then checked against the actor's permissions. Keeping those inputs separate prevents a request body from appointing its own authority.

The same distinction applies to audit records. Recording only BorrowerId says who the loan concerns, not who initiated it. A staff member acting on behalf of a borrower produces two meaningful identities. An audit proposal should record the verified actor and target separately, with an operation ID and relevant outcome, while avoiding unnecessary personal data in routine logs.

This is a design exercise, not an instruction to add broad surveillance. Choose fields because they answer specific operational questions: who requested the transition, which resource changed, what result was committed, and how to correlate a retry. Define access and retention according to the intended deployment rather than assuming every log consumer may see every borrower detail.

## Correlation is not authentication

Program accepts an X-Correlation-ID header when it is nonblank and at most one hundred characters; otherwise it uses TraceIdentifier. It includes the selected correlation value in a response header and logging scope. That helps connect observations across a request, but the value is caller-supplied in the accepted case. It should not be treated as proof of identity, uniqueness, or authority.

The problem-details handler includes TraceIdentifier, which is a different value from the selected correlation header when a caller supplies its own accepted value. A troubleshooting guide should explain which identifier appears where. Otherwise, a user may quote a correlation value while an operator searches only for the server trace value and incorrectly conclude that no request occurred.

A proposed logging improvement could include both fields with explicit names. It should also consider formatting and downstream log handling rather than assuming a length limit establishes every desirable property. This workbook does not claim a particular logging exploit; it teaches the narrower principle that untrusted correlation metadata remains untrusted even when useful.

## Guided investigation: evidence matrix

Build a matrix with anonymous, Student, and Staff as rows and the inspected write operations as columns. Fill only the policy-level expected gates supported by source. Then add a second matrix for actor-to-borrower relationships: self, another borrower, no mapping, and staff delegation. Mark every cell in the second matrix as current, proposed, or unknown based on actual enforcement.

Use the existing negative retirement tests as executed-project evidence only if you actually run them; otherwise cite them as source-inspected tests. This continuation does not require rerunning the full API suite. A test design that names the exact expected status and required setup is useful, but it must not be reported as a passing runtime result before execution.

## Independent exercises

**E12.1: Policy coverage.** Specify five request tests covering absent identity, invalid demo role, Student on staff-only retirement, Staff on valid retirement, and authenticated checkout for a mismatched borrower. For the last case, separate current expected behavior from the proposed self-service policy.

**E12.2: Actor binding.** Design the minimal trusted actor context for student self-service and staff delegation. Explain where it is created, how the borrower mapping is validated, and which service boundary enforces the permission. Include a missing-mapping case and a disabled-borrower case without confusing identity with eligibility.

**E12.3: Audit and correlation.** Propose an audit event for a completed staff-assisted checkout. Distinguish actor, target borrower, operation ID, server trace, and caller correlation. State which fields are authoritative and which are diagnostic hints, and identify one unnecessary personal field you would omit.

## Review checkpoint

Your security explanation should be specific enough to avoid both complacency and exaggeration. The demo scheme supports role-gate teaching. The current paths do not establish production identity or borrower ownership. A sound proposal closes those concrete gaps with verified identity and explicit policy, while retaining separate validation of borrower activity and lending capacity.

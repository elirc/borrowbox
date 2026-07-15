# API integration guide

Base path: `/api/v1`. JSON is the default representation; errors use `application/problem+json` with a stable `code` extension for business errors.

## Identity in this course

Send `X-Demo-Role: Student` for borrowing operations or `X-Demo-Role: Staff` for administrative operations. Replace this scheme in any deployment; it provides no identity assurance.

## Compatibility

- Additive response fields may appear within v1; clients should ignore unknown fields.
- Enum values are strings in API responses; handle unknown future values safely.
- Send a unique `X-Correlation-ID` of at most 100 characters and record the echoed response value.
- There is no idempotency-key support yet. Do not blindly retry mutating requests after an ambiguous timeout.

## Typical flow

1. Staff creates equipment and a borrower.
2. A student or staff identity creates a loan or reservation using returned IDs.
3. Staff completes returns and inspections.
4. Staff reads inventory summary or CSV export.

Use the Development OpenAPI document at `/openapi/v1.json` as the machine-readable contract. Contract changes require tests and changelog notes.

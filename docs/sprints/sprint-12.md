# Sprint 12: authentication and authorization concepts

## Goal

Distinguish identity from permission in an ASP.NET Core API.

- Authentication turns the teaching-only `X-Demo-Role` header into a user identity.
- Authorization requires any identity for borrowing and the `Staff` role for inventory changes and inspections.
- API tests distinguish `401 Unauthorized` (no identity) from `403 Forbidden` (known identity, insufficient permission).

## Security warning

The demo header is deliberately **not production authentication**: any caller can forge it. A real deployment must validate signed tokens or use an institutional identity provider. The policies and endpoint permissions can remain when the demo handler is replaced.

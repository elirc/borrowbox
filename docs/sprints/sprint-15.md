# Sprint 15: operations and v1.0

## Goal

Finish the feedback loop from code to operation: identify requests, distinguish liveness from readiness, document incident response, assess production gaps, and release from a green commit.

## Review route

1. Run all quality gates locally or in CI.
2. Call both health endpoints and inspect the correlation header.
3. Work through the duplicate-loan incident drill.
4. Compare `OEReadMe.md` with the production-readiness assessment.
5. Complete the release checklist and create `v1.0.0` only after main is green.

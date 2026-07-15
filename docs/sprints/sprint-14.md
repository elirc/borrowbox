# Sprint 14: bug harvest and hardening

## Goal

Turn three known risks into executable counterexamples, then fix them at the boundary that owns the guarantee.

Read the two sprint commits in order and use `docs/quality/BUG-HARVEST.md` as the review guide. The exercise demonstrates why a failing test is stronger evidence than a comment saying code might be unsafe.

## Definition of done

- A second return is rejected.
- Expiry uses an injected UTC clock.
- Exactly one of two competing checkouts succeeds.
- The threat model names current controls and honest follow-up work.

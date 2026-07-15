# Sprint 14 bug harvest

This sprint uses red-green-refactor on defects deliberately left visible in earlier sprints. Commit `1d3d3ee` adds the counterexamples; the next commit makes them pass.

## 1. Returning the same loan twice

**Counterexample:** call the return use case twice for one loan.

**Risk:** the second request silently changes the audit timestamp and claims success.

**Smallest fix:** `Loan.Return` rejects a loan that is no longer active. The rule belongs beside the state it protects.

## 2. Expiry reads the machine's local clock

**Counterexample:** inject a clock set in the future and observe that expiry ignores it.

**Risk:** behavior depends on machine configuration and tests cannot control time.

**Smallest fix:** depend on `IClock.UtcNow`; production uses `SystemClock`, while tests use a tiny fake.

## 3. Competing checkouts both succeed

**Counterexample:** let two requests read separate available snapshots before either saves.

**Risk:** two borrowers receive active loans for one item.

**Smallest fix:** move compare-and-change behind `TryCheckOutAsync`. The in-memory adapter locks the item; SQLite uses one conditional `UPDATE`. Atomicity belongs at the persistence boundary that can guarantee it.

## What we did not build

There is no distributed lock, event bus, generic transaction framework, or home-grown mocking library. Those would increase concepts without improving these concrete guarantees.

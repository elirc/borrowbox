# Incident drill: duplicate equipment loan

## Scenario

A staff member reports that one asset appears on two active loans after a deployment.

## Exercise

1. Declare an incident owner and record UTC start time.
2. Stop new checkouts only if evidence shows continued corruption; keep read access available.
3. Find requests by asset ID and correlation ID. Preserve logs and database evidence.
4. Identify the authoritative borrower with staff; do not silently delete history.
5. Reproduce with a deterministic competing-request test.
6. Repair the smallest owning boundary and add the regression test before deploying.
7. Verify health, checkout, return, and reporting after recovery.
8. Write a blameless timeline: detection, impact, contributing conditions, control gap, and follow-ups with owners.

Success means the team protects data, communicates clearly, and produces a verified prevention—not merely a fast restart.

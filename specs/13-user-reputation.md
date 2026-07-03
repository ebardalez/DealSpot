# 13 — User Reputation

**Phase:** 3 · **Status:** Not built (field + method exist) · **Depends on:** 04, 12

## Goal
Reward useful contributions. `User.ReputationScore` + `AddReputation(points)` already exist. Award/deduct on community events and gate privileges (auto-approve, submission rate).

## Point rules (tune later)
| Event | Δ |
|---|---|
| Submitted deal approved | +10 |
| Submitted deal rejected | −2 |
| Received upvote on your deal | +1 |
| Received downvote | −1 |
| Your deal auto-expired (community/UrlDead) | −3 |

## Changes

### Application (event handlers)
- `INotificationHandler<DealApprovedDomainEvent>` → +10 to `SubmittedByUserId`.
- Vote cast (04) → ±1 to the deal's submitter (skip self-votes).
- `DealExpiredDomainEvent` (reason `CommunityReported`/`UrlDead`) → −3 to submitter.
- All via `IUserRepository` + `User.AddReputation` (already clamps at ≥0).

### Domain
- `User.CanAutoApprove => ReputationScore >= 50` and `DailySubmissionLimit => score switch {…}` — consumed by 12's rate limit and to skip the queue for trusted users.

### Web
- Show reputation on profile / next to submitted deals.

## Acceptance
- Approved submission raises submitter reputation by 10; rejection lowers by 2.
- Self-votes don't change reputation.
- Score never goes below 0.
- User ≥50 rep auto-approves (bypasses queue in 12).

## Test
Reputation handlers: approve +10, reject −2, self-vote no-op, floor at 0.

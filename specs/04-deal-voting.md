# 04 — Deal Voting (Upvote / Downvote)

**Phase:** 2 · **Status:** Not built · **Depends on:** 01

## Goal
Signed-in users upvote/downvote deals. Net score orders the feed and feeds the velocity component of Deal Score (05). One vote per user per deal, changeable.

## Changes

### Domain
- `Deals/DealVote.cs` (`Entity`): `DealId`, `UserId`, `Value` (`+1`/`-1`), `CreatedAtUtc`. Factory `Create`.
- On `Deal`: `UpVoteCount`, `DownVoteCount`, `NetScore => Up - Down`; methods `ApplyVote(delta)` / `RemoveVote(delta)` maintaining counts (deltas applied by handler so counts stay denormalized on the aggregate for cheap sorting).
- `IDealVoteRepository`: `GetAsync(dealId, userId)`, `Add`, `Update`.
- `DealErrors.AlreadyVotedSame` (idempotent re-vote).

### Application
- `Deals/Vote/CastVoteCommand(DealId, Value)` — user from context:
  - no existing vote → add, `deal.ApplyVote(value)`.
  - existing, different → flip: adjust counts, update vote.
  - existing, same → no-op success.
- Include first-30-min vote timestamps needed by 05 (query in that spec).

### Infrastructure
- `DealVoteRepository`; `DealVoteConfiguration` unique `(deal_id, user_id)`; add vote columns to deal.
- Migration `AddDealVoting`.

### Web
- Vote widget on `DealCard` + `DealDetail` (`InteractiveServer`): up/down arrows, live net score, disabled/redirect for guests.
- Add `NetScore` to `DealSummaryDto`; offer feed sort "Hottest" (by net score) vs "Newest".

## Acceptance
- Second identical vote is a no-op (counts unchanged).
- Flipping up→down moves net by 2.
- Guests cannot vote.

## Test
`CastVoteCommandHandler`: new/flip/duplicate paths assert correct `Up`/`Down`/`Net`.

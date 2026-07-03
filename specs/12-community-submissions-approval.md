# 12 — Community Deal Submissions & Approval Queue

**Phase:** 3 · **Status:** Not built · **Depends on:** 01, 07

## Goal
Let registered users submit deals into a moderation queue. `DealStatus` already has `PendingApproval` and `Rejected`; `Deal.SubmittedByUserId` exists. Add public submit + admin approve/reject.

## Changes

### Domain
- On `Deal`: `Approve()` (`PendingApproval`→`Active`, raise `DealApprovedDomainEvent`), `Reject(reason)` (→`Rejected`). Guard illegal transitions → `DealErrors` (`NotPending`).
- Community-submitted deals start `PendingApproval`, not `Active`.

### Application
- `Deals/Submit/SubmitDealCommand` — same shape as `CreateDealCommand` but user from context, forced `PendingApproval`, still runs affiliate rewrite + slug. (Admin path 07 stays direct-`Active`.)
- `Deals/Approve/ApproveDealCommand(DealId)` + `Deals/Reject/RejectDealCommand(DealId, Reason)` — `[Admin]`.
- `Deals/GetPending/GetPendingDealsQuery` for the queue.
- Approval fires `DealCreatedDomainEvent`-equivalent so alerts (03) + cache evict (09) run **on approval**, not submission.
- Rate-limit submissions per user (reputation-gated, see 13).

### Infrastructure
- `IDealRepository.GetPendingAsync(tenantId, page, size)`; index `(tenant_id, status)` already covers it.

### Web
- `/submit` (`InteractiveServer`, `[Authorize]`): submission form → `SubmitDealCommand`; confirmation "pending review".
- `/admin/deals/pending`: queue with approve/reject + reason.

## Acceptance
- Community submit creates a `PendingApproval` deal, invisible on public feed.
- Approve makes it `Active` and triggers alerts + cache eviction; reject hides it with reason.
- Non-admin cannot approve/reject.

## Test
`ApproveDealCommandHandler`: pending→active + event; approving a non-pending deal → failure.

> Consider the Outbox pattern here (roadmap Phase 3) so approval-triggered emails survive a crash — supersedes the inline dispatch from 00 for this path.

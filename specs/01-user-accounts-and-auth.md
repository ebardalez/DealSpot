# 01 — User Accounts & Auth Wiring

**Phase:** 2 · **Status:** Partial (OAuth cookie works; no domain user) · **Depends on:** —

## Goal
Google OAuth is wired in `Program.cs` but sign-in produces only an Identity cookie — no domain `User` row, so watchlist/voting/reputation have nothing to attach to. Provision a domain `User` on first login, expose current-user context, and add login/logout UI.

## Current state
- `Program.cs`: `AddGoogle`, cookie scheme, `/auth/login/{provider}`, `/auth/logout` endpoints exist.
- `MainLayout` references auth nav.
- `User` entity + `UserErrors` exist. No `IUserRepository`, no provisioning, no `ICurrentUser`.

## Changes

### Domain
- `IUserRepository`: `GetByEmailAsync`, `GetByIdAsync`, `Add`.
- (Reuse existing `User.Create` / `UpdateProfile`.)

### Application
- `Abstractions/Authentication/ICurrentUser.cs`: `Guid? UserId`, `string? Email`, `bool IsAuthenticated`.
- `Users/ProvisionUser/ProvisionUserCommand(Email, DisplayName, AvatarUrl)` → returns `UserId`. Idempotent: return existing by email else `User.Create` + add.

### Infrastructure
- `UserRepository : IUserRepository`.
- `CurrentUser : ICurrentUser` reading `ClaimTypes` off `IHttpContextAccessor`.
- `UserConfiguration` — unique index on `email`.
- Migration `AddUser` (only entity added: table already declared in DbSet; verify columns).

### Web
- On OAuth callback (cookie `SignedIn` event or a `/auth/callback` handler), send `ProvisionUserCommand`, store resulting `UserId` as a claim (`sub` → app user id).
- `MainLayout`: authed → display name + avatar + "My Watchlist" + logout; guest → "Sign in with Google" (`/auth/login/Google`).

## Acceptance
- First Google login inserts one `app_users` row; second login reuses it (no duplicate).
- `ICurrentUser.UserId` resolves in handlers when authenticated, null otherwise.
- Site remains fully usable when signed out (no forced auth).

## Test
`ProvisionUserCommandHandler`: new email creates; existing email returns same id, no second `Add`.

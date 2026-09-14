# FlowBoard — Architecture Rules

> **For every developer (including future you) and every AI session.**
> This file explains *how* we build FlowBoard so that the code stays consistent, secure, and easy to review.
> It is based on `FlowBoard_System_Design.docx` and `FlowBoard_Tasks_Plan.docx`.
> **Read this + `Documents/TASK_LOG.md` before writing any code.**

**Applies to:** `Identity.Service`, `Project.Service`, `File.Service`, `Notification.Service`, `Gateway.YARP`, `frontend/flowboard-web` (Angular 22).

**Enforcement:** Code review blocks any violation. No exceptions.

---

## Table of Contents
1. [How to Use This File](#how-to-use-this-file)
2. [Layering — Clean Architecture](#1-layering--clean-architecture)
3. [Controller → Command → Service Flow](#2-controller--command--service-flow)
4. [Folder Structure](#3-folder-structure)
5. [Frontend Rules](#4-frontend--angular-22-standalone)
6. [Redis Caching](#5-redis-caching)
7. [YARP, EF Core & Other Backend Rules](#6-yarp-ef-core--other-backend-rules)
8. [Frontend 3-File Rule](#7-frontend-3-file-rule)
9. [Git & Session Rules](#8-git--session-rules)
10. [Permissions — Every New Feature Needs Them](#9-permissions--every-new-feature-needs-them)
11. [Human Error Messages — Never Expose Internals](#10-human-error-messages--never-expose-internals)

---

## How to Use This File

- **If you are a developer:** Read sections 1-4 before creating a new controller, command, or service.
- **If you are an AI session:** Read this file + `Documents/TASK_LOG.md` + `Documents/FlowBoard_Tasks_Plan.docx` in order. Do not assume a task is next — check `TASK_LOG.md` for the first `Pending` task.
- **If you are a reviewer:** Use the checklists at the end of sections 9 and 10.

We keep this file human-readable: each rule has *why* it exists, a *good vs bad* example, and a *check*.

---

## 1. Layering — Clean Architecture

**Why:** Keeps business logic testable and independent of frameworks. `Application` can be unit-tested without a database or Redis.

```
Api (Controllers)
  → Application (Interfaces + Commands/Queries + DTOs + Handlers)
  → Domain (Entities, Enums, BaseEntity)
  → Infrastructure (Implementations + Persistence + External Services)
  ← SharedKernel (BaseEntity, Result, DomainEvent)
  ← Shared.Contracts (Integration Events)
```

**Rule:** `Application` defines *interfaces*, `Infrastructure` implements them. `Application` never imports `Infrastructure`. `Api` only knows `Application` (via `IMediator` + `IApplicationDbContext`). `Domain` has no dependencies at all.

**Bad:** `using Infrastructure` inside `Application/Commands/MyHandler.cs`, `new ProjectDbContext()` in a controller, `static` calls to `JwtProvider`.

**Check:** `dotnet build` should not show `Application` referencing `Infrastructure`.

---

## 2. Controller → Command → Service Flow

**Why:** Controllers stay thin, business logic stays in services, and every operation is testable via `MediatR`.

**Flow:**

1. **Controller (`Api`)** — thin, only orchestration:
   - Injects `IMediator` and `IApplicationDbContext` (only for `GetUserId()` from claims).
   - No `_db.Users.Where(...).ToList()` business logic.
   - Calls `await _mediator.Send(new CreateProjectCommand(...))`.
   - Maps `Result<T>` to `201 / 200 / 403 / 400`.

2. **Command/Query (`Application`)** — `MediatR 12.4` + `FluentValidation 11.10`:
   - `record CreateProjectCommand(...) : IRequest<Result<ProjectDto>>`
   - `Validator : AbstractValidator<T>` (e.g., `Name` max 200, `Title` max 300).
   - `Handler : IRequestHandler<T, Result<T>>` injects *service interfaces* (`IProjectService`, `IJwtProvider`), not `DbContext` directly. Calls the service, returns `Result.Success` or `Result.Failure` (no `throw` for business errors).

3. **Service Interface (`Application/Interfaces`)** — e.g., `IProjectService`, `IJwtProvider`, `IRedisCacheService`. Methods return `DTOs` or `Result<T>`.

4. **Service Implementation (`Infrastructure/Services`)** — injects `IApplicationDbContext` + `IRedisCacheService` + `HttpClient`. Contains `EF Core` queries, `ActivityLog`/`Outbox` in same transaction, cache invalidation. Registered as `AddScoped<IProjectService, ProjectService>`.

**Good example (Auth):**
- `Api/Controllers/AuthController.cs` → `await _mediator.Send(new RegisterCommand(dto.Email, dto.Password, dto.FullName))`
- `Application/Commands/RegisterCommand.cs` handler injects `IJwtProvider, IPasswordHasher, IRefreshTokenService`
- `Infrastructure/Services/JwtProvider.cs : IJwtProvider`

**Bad example (to refactor):**
- `WorkspacesController.cs` with `_db.WorkspaceMembers.Where(...).ToListAsync()` → move to `IWorkspaceService` → `WorkspaceService.cs`.

---

## 3. Folder Structure

**Why:** A predictable layout lets any developer find code in 30 seconds.

```
backend/Services/{Service}/
  Api/Controllers/          <- thin controllers, IMediator only
  Application/
    Interfaces/             <- IApplicationDbContext, IRedisCacheService, IJwtProvider, DTOs
    Commands/               <- Records + Validators + Handlers (call services, no EF)
    Queries/                <- Records + Handlers (ICacheableRequest for caching)
    DTOs/                   <- ProjectDto, TaskDto (no EF navigation)
    Behaviors/              <- CachingBehavior<TRequest,TResponse>
    Caching/                <- CacheKeys, ICacheableRequest
  Domain/
    Entities/               <- Project, Board, TaskItem, SubTask, Comment, ActivityLog, etc. : BaseEntity
    Enums/                  <- TaskPriority (only)
  Infrastructure/
    Persistence/            <- DbContext : IApplicationDbContext, HasDefaultSchema("project"/"identity"), Migrations
    Services/               <- Implementations of Application interfaces
    Caching/                <- RedisCacheService : IRedisCacheService
  Program.cs                <- AddDbContext + AddMediatR + AddScoped + AddAuthentication + MapControllers
```

**Rule:** `Application/Services` must **not** exist. All implementations go to `Infrastructure/Services`. If you see `Application/Services`, move it and update `namespace` + `Program.cs`.

---

## 4. Frontend — Angular 22 Standalone

**Why:** Signals + TanStack Query is the 2026 MNC standard for .NET + Angular shops, not legacy `NgRx`.

- `input.required<T>()` + `computed()` + `ChangeDetectionStrategy.OnPush` + `inject(HttpClient)` + `firstValueFrom` + `injectQuery/injectMutation` (TanStack experimental 5.62)
- No `NgRx`, no internal CSS (`style: none` → Tailwind + DaisyUI 4.12.14 `src/styles.css` only, 6 themes `light/dark/corporate/cupcake/emerald/synthwave`)
- `core/services/project.service.ts` uses `inject(HttpClient)` + `signals` + `environment.apiUrl` (`http://localhost:5000` Gateway) + `withCredentials:true`
- `app.config.ts` `provideTanStackQuery(new QueryClient({defaultOptions: {queries: {staleTime: 2*60*1000}}}))` matches Redis `board 5m`/`tasks 2m`

---

## 5. Redis Caching

**Why:** Avoid repeated `SQL` for `board:{projectId}` and `tasks:{hash}`; works across `4` instances via `Upstash`.

- `Application/Caching/CacheKeys.cs` (pure) + `Application/Interfaces/IRedisCacheService.cs` (`GetAsync<T>/SetAsync/RemoveAsync`) + `Application/Caching/ICacheableRequest<T>` + `Application/Behaviors/CachingBehavior<TRequest,TResponse>` (`HIT/MISS` `X-Cache` header)
- Never call `RedisCacheService` directly in a controller — handler or behavior uses `IRedisCacheService` + `CacheKeys`.
- On write (`CreateTask/MoveTask`): `_cache.RemoveAsync(CacheKeys.Board(projectId))` + `RemoveByPrefixAsync($"tasks:{projectId}:")`

---

## 6. YARP, EF Core & Other Backend Rules

- **Secrets:** `.env.example` + `appsettings.Development.json.example` with `PASTE_` placeholders. Real secrets are gitignored. Same keys for local/prod (`rediss://`, `amqps://`, `Cloudinary`, `Brevo xkeysib-...`, `Gemini AIza...`), only URLs differ.
- **YARP 2.3:** `yarp.json` `Order 0` specific (`/api/workspaces/{wid}/projects/{**catch-all}`) before `Order 1` catch-all (`/api/workspaces/{**catch-all}`). Add `team-route` for `/api/teams` with same `Order` rule.
- **EF Core 10:** `HasDefaultSchema("project")` single `flowboard` DB `4` schemas, `Ignore(DomainEvents)`, `MigrationsHistoryTable("__EFMigrationsHistory", schema)`, composite `WorkspaceMember` PK, `TaskItem` avoids `Task` clash.
- **Roles — single source `BuildingBlocks/SharedKernel/Roles.cs` + `frontend/shared/constants/roles.ts`:** Fixed `Member 1 / OrgAdmin 2 / Client 3` (`OrganizationMember.Role`) + `SuperAdmin 0` (`Users.IsSuperAdmin`). Workspace roles are **dynamic custom** via `[identity].OrganizationWorkspaceRoles` + `WorkspaceMembers.CustomRoleId` (e.g., `Developer`, `QA`) — **never hardcoded `ProjectManager/Viewer`**. Use `Roles.OrgAdmin` constants, `RolePermissions` join for `attachment:view` etc.
- **Board/Sprint/Issue:** `Board = view` (filter `teamIds` + `sprintId`), `Sprint = project time-box` (`ProjectId`, `BoardId?`), `Issue = single source` (`Status` synced on `MoveToList`), `Backlog = view` `WHERE SprintId IS NULL`.
- **Future:** `3.1` `CloudAMQP+MassTransit+Outbox 2s` poll, `3.2` `SignalR 10.0 Hub :5004 /hubs/board` Groups, `3.3` `CDK DragDrop` + Redis lock + optimistic + realtime.

---

## 7. Frontend 3-File Rule

**Why:** Keeps styling consistent (one `src/styles.css`) and makes every component predictable for code review.

**Rule:** Every **component folder** must have **exactly 3 files**: `*.component.html` + `*.component.ts` + `*.component.css` (css **always empty** `/* No internal CSS */`, styles via `src/styles.css`).

**Good:**
- `features/notifications/notification-list/notification-list.component.{html,ts,css}`
- `shared/components/loader/loader.component.{html,ts,css}`
- `features/board/board.component.{html,ts,css}` (not `board/board/board.component.*`)

**Bad:**
- `features/notifications/notification-list.component.ts` + `notification-detail-modal.component.ts` in same folder (split required)
- `features/board/board/board.component.*` (double folder)
- `template: `...`` in `*.ts` (forbidden, use `templateUrl`)

**Check:** `grep -r "template:" src/app` must be `0`. Every `*.css` empty. `ng build` `0 errors`.

---

## 8. Git & Session Rules

- `git` at `FlowBoard` root (`backend/` + `frontend/` siblings), `origin https://github.com/Kumar209/FlowBoard.git` `main`
- One task at a time, `Documents/TASK_LOG.md` `8` sections + `Progress Overview` `X/50`, `Documents/FlowBoard_Tasks_Plan.docx` + `Documents/FlowBoard_System_Design.docx` are source of truth — any new chat must read them before coding.
- After each task: `dotnet build -c Release 0W` + `ng build --configuration production` `0 errors` before `git push`.

---

## 9. Permissions — Every New Feature Needs Them

**Why:** Custom roles (`Developer`, `QA`) need explicit permissions, otherwise `403` is inconsistent.

**Rule:** When you add a new entity or operation (e.g., `Status`, `Boards`, `Sprints`), add `4` permissions to `IdentitySeeder.SeedPermissionsAsync` (`[identity].Permissions`):

```
{entity}:view    → View {Entity}      — Group {Entity}
{entity}:create  → Create {Entity}    — Group {Entity}
{entity}:update  → Update {Entity}    — Group {Entity}
{entity}:delete  → Delete {Entity}    — Group {Entity}
```

plus `task:move`, `task:assign`, etc. Use `lowercase:view/create` (`status:view` not `StatusView`).

**Example:** `Status → status:view/create/update/delete` (`7.0`, `[project].Statuses` + `BoardColumnStatuses`) — total `27 → 31` → `attachment:view/create/update/delete` `31 → 35`.

**Checklist before `git push`:**
- [ ] Added `4` `Permission` rows in `IdentitySeeder.cs` with `Group` `Description`
- [ ] Updated permission count in docs
- [ ] Added `RolePermissions` checks in `*Service.CanViewAsync` (like `OrganizationActivityService.CanViewAsync` for `status:view`)
- [ ] Verified `GET /api/permissions` returns new keys

---

## 10. Human Error Messages — Never Expose Internals

**Why:** Users should see `Title required`, not `Raw: {"line":5} at System.Text.Json` + `StackTrace`.

**Rule (applies to ALL 4 services + Gateway + Frontend):**
- Log `Exception + RawPreview 500 + PromptHash + DurationMs + Stack` to `ILogger`/`Serilog` server only.
- Client gets `400 {error:"AI draft failed — please try again."}` or `429 {error:"Too Many Requests — please try again after 60s", retryAfter:60, Header Retry-After}` — **no** `Raw:` `LineNumber` `at System`.
- Frontend `toast.error` truncates `>120` chars to `AI operation failed — please try again.`
- `AI Enhance` `description` is `optional` — `if empty → generate from title only` `prompt = Title: {title}` `maxOutputTokens 2048` — never `toast "Description shouldn't be empty"`; `Title empty → toast "Title required"`.

**Enforcement:** Every `Controller → Command → Handler → Service` must:
```csharp
catch (Exception ex) { _logger.LogError(ex, full); return Result.Failure("AI operation failed — please try again."); }
```
not `return Failure($"Raw: {raw[..200]}")`. Code review: `grep -r "Raw:"` `grep -r "LineNumber"` must be `0` (except logger).

**Checklist before `git push`:**
- [ ] `Backend` `catch` logs `ex` + `RawPreview` via `_logger`, returns `human` `400/429` only (keep `Retry-After`).
- [ ] `Frontend` `toast` never shows `Raw:`/`LineNumber`/`at System` — `>120` truncated.
- [ ] `AI Enhance` `description optional` `Title only` `2048` tokens.

---

*Last updated: 2026-09-14 — Added Rate Limiter + Human Error + 3-File Rule human-readable. Source of truth: `Documents/TASK_LOG.md` `38/50`.*


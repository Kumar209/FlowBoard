# FlowBoard — Architecture Rules (MNC-Grade — Strict)

> **Source:** `Documents/FlowBoard_System_Design.docx v1.3` + `Documents/FlowBoard_Tasks_Plan.docx v1.1`
> **Applies to:** All microservices (`Identity.Service`, `Project.Service`, `File.Service`, `Notification.Service`) + Gateway + Frontend
> **Enforcement:** Every new controller/command/query/service must follow this. Any session (new Opencode chat, new agent) must read this + `TASK_LOG.md` + `SESSION_RESUME.md` before coding. Violations are blocked in code review.

---

## 1. Layering — Clean Architecture + DIP (Strict)

```
Api (Controllers) 
  → Application (Interfaces + Commands/Queries/DTOs + MediatR Handlers) 
  → Domain (Entities, Enums, BaseEntity)
  → Infrastructure (Implementations of Application Interfaces + Persistence + External Services)
  ← SharedKernel (BaseEntity, Result, DomainEvent)
  ← Shared.Contracts (Integration Events)
```

**Rule:** `Application` defines `interfaces`, `Infrastructure` implements. `Application` never references `Infrastructure` (except via DI). `Api` depends only on `Application` (via `IMediator` + `IApplicationDbContext` abstraction). `Domain` has no dependencies.

**Forbidden:** `using Infrastructure` inside `Application/Commands` handlers, `new DbContext` in controller, `static` service calls.

---

## 2. Controller → Command/Query → Service (Strict — like AuthController)

**Pattern (Auth is reference):**

```
Controller (Api) 
  - Injects IMediator + IApplicationDbContext only for thin orchestration (e.g., GetUserId() from claims)
  - Does NOT contain business logic, no _db.Users.Where(...).ToList()
  - Calls await _mediator.Send(new XxxCommand(...)) or new XxxQuery(...)
  - Maps Result<T> to 201/200/403/BadRequest via Result.IsSuccess
  - Extracts CallerId (Guid from ClaimTypes.NameIdentifier/sub) + CallerRoles (ClaimTypes.Role/role) and passes to command

Command/Query (Application) 
  - Record implements IRequest<Result<T>> (MediatR 12.4)
  - Validator : AbstractValidator<T> (FluentValidation 11.10) — Title 300, Name 200, etc.
  - Handler : IRequestHandler<T, Result<T>> — Injects *Service Interfaces* (e.g., IProjectService, ITaskService, IJwtProvider) — NOT IApplicationDbContext directly (except for Project legacy, must migrate to service)
  - Handler calls service methods, returns Result<T>.Success / Failure (SharedKernel Result) — no throw for business errors

Service Interface (Application/Interfaces) 
  - e.g., IProjectService, IBoardService, ITaskService, IWorkspaceService, IOrganizationService, IJwtProvider, IPasswordHasher, IRefreshTokenService, IBrevoEmailService, IRedisCacheService
  - Methods return DTOs or Result<T>, hide EF details

Service Implementation (Infrastructure/Services or Infrastructure/Persistence) 
  - Implements interface, injects IApplicationDbContext + other infra (IRedisCacheService, HttpClient for Brevo/Cloudinary/Gemini, IConnectionMultiplexer for Redis)
  - Contains EF Core queries (Where, Include), hasTransaction, ActivityLog/Outbox same txn, cache invalidation via IRedisCacheService
  - Registered in Program.cs as AddScoped<IInterface, Implementation> (or AddHttpClient for Brevo, AddSingleton for Redis)
```

**Example — Auth (good):**
- `Api/Controllers/AuthController.cs` → `await _mediator.Send(new RegisterCommand(dto.Email, dto.Password, dto.FullName))`
- `Application/Commands/RegisterCommand.cs` + `RegisterCommandHandler` injects `IJwtProvider, IPasswordHasher, IRefreshTokenService, IApplicationDbContext` (via IApplicationDbContext abstraction, but should ideally be IIdentityService — future)
- `Infrastructure/Services/JwtProvider.cs : IJwtProvider` (moved from Application/Services)

**Example — Workspace/Organization (must refactor to this):**
- Before (bad): `WorkspacesController.cs` has `_db.WorkspaceMembers.Where(...).ToListAsync()` — remove
- After (good): `WorkspacesController` → `GetMyWorkspacesQuery(workspaceId, page, search)` → `GetMyWorkspacesHandler` injects `IWorkspaceService` → `Infrastructure/Services/WorkspaceService.cs` does EF

**Example — Project (must refactor):**
- Before (bad): `CreateProjectCommandHandler` has `_db.Projects.Add(...)` — move to `IProjectService.CreateProjectAsync(...)` in `Infrastructure/Services/ProjectService.cs`
- After (good): `ProjectsController` → `CreateProjectCommand(workspaceId, name, description, callerId, callerRoles)` → handler → `IProjectService` → Infrastructure EF + cache invalidation

---

## 3. Folder Structure (Strict)

```
backend/Services/{Service}/
  Api/Controllers/          <- thin, IMediator only, no business logic, no _db
  Application/
    Interfaces/             <- IApplicationDbContext, IRedisCacheService, IJwtProvider, IWorkspaceService, IProjectService, etc. + DTOs
    Commands/               <- Records + Validators + Handlers (no EF, call services)
    Queries/                <- Records + Handlers (call services, ICacheableRequest for pipeline)
    DTOs/                   <- ProjectDto, TaskDto, etc. (no EF navigation)
    Behaviors/              <- CachingBehavior<TRequest,TResponse> for ICacheableRequest
    Caching/                <- CacheKeys, ICacheableRequest
    Validators/             <- FluentValidation (if separate)
  Domain/
    Entities/               <- Project, Board, BoardList, TaskItem, SubTask, Comment, ActivityLog, OutboxMessage, Sprint, Team, etc. : BaseEntity, IAggregateRoot, private setters, methods Update/Move/Rename
    Enums/                  <- TaskPriority, WorkspaceRole
  Infrastructure/
    Persistence/            <- DbContext : IApplicationDbContext, HasDefaultSchema("project"/"identity"), Ignore(DomainEvents), Migrations
    Services/               <- Implementations of Application/Interfaces (e.g., WorkspaceService, ProjectService, JwtProvider, BrevoEmailService, RedisCacheService, CloudinaryService, GeminiService)
    Caching/                <- RedisCacheService : IRedisCacheService (StackExchange.Redis)
  Program.cs                <- AddDbContext + AddMediatR + AddScoped<IInterface, Implementation> + AddAuthentication(JwtBearer) + AddAuthorization + MapControllers
```

**Rule:** `Application/Services` folder must NOT exist — all service implementations go to `Infrastructure/Services` (see Image 1 — Application has Services, should be Infrastructure). If you see `Application/Services`, move to `Infrastructure/Services` and update `namespace` + `Program.cs` `using`.

---

## 4. Frontend — Angular 22 Standalone + Signals + TanStack (Strict)

- `input.required<T>()` + `computed` + `ChangeDetectionStrategy.OnPush` + `inject(HttpClient)` + `firstValueFrom` (not `toPromise`) + `injectQuery/injectMutation` (TanStack experimental 5.62)
- No `NgRx`, no internal CSS (`style: none` → Tailwind + DaisyUI 4.12.14 `src/styles.css` only, 6 themes `light/dark/corporate/cupcake/emerald/synthwave`)
- `core/services/project.service.ts` uses `inject(HttpClient)` + `signals` + `environment.apiUrl` (`http://localhost:5000` Gateway) + `withCredentials:true`
- `app.config.ts` `provideTanStackQuery(new QueryClient({defaultOptions: {queries: {staleTime: 2*60*1000}}})` matches Upstash `board 5m`/`tasks 2m`

---

## 5. Redis Caching (MNC-Grade)

- `Application/Caching/CacheKeys.cs` (pure, no Infra) + `Application/Interfaces/IRedisCacheService.cs` (GetAsync<T>/SetAsync/RemoveAsync) + `Application/Caching/ICacheableRequest<T>` + `Application/Behaviors/CachingBehavior<TRequest,TResponse> : IPipelineBehavior` (HIT/MISS `X-Cache` header)
- Never use `RedisCacheService` directly in controller — handler or behavior uses `IRedisCacheService` + `CacheKeys`
- Invalidation on write: `CreateTask/MoveTask` handler calls `_cache.RemoveAsync(CacheKeys.Board(projectId))` + `RemoveByPrefixAsync($"tasks:{projectId}:")`

---

## 6. Other MNC Rules (Memorized)

- `.env.example` + `appsettings.Development.json.example` with `PASTE_` placeholders — real secrets gitignored, same keys local/prod (Upstash `rediss://`, CloudAMQP `amqps://`, Cloudinary, Brevo `xkeysib-...`, Gemini `AIza...`)
- `YARP 2.3` `yarp.json` `Order 0` specific (`/api/workspaces/{wid}/projects/{**catch-all}`) before `Order 1` catch-all (`/api/workspaces/{**catch-all}`) — add `team-route` for `/api/teams`
- `EF Core 10` `HasDefaultSchema("project")` single `flowboard` DB 4 schemas, `Ignore(DomainEvents)`, `MigrationsHistoryTable("__EFMigrationsHistory", schema)`, composite `WorkspaceMember` PK, `TaskItem` avoids `Task` clash
- `Task 1.5` RBAC `PM can create projects` `IsInRole(OrgAdmin,ProjectManager,SuperAdmin)` else `403` — `Client 403 POST /tasks`, `Viewer` view only
- `Board = view` (filter `teamIds` + `sprintId`), `Sprint = project time-box` (`ProjectId`, `BoardId?` nullable), `Issue = single source` (`Status` synced to `BoardList.Name` on `MoveToList`), `Backlog = view` `WHERE SprintId IS NULL`, `Environments` FK optional `Url`, `Team` `TeamMember` per project
- `Future` `3.1` CloudAMQP+MassTransit+Outbox `2s` poll, `3.2` SignalR `10.0` Hub `:5004 /hubs/board` Groups, `3.3` CDK DragDrop + Redis lock + optimistic + realtime

---

## 7. Frontend Component — 3-File Rule (MNC-Grade — Strict — Memorize Forever)

**Rule (never break):**
- Every **component folder** must contain **exactly 3 files**: `*.component.html` + `*.component.ts` + `*.component.css` (css **always empty** `/* No internal CSS */`, all styles via `src/styles.css` Tailwind+DaisyUI).
- **TS never holds HTML**: `template: `...`` is **forbidden**. Always use `templateUrl: './xxx.component.html'` + `styleUrls` is empty. `imports: [CommonModule]` + `ChangeDetectionStrategy.OnPush`.
- A **feature folder** (e.g., `features/notifications`) may contain **multiple component subfolders** (e.g., `notification-list/` + `notification-detail-modal/`), each with 3 files. A **child component not shared** by others may live as subfolder inside parent component folder (e.g., `notification-list/notification-detail-modal/`), also 3 files — but shared components go to `shared/components/`.
- **Double folder forbidden**: `features/board/board/board.component.*` is **incorrect** (component name `board` then folder `board` again). Correct is `features/board/board.component.*` (3 files directly under `features/board/`). Same for any `feature/<name>/<name>/`.

**Correct examples:**
- `features/notifications/notification-list/notification-list.component.{html,ts,css}` + `features/notifications/notification-detail-modal/notification-detail-modal.component.{html,ts,css}` (sibling component folders)
- `shared/components/loader/loader.component.{html,ts,css}` (css empty)
- `features/board/board.component.{html,ts,css}` (not `board/board/board.component.*`)

**Incorrect examples:**
- `features/notifications/notification-list.component.ts` + `notification-detail-modal.component.ts` in same folder (4 files in one folder — split required)
- `features/board/board/board.component.*` (nested double)

**Enforcement:** `grep -r "template:" src/app` must return **0** hits. Every `*.ts` must have `templateUrl`. Every `*.css` is empty. `ng build` must stay `0 errors`.

---

## 8. Commit & Session

- `git` at `FlowBoard` root (backend/ + frontend/ siblings), `origin https://github.com/Kumar209/FlowBoard.git` `main`
- One task at a time, `TASK_LOG.md` 8-section + `Progress Overview` `X/26`, `SESSION_RESUME.md` + `Documents/*.md` + `Postman` remain source of truth — any new chat must read them before coding, even if told "already verified"
- After each task, `dotnet build -c Release 0W` + `ng build --configuration production` `0 errors` before `git push`

---

## 9. Permissions CRUD Rule (Strict — Every New Feature Must Add Permissions)

**Rule (never skip):**
- Whenever a new domain, entity, or operation is added (e.g., `Status` in `6.5→7.0`, `Boards`, `Sprints`, `Tasks`, `Files`, `AI`), you **MUST** add its CRUD permissions to the fixed catalog `IdentitySeeder.SeedPermissionsAsync` (`[identity].Permissions`):

```
{entity}:view    → View {Entity}      — Group {Entity}
{entity}:create  → Create {Entity}    — Group {Entity}
{entity}:update  → Update {Entity}    — Group {Entity}
{entity}:delete  → Delete {Entity}    — Group {Entity}
```

  plus any extra operation-specific perms (e.g., `task:move`, `task:assign`, `activity:view:org`).
- `view` is mandatory as the base read gate; `create/update/delete` follow REST `POST/PUT/DELETE`. Use lowercase `:` separator (`status:view` not `StatusView`).
- Naming: `key` must be `lowercase:view/create/update/delete` (`status:view`, `sprint:view`, `file:view`), `Group` is capitalized (`Status`, `Sprint`, `Board`), `Name` is `View/Create/Update/Delete {Entity}`.
- For this Jira-like implementation we added `status:view | status:create | status:update | status:delete` (4) → total `27 → 31` permissions (count = previous 27 + 4). Future `Phase 4` (`File`, `AI`) and any new module must do the same (e.g., `attachment:view/create/delete`, `ai:view/create`).
- Enforcement: `IOrganizationRoleService` + `IOrganizationActivityService` check `activity:view:org` etc. via `RolePermissions` join; `SeedPermissionsAsync` is the single source of truth — no hard-coded permission string outside seeder + check.

**Examples:**
- `Status` → `status:view / status:create / status:update / status:delete` (implemented `7.0`, `[project].Statuses` + `[project].BoardColumnStatuses`)
- `Sprint` → `sprint:view / sprint:create / sprint:update / sprint:delete` (already implied by `board` group, but should be explicit for sprint CRUD if separated)
- `BoardColumn` → `board:update` covers column mapping (or `board:manage`); if new entity `BoardColumn` has its own CRUD, add `boardcolumn:view/...`

**Checklist before `git push`:**
- [ ] Added 4 `Permission` rows in `IdentitySeeder.cs` with `Group` and `Description`
- [ ] Updated permission `count` in docs (`27 → 31 → ...`)
- [ ] Added `RolePermissions` handling (seed for new roles if needed) and checks in `*Service.CanViewAsync` (like `OrganizationActivityService.CanViewAsync` for `status:view`)
- [ ] Verified `GET /api/permissions` returns new keys

---

*Last updated: 2026-09-09 — Added Permissions CRUD Rule (every new feature must add view/create/update/delete per entity, example status:view/create/update/delete 27→31), Jira company-managed Statuses/Boards/Columns mapping, Statuses explicit creation, Boards as views.*

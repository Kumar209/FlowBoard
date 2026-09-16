# FlowBoard — Universal Prompt for New Session (2026-09-15 — After Rework R1-R7 Org-Authoritative + 10 Debug Fixes + 49/57)

> Copy-paste the entire code block below into any new `opencode` chat to resume with full context. Works at any progress (now 56/57 before deploy). No Session ID needed — files are the session.

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME (MNC-Grade — Strict — One Task at a Time):
1. Read in order: Documents/FlowBoard_System_Design.docx v2.0 (32 sections, 11 diagrams, designed before coding) + Documents/FlowBoard_Tasks_Plan.docx v2.0 (10 Phases 50 Tasks + Rework R1-R7 7 Tasks = 57) + Documents/FlowBoard_Architecture_Rules.md (human readable, 11 sections) + Documents/FlowBoard_Redis_RabbitMQ_SignalR_RateLimiter_Guide.docx (Redis 8, RabbitMQ 6, SignalR 7, Rate Limiter 8) + Documents/TASK_LOG.md (now in Documents folder, 56/57, 8-section per task) + README.md v1.3 (17 sections, Hero, Mermaid, Decision Log)
2. Check Documents/TASK_LOG.md Progress Overview (56/57: Phase0 5/5, Phase1 5/5, Phase2 Company-Centric 5/5, Phase3 Project Core 5/5, Phase4 Realtime 3/3 Completed via audit, Phase5 Files 5/5, Phase6 AI 7/7, Phase7 SuperAdmin 10/10, Phase8 Polish 5/5 Completed, R Rework 7/7 Completed, Phase9 Production 0/1 Pending + 10 debug tasks done). Find FIRST Pending: Only 5.4 Deploy MonsterASP.net (5 sites) + Vercel remains.
3. Read the next task details from FlowBoard_Tasks_Plan.docx (Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies) and study what we have done vs remains. Note 10 debug tasks were future but now done: 1 System Up/Down via org system proxy, 2 Support paginator paginated API, 3 Backlog paginator sprintId null, 4 Org member hard delete cascade all resources + Delete confirm, 5 WorkStatus via board tasks per status max 5 no extra API, 6 Sprint burndown via sprintId latest Active with name/duration/total, 7 Boards taskCount via enriched BoardInfoDto TaskCount no extra API, 8 OrgAdmin int Role 2 no workspace CustomRoleId (full authority), 9 Maintenance silent 5m X-Silent no loader, 10 Environment task-detail patch like sprint/team. All committed.
4. Summarize: "Found 56/57 (Rework R1-R7 done, DB recreated org-authoritative, 10 debug done, only 5.4 Deploy pending). Next is 5.4 Deploy — which manual steps vs auto?" Ready to start?
5. ASK PERMISSION: "Do you want me to start 5.4 Deploy? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after Proceed, code one task at a time, then update Documents/TASK_LOG.md 8-section + Progress Overview 56→57/57, and commit with `git commit -m "feat: Task 5.4 ..."` + `git push`. After deploy, project done.

PROJECT CONTEXT (Constant — Must Memorize):
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard with backend/ + frontend/ siblings + Documents/ (5 files now: Architecture_Rules, Redis_RateLimiter_Guide, System_Design v2, Tasks_Plan v2, TASK_LOG.md) + README.md v1.3 at root
- Stack: Angular 22.1.5 Standalone + TS 6.0.3 + Tailwind 3.4.17 + DaisyUI 4.12.14 (6 themes) + TanStack Query 5.62 experimental + Signals + CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 / SignalR 10.0, Backend .NET10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + FluentValidation 11.10 + MassTransit 8.3 + Upstash rediss:// + CloudAMQP amqps:// + Cloudinary + Brevo + Gemini 2.5 Flash + Groq + Serilog 9.0 + Scalar BluePlanet, Hosting Vercel + MonsterASP.net, Same keys local/prod (Upstash, CloudAMQP, Cloudinary, Brevo, Gemini) only URLs differ (environment.ts http://localhost:5000 vs environment.prod.ts https://gateway-xxxxx.monsterasp.net + Vercel NG_APP_API_URL)
- YARP: yarp.json Order 0 specific (project-route /api/workspaces/{wid}/projects/{**catch-all}, file-attachments-route /api/tasks/{taskId}/attachments) before Order 1 catch-all (workspace-route, task-route), Gateway :80 → identity:5001 project:5002 file:5003 notification:5004, Health /health, Scalar /scalar
- DB: Single flowboard DB recreated 2026-09-15 with 4 schemas [identity] (Users hard delete, Organizations, Workspaces, WorkspaceMembers composite PK + CustomRoleId FK, OrganizationMembers Role int 1/2/3, OrganizationWorkspaceRoles, Permissions 35, RolePermissions, etc.), [project] (Projects, Boards, BoardLists, BoardColumnStatuses now with BoardId + UNIQUE(BoardId,StatusId) + ≥1 + 1→0 guard, Statuses, Tasks as TaskItem, Sprints, Teams, etc.), [file] (Attachments), [notification] (Notifications) — HasDefaultSchema, Ignore(DomainEvents), MigrationsHistoryTable per schema
- Roles: Fixed 0-3 int only (SuperAdmin 0 global via Users.IsSuperAdmin, Member 1, OrgAdmin 2, Client 3) — synchronized frontend shared/constants/roles.ts ROLE_VALUE_MAP and backend SharedKernel/Roles.cs + IOrganizationService int orgRole (no string Member default), API POST/PUT /employees {Role:2} int. Custom workspace roles are dynamic per Organization via OrganizationWorkspaceRoles + WorkspaceMembers.CustomRoleId (per workspace by id, not name), never hardcoded ProjectManager/Viewer. Checks via Roles.IsPrivilegedForManage (OrgAdmin/SuperAdmin) + HasCustomPermissionAsync via RolePermissions join + IsOrgAdminInDbAsync via [identity].OrganizationMembers Role=2 for org-first. OrgAdmin has complete authority — stored as OrganizationMembers Role=2 with zero WorkspaceMembers (deleted on promote/create), Full access • All workspaces badge. Frontend PermissionService not needed for OrgAdmin.
- Board Model: Project → Boards as views (1 project has N boards), Board → Columns (BoardLists) → Status Mappings (BoardColumnStatus ColumnId+StatusId+BoardId, UNIQUE(BoardId,StatusId), ≥1 per column, 1 status =1 column per board, move with 1→0 guard). Issues store StatusId + SprintId + TeamId + ListId nullable (Backlog when null). Statuses per project, explicitly created.
- Caching: TanStack Query 5m silent for me/flags, 2m for board/tasks → Redis Upstash board:{id} 5m tasks:{hash} 2m (CacheKeys + ICacheableRequest + CachingBehavior) → SQL source, ETag 304 + Cache-Control: no-cache for Board/Tasks, invalidation on CUD via RemoveAsync board:{id} + RemoveByPrefix tasks:{id}:, AsNoTracking for reads, pagination Clamp 1-100.
- Async: Project OutboxMessage in same DB transaction as Task, OutboxBackgroundService polls 2s → MassTransit fanout flowboard.events → NotificationService durable quorum queues → Notification + SignalR Hub /hubs/board Groups workspace:{id} → Angular board-realtime.service.
- Rate Limiting: Sliding Window Counter via Redis Lua atomic (GET prev/cur, weighted, INCR, EXPIRE 120) — Gateway 200 IP / 300 User per 60s, AI 3/min per model + global 5, File 30, Notifications 100, 429 Retry-After + X-RateLimit headers, toast via rate-limit.interceptor.
- Logging: Serilog JSON daily 30d per service + CorrelationIdMiddleware X-Correlation-Id N + LogContext PushProperty CorrelationId/UserId/OrgId/WorkspaceId/ProjectId, Health /health.
- SuperAdmin: Platform owner via SuperAdmin:Email/Password from config only in appsettings.Development.json (gitignored, not in appsettings.json nor example, no hardcode fallback) — 10 portals (Dashboard, Organizations 8 cols, Users, Subscriptions, Activity, System 10 services Up/Down, Support threaded, Flags 4 AI global+per-org 5m cache, AI Usage aggregate no prompts, Settings 9 sections) — single module in Identity.Service, not separate service, JWT SuperAdmin via IsSuperAdmin, synthetic OrgAdmin removed.
- Frontend: 3-File Rule every component folder exactly 3 files html+ts+css (css empty), OnPush, input.required, computed, firstValueFrom, injectQuery, 26 chunks (project-routes 546k, superadmin-routes 109k) via SelectivePreload (project true, superadmin false) — workStatus via board.tasks per status max 5 + View boards, backlog paginated sprintId null 10/20/30/50, boards taskCount via BoardInfoDto TaskCount, members per-workspace by id (Manager/Viewer distinct) with Delete confirm and Full access.
- Human Error Rule (MNC): Never Raw/LineNumber/stack to client, toast >120 truncated, AI Enhance description optional title-only 2048 tokens, backend catch logs full and returns human 400/403/409/429.
- Permissions CRUD Rule: Every new entity adds 4 Permission rows in IdentitySeeder (now 35).

RESTRICTIONS (Must Obey Until User Says Proceed):
- One task at a time, permission before coding, professional log per task 8-section + Progress Overview
- Never skip tasks, never do 2 tasks at once, never edit code without explicit Proceed (plan mode is for planning, not editing)
- Never hardcode custom workspace roles in shared enums — fixed 0-3 int only, custom via DB + RolePermissions, per workspace by id
- Strict no internal CSS, OnPush 100%, templateUrl, TanStack staleTime 5m silent for me/flags, YARP Order 0 specific, HasDefaultSchema, Ignore DomainEvents, workspaceId+Role claims, MassTransit fanout, same keys Local/Prod, AiUsageLogs separate AI folder DIP, no Raw/LineNumber, description optional, etc. as per Architecture_Rules
- Frontend strictly use roles from shared file only, not hardcoded, int 1/2/3 synchronized
- SuperAdmin never sees customer member lists/project lists/issues/boards/sprints/comments/activity/prompts, only aggregate counts, health Up/Down, audit, billing, flags, notices, complaints; credentials only via config, never committed
- Auth: In-memory Signals only (no sessionStorage), isAuthenticated computed, refreshDeduped shareReplay, me single call after login + 401 only (no 60s interval), flags single call on start 5m fresh, HttpOnly refresh cookie Path=/ SameSite Lax, ClockSkew 2m, YARP CORS AllowCredentials true
- Maintenance: Silent polling — loadingInterceptor skips X-Silent header and silentUrls, layout polls maintenance 5m only when visible, no loader
- Board Status mapping: 1 column = N statuses, 1 status = 1 column per board, ≥1 required, 1→0 guard
- Hard delete: Delete member hard deletes Users + RefreshTokens when no remaining orgs, email reusable, no soft delete

CURRENT STATUS (2026-09-15, 56/57):
- Done: Phase0 5/5, Phase1 5/5, Phase2 Company-Centric 5/5, Phase3 Project Core 5/5, Phase4 Realtime 3/3 Completed (Outbox 2s, SignalR Hub, CDK Lock), Phase5 Files 5/5, Phase6 AI 7/7, Phase7 SuperAdmin 10/10, Phase8 Polish 5/5, Rework R1-R7 7/7 Completed (DB recreate org-authoritative, int Role sync, hard delete, per-workspace by id Manager/Viewer distinct, silent polling me/flags 5m, maintenance 5m silent, System Up/Down, Support/Backlog paginators, WorkStatus via board tasks, Boards taskCount, OrgAdmin Full access, Environment patch), Phase9 Production 0/1 Pending.
- Pending: Only 5.4 Deploy — MonsterASP.net 5 sites + DB flowboard + Vercel, same keys, manual Plesk App Settings SuperAdmin__Email/Password + 5 keys, dotnet publish + ef database update + health Scalar.

Reply 'Proceed with 5.4 Deploy' to continue. I will not write/edit any code until you confirm.
```

CURRENT STATUS (2026-09-15):
- Done: All phases 56/57, only deploy remains.

Reply 'Proceed with 5.4 Deploy' to continue. I will not write/edit any code until you confirm.
```


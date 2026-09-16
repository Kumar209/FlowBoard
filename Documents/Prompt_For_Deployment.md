# FlowBoard — Prompt for Deployment Session (2026-09-15 — 63/64 Before Deploy)

> Copy-paste the entire code block below into any new `opencode` chat to resume deployment. Works at any progress (now 63/64). No Session ID needed — files are the session.

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME (MNC-Grade — Strict — One Task at a Time):
1. Read in order: Documents/FlowBoard_System_Design.docx v2.0 + Documents/FlowBoard_Tasks_Plan.docx v2.0 + Documents/FlowBoard_Architecture_Rules.md + Documents/FlowBoard_Redis_RabbitMQ_SignalR_RateLimiter_Guide.docx + Documents/TASK_LOG.md (now 63/64) + README.md v1.3 + Documents/Prompt_For_New_Session.md (now deployment) + Documents/Prompt_For_Deployment.md (this file)
2. Check Documents/TASK_LOG.md Progress Overview (63/64: All phases 0-8 + R 7/7 + N 7/7 Completed, only 5.4 Deploy 0/1 Pending). Find FIRST Pending: 5.4 Deploy.
3. Read Task 5.4 details from FlowBoard_Tasks_Plan.docx (Objective: Deploy to MonsterASP.net 5 sites + Vercel + DB flowboard with same Upstash/CloudAMQP/Cloudinary/Brevo/Gemini keys local/prod, only URLs differ). Study what is done vs remains.
4. Summarize: "Found 63/64, only 5.4 Deploy pending (MonsterASP 5 sites + Vercel + DB + same keys). Next is 5.4." Ready to start?
5. ASK PERMISSION: "Do you want me to start 5.4 Deploy? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after Proceed, do deployment tasks one by one: Manual Plesk App Settings (Jwt, Redis, RabbitMQ, Cloudinary, Brevo, Gemini, SuperAdmin__Email/Password) for Identity site only, dotnet publish Release for Gateway + 4 services, dotnet ef database update on mssql.monsterasp.net for Identity/Project/File/Notification (4 schemas same DB), FTP/Web Deploy 5 zips to site/wwwroot, check https://gateway-xxxxx.monsterasp.net/health 200 and /scalar, Vercel auto builds on push main. Then update TASK_LOG.md 63→64/64, commit, push. Project done.

PROJECT CONTEXT (Constant — Must Memorize):
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard with backend/ + frontend/ siblings + Documents/ (5 files now) + README.md v1.3 at root
- Stack: Angular 22.1.5 Standalone + TS 6.0.3 + Tailwind 3.4.17 + DaisyUI 4.12.14 (6 themes) + TanStack Query 5.62 experimental + Signals + CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 / SignalR 10.0, Backend .NET10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + FluentValidation 11.10 + MassTransit 8.3 + Upstash rediss:// + CloudAMQP amqps:// + Cloudinary + Brevo + Gemini 2.5 Flash + Groq + Serilog 9.0 + Scalar BluePlanet, Hosting Vercel + MonsterASP.net, Same keys local/prod only URLs differ (environment.ts http://localhost:5000 vs environment.prod.ts https://gateway-xxxxx.monsterasp.net + Vercel NG_APP_API_URL)
- YARP: yarp.json Order 0 specific before Order 1 catch-all, Gateway :80 → identity:5001 project:5002 file:5003 notification:5004, Health /health, Scalar /scalar
- DB: Single flowboard DB with 4 schemas [identity] (Users hard delete, Organizations, Workspaces, WorkspaceMembers composite PK + CustomRoleId FK, OrganizationMembers Role int 1/2/3, OrganizationWorkspaceRoles, Permissions 35, RolePermissions, OutboxMessages for Identity ComplaintCreated), [project] (Projects, Boards, BoardLists, BoardColumnStatuses with BoardId + UNIQUE(BoardId,StatusId) + ≥1 + 1→0 guard, Statuses, Tasks as TaskItem, Sprints, Teams, OutboxMessages for TaskCreated/Moved/Assigned/Deleted/ProjectMemberAdded), [file] (Attachments), [notification] (Notifications) — HasDefaultSchema, Ignore(DomainEvents), MigrationsHistoryTable per schema
- Roles: Fixed 0-3 int only (SuperAdmin 0 global via Users.IsSuperAdmin, Member 1, OrgAdmin 2, Client 3) — custom workspace roles dynamic per Organization via OrganizationWorkspaceRoles + WorkspaceMembers.CustomRoleId per workspace by id, never hardcoded. OrgAdmin has complete authority — stored as OrganizationMembers Role=2 with zero WorkspaceMembers (deleted on promote/create), Full access • All workspaces badge. Frontend isOrgAdmin from OrganizationMembers via Me, not WorkspaceMembers.
- SuperAdmin: Via SuperAdmin:Email/Password from config only in appsettings.Development.json (gitignored) or App Settings SuperAdmin__Email/Password on MonsterASP Identity site, not in appsettings.json nor example, no hardcode fallback.
- Notification MNC: Outbox 2s fanout flowboard.events per service (Project and Identity separate OutboxMessages), idempotency via EventId, recipient list per project members at publish time, hub per-project + per-user Groups user:{id} with leave/join on route change, token factory live accessToken, frontend event-driven invalidation, 7 events TaskCreated/Moved/Commented/Assigned/Deleted/ProjectMemberAdded/ComplaintCreated each with consumer and personal+project push, mark as read invalidation, flags/me single call 5m silent, maintenance 5m X-Silent.

RESTRICTIONS (Must Obey Until Proceed):
- One task at a time, permission before coding, professional log per task 8-section + Progress Overview
- Never hardcode roles, no internal CSS, OnPush 100%, TanStack 5m silent for me/flags, YARP Order 0 specific, same keys Local/Prod, no Raw/LineNumber
- SuperAdmin never sees customer data, only aggregate; credentials only via config
- Hard delete for member removal when no remaining orgs, email reusable

CURRENT STATUS (2026-09-15, 63/64):
- Done: All phases 0-8 + R 7/7 + N 7/7 + 10 debug fixes + int Role + hard delete + per-workspace by id + silent polling.
- Pending: Only 5.4 Deploy — MonsterASP.net 5 sites + DB + Vercel. Manual Plesk App Settings (5 keys + SuperAdmin) for Identity site, then dotnet publish + ef database update + health Scalar.

Reply 'Proceed with 5.4 Deploy' to continue. I will not write/edit any code until you confirm.
```

CURRENT STATUS (2026-09-15):
- Done: 63/64, only deploy remains.

Reply 'Proceed with 5.4 Deploy' to continue. I will not write/edit any code until you confirm.

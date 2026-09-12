# FlowBoard — Universal Prompt for New Session (2026-09-12 - Post Phase 4 + 5.1 + SuperAdmin 10 Tasks)

> Copy-paste the entire code block below into any new `opencode` chat to resume with full context. Works at any progress (now 29/40 + 10 SuperAdmin pending = 39/50). No Session ID needed — files are the session.

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME (MNC-GRADE — STRICT — NEVER SIMPLICITY):
1. Read in order: Documents/FlowBoard_System_Design.docx v1.3 + Appendix X Company-Centric + Appendix Boards/Sprints/Environments/Statuses (Jira company-managed), Documents/FlowBoard_Tasks_Plan.docx v1.3 (7 Phases 40 Tasks ~110h — Phase 4 expanded 4.1-4.5 Charts 18h, Phase 5 5.1-5.4 Polish 13h + SuperAdmin SA.1-SA.10 32h → total 50 Tasks) + SuperAdmin module location single module in Identity.Service (Application/SuperAdmin + Infrastructure/SuperAdmin + Api/Controllers/SuperAdminController, frontend features/superadmin/superadmin-layout) not separate service not distributed, Documents/FlowBoard_Redis_Caching_Guide.docx v1.0, Documents/FlowBoard_Architecture_Rules.md (STRICT — includes Frontend 3-File Rule + Section 9 Permissions CRUD + Section 10 Human Error Rule (never expose Raw/LineNumber/stack, simple human error, keep Retry-After, description optional title-only 2048 tokens) + Roles single-source compact 0-3 + CorrelationId Flow + Serilog per service), TASK_LOG.md (29/40: Phase0 5/5, Phase1 5/5, Phase2 5/5, Phase4 5/5 Completed (4.1 File Cloudinary done, 4.2 Attachments Jira done, 4.3 Org 6 KPIs+5 charts with TotalTokens fix, 4.4 Project 5 KPIs+burndown+5 charts shared burndown, 4.5 Sprints selector+burndown+stats), Phase5 1/5 Completed (5.1 YARP Sliding Window Counter 60 IP/100 User via Lua atomic + Serilog JSON daily 30d with claim/route-only enrichment + Scalar + ForwardedHeaders + HSTS/CSP), Phase6 5/5, Phase7 7/7 Completed (7.1 Infra 3h + 7.2 Draft + 7.3 Enhance + 7.4 AcceptanceCriteriaJson 2000 + 7.5 Breakdown + 7.6 Org AI Usage + 7.7 Project AI Usage), SuperAdmin SA.1-SA.10 Pending 10 tasks (Dashboard, Organizations with Free badge, Users/Accounts, Subscriptions & Billing simplified mock with Free type, Platform Activity, System, Support mock, Feature Flags, AI Usage, Settings) single module in Identity.Service + frontend features/superadmin — Phase3 0/3 deferred, Phase5 5.2-5.4 pending), SESSION_RESUME.md, Documents/Prompt_For_New_Session.md (this file), Documents/Credentials.md (superadmin@flowboard.local / Super666@lmp), Documents/Future_Tasks.md (PDF proxy + left part stale), Documents/Postman/FlowBoard_Auth_6Roles.postman_collection.json + FlowBoard_Project_2_5.postman_collection.json
2. Check TASK_LOG.md Progress Overview (29/40 + SA.1-10 Pending). Find FIRST Pending/InProgress. Next is SuperAdmin SA.1 Dashboard after Phase 5.1, then SA.2 Organizations (Free badge), SA.3 Users, SA.4 Subscriptions Billing with Free type, SA.5 Platform Activity, SA.6 System, SA.7 Support, SA.8 Feature Flags, SA.9 AI Usage, SA.10 Settings, then Phase 5.2 Tests 70%, 5.3 Docs, 5.4 Deploy. Admin deferred after SuperAdmin.
3. Read SA.1-SA.10 task details from TASK_LOG (Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies) and study what we have done vs remains. Subscription types: MNC-grade table + enum hybrid - SubscriptionPlan enum Free=0 Pro=1 Business=2 Enterprise=3 + SubscriptionPlans table (Id, Name, Price, MaxUsers, MaxWorkspaces, MaxProjects, StorageGB, AiRequests, ApiLimit, FeaturesJson) seeded Free/Pro/Business/Enterprise, Organizations has SubscriptionPlanId FK default Free on Register via CreateOrganizationAsync.
4. Summarize: "Found 29/40 + 10 SA Pending (Phase 5.1 Sliding Window Counter Lua atomic via Upstash + Serilog daily 30d + Scalar + ForwardedHeaders + Correlation N format, Phase 4 5/5 done). Next is SA.1 Dashboard 4h + SA.2 Organizations 4h Free badge + SA.3 Users 3h + SA.4 Subscriptions 5h Free type in table + SA.5-10 ...". Ready to start?
5. ASK PERMISSION: "Do you want me to start SuperAdmin SA.1? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after Proceed, code one task at a time, then update TASK_LOG.md 8-section + Progress Overview. After SA.1-10 + 5.2-5.4, project done.

PHASE 5.1 COMPLETE (2026-09-12 - Sliding Window Counter + Serilog + Scalar):
[✓] Gateway YARP Sliding Window Counter 60 IP / 100 User via Upstash Redis Lua atomic (keys rl:ip/user:{id}:{bucket} Unix bucket, 2 buckets weighted prev*(1-elapsed/60)+cur, no increment on reject, DI IConnectionMultiplexer singleton, ForwardedHeaders, UseForwardedHeaders)
[✓] CorrelationId Angular crypto.randomUUID -> X-Correlation-Id N format (validate >100), Gateway CorrelationIdMiddleware before SerilogRequestLogging, Serilog per service logs/log-YYYY-MM-DD.json JSON daily rolling 30d 10MB, enrichment CorrelationId/UserId/OrganizationId/WorkspaceId/ProjectId from JWT claims organization_id/workspace_id/sub or route values only, no DB lookup, 5 services separate files, CorrelationId search reconstructs request
[✓] Serilog per service Gateway/logs, Identity/logs, Project/logs separate, retained 30d, rollOnFileSize, Console + File
[✓] Scalar 2.0 + OpenAPI at Gateway /scalar BluePlanet + /openapi/v1.json, health /health aggregated, CORS http://localhost:4200 + https://flowboard.vercel.app, HSTS, CSP default-src self, X-Content-Type-Options nosniff, X-Frame-Options DENY, Referrer-Policy, root endpoint no internal details

PHASE 4 COMPLETE (2026-09-12):
[✓] 4.1 File.Service Cloudinary
[✓] 4.2 Attachments UI
[✓] 4.3 Org 6 KPIs + 5 charts with TotalTokens fix (AiUsageLogs TotalTokens)
[✓] 4.4 Project 5 KPIs + burndown shared BurndownComponent + 5 charts Status/Type/Priority/Assignee+Unassigned/Velocity
[✓] 4.5 Sprints selector + shared burndown + stats row 4 cards, reuse burndown

SUPERADMIN SA.1-SA.10 PENDING (MNC least privilege, no customer data browse):
- SA.1 Dashboard Global KPIs Organizations/Users/Active/Users/Workspaces/Projects/Storage + health 8 services + 6 growth charts mock
- SA.2 Organizations lightweight 8 cols Organization/Owner/Plan/Status/Users/Workspaces/Projects/Created with Plan badge Free visible counts only
- SA.3 Users global Accounts User/Email/Status/Org Count/Created/LastLogin actions Suspend/Reactivate
- SA.4 Subscriptions & Billing simplified mock Overview MRR/ARR etc + Subscriptions table Organization/Plan/Billing Cycle/Status/Amount/Renwal with Free type row + Plans Free/Pro/Business/Enterprise limits - Free default for every new org via enum+table
- SA.5 Platform Activity audit only Organization Created/Suspended etc Time/Actor/Action/Resource/Result
- SA.6 System Services/DB/Redis/RabbitMQ/SignalR/File Storage
- SA.7 Support/Impersonation mock with Reason/Expiration/Audit
- SA.8 Feature Flags Global + org overrides
- SA.9 AI Platform Usage aggregate Tokens/Cost per org/provider/model, no prompts
- SA.10 Settings platform General/Security/Authentication/Email/AI/Storage/Rate Limits/Maintenance

SUBSCRIPTION DESIGN (MNC-GRADE):
- Enum SubscriptionPlan { Free=0, Pro=1, Business=2, Enterprise=3 } in SharedKernel + table SubscriptionPlans (Id, Name, Price, MaxUsers, MaxWorkspaces, MaxProjects, StorageGB, AiRequests, ApiLimit, FeaturesJson) seeded Free/Pro/Business/Enterprise, Organizations has SubscriptionPlanId FK default Free on CreateOrganizationAsync via RegisterCommand, future payment will update PlanId, mock menu/detail pages show Free badge for every new org.

FUTURE: SA.1-10 then Phase 5.2 Tests 70% (must include SuperAdmin + rate limit mock + Serilog in-memory) → 5.3 README/Postman/Lighthouse → 5.4 MonsterASP.net + Vercel same keys, Admin deferred

CURRENT DB (flowboard, same keys local/prod):
- [identity] 9 tables: Users, Organizations (BioTech 2ced) + SubscriptionPlans (new) + PlanId FK, Workspaces, WorkspaceMembers(Role 0-3 + CustomRoleId), OrganizationMembers(1/2/3), OrganizationWorkspaceRoles, Permissions(35), RolePermissions, OrganizationActivities, RefreshTokens — HasDefaultSchema identity
- [project] 17 tables: Projects, Boards, BoardLists, Statuses, BoardColumnStatuses, Tasks(ListId/StatusId nullable + AcceptanceCriteriaJson 2000), SubTasks, Comments, ActivityLogs(WorkspaceId filled), Sprints, Teams, ProjectMembers, Environments, OutboxMessages, AiUsageLogs(22 cols OrgId filled) — HasDefaultSchema project
- [file] Attachments, OutboxMessages — HasDefaultSchema file
- [notification] Notifications

PROJECT CONTEXT:
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard backend/ + frontend/ siblings
- Stack: Angular 22.1.5 Standalone + TS 6.0 + Tailwind 3.4.17 + DaisyUI 4.12.14 (6 themes) + TanStack Query 5.62 experimental + Signals + CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 / SignalR 10.0, Backend .NET10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + FluentValidation 11.10 + MassTransit 8.3 + Upstash rediss:// + CloudAMQP amqps:// + Cloudinary + Brevo + Gemini 3.5 Flash + Groq hidden disabled + Serilog 9.0 + Scalar 2.0
- YARP: Sliding Window Counter 60 IP/100 User Lua atomic via Upstash, ForwardedHeaders, CorrelationId N format, Serilog daily 30d per service, Scalar /scalar BluePlanet, health /health, Order 0 specific routes
- Roles compact 0-3 + SuperAdmin 0 via IsSuperAdmin + WorkspaceMembers Role 5 for SuperAdmin org - SuperAdmin portal uses superAdminGuard (SuperAdmin only), OrgAdmin uses orgAdminGuard - separate sidebars, least privilege
- Subscription: Free default for every new org, enum+table hybrid MNC-grade, mock billing UI shows Free badge

MNC RULES (never break):
- Clean DIP IApplicationDbContext/I*Service + ICommand/Query/Service: Controller IMediator only → Command/Query Validator → Handler I*Service → Infrastructure EF+cache+Brevo — never _db in controller/handler
- Frontend 3-File Rule: every component folder exactly 3 files html+ts+css (css empty), ts templateUrl only, CommonModule OnPush
- Signals input/computed OnPush firstValueFrom, shared roles single source compact 0-3, Redis CacheKeys + ICacheableRequest, Responsive p-3 sm:p-4, HasDefaultSchema, Ignore DomainEvents, workspaceId+Role claims, MassTransit fanout
- Same keys Local/Prod, YARP Order 0 specific, AiUsageLogs separate AI folder DIP ready to extract, not store full Prompt/ResponseJson only hash/preview + Operation/Provider/Model/Tokens/Cost/Ids/Status/FailureReason
- Section 9 Permissions CRUD: Every new entity 4 perms, Section 10 Human Error: never expose Raw/LineNumber/stack simple human error keep Retry-After, description optional title-only 2048 tokens
- Keep existing dashboards: Organization and Project dashboards keep current content, only add new KPI + chart sections below (no removals)
- Sliding Window Counter not Fixed Window Log, Counters low memory, Lua atomic no increment on reject, ForwardedHeaders trusted proxy, DI IConnectionMultiplexer singleton, Unix bucket
- CorrelationId X-Correlation-Id N format validate >100, before SerilogRequestLogging, enrichment from JWT organization_id/workspace_id claims or route values only, no DB lookup, per service logs/log-YYYY-MM-DD.json daily 30d separate files, SuperAdmin Platform Activity is audit not Serilog
- Subscription MNC-grade enum+table hybrid with Free default, mock billing shows Free type, never show raw API keys

RESTRICTIONS:
- One task at a time, permission before coding, professional log per task 8-section + Progress Overview
- Never simplicity even if boilerplate increases
- Strict manual: no auto statuses/columns/tasks
- UI placement: Org Dashboard below existing (KPI 6 + 5 charts), Project Overview below existing (KPI 5 + burndown + 2x2 grid + velocity), Sprints top selector + burndown + stats row, SuperAdmin dedicated /superadmin layout with 10 items similar to org sidebar
- Least privilege: SuperAdmin never sees customer member lists/project lists/issues/boards/sprints/comments/activity/prompts, only aggregate counts, health, audit, billing, feature flags

Reply 'Proceed SA.1' to continue SuperAdmin Dashboard. I will not write/edit any code until you confirm.
```

(End of file)

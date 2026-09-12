# FlowBoard — Universal Prompt for New Session (2026-09-12 - Post Activity Workspace Fix + Charts Expanded 4.3-4.5)

> Copy-paste the entire code block below into any new `opencode` chat to resume with full context. Works at any progress (now 25/40, Phase 7 done, Phase 4.3-4.5 charts next). No Session ID needed — files are the session.

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME (MNC-GRADE — STRICT — NEVER SIMPLICITY):
1. Read in order: Documents/FlowBoard_System_Design.docx v1.3 + Appendix X Company-Centric + Appendix Boards/Sprints/Environments/Statuses (Jira company-managed), Documents/FlowBoard_Tasks_Plan.docx v1.3 (7 Phases 40 Tasks ~110h — Phase 4 expanded 4.1-4.5 Charts 18h, Phase 7 AI 7.1-7.7), Documents/FlowBoard_Redis_Caching_Guide.docx v1.0, Documents/FlowBoard_Architecture_Rules.md (STRICT — includes Frontend 3-File Rule + Section 9 Permissions CRUD + Section 10 Human Error Rule (never expose Raw/LineNumber/stack, simple human error, keep Retry-After, description optional title-only 2048 tokens) + Roles single-source compact 0-3), TASK_LOG.md (25/40: Phase0 5/5, Phase1 5/5, Phase2 5/5, Phase4 2/5 (4.1 File Cloudinary done, 4.2 Attachments Jira done, 4.3-4.5 charts pending), Phase6 5/5, Phase7 7/7 Completed (7.1 Infra 3h + 7.2 Draft isDraft Suggested Steps DueDate optional Fibonacci 1-21 + 7.3 Enhance diff pending + 7.4 AcceptanceCriteriaJson 2000 manual+Generate edit ✎ delete 🗑 view hide Generate + 7.5 Breakdown pending subtasks edit ✎ + 7.6 Org AI Usage + 7.7 Project AI Usage paginator 10 enriched Caller/Project/Workspace/CustomRole), Phase3 0/3 code complete deferred, Phase5 0/5 pending — Admin deferred), SESSION_RESUME.md, Documents/Prompt_For_New_Session.md, Documents/Credentials.md (superadmin@flowboard.local / Super666@lmp), Documents/Future_Tasks.md (PDF proxy + left part stale), Documents/Postman/FlowBoard_Auth_6Roles.postman_collection.json + FlowBoard_Project_2_5.postman_collection.json
2. Check TASK_LOG.md Progress Overview (25/40, Phase 4 In Progress). Find FIRST Pending/InProgress. Next is Phase 4.3 Organization Dashboard KPIs + Charts (Org Health — 6 KPIs + 5 ApexCharts) after Phase 7, then 4.4 Project Overview, 4.5 Sprints Detailed, then Phase 5 Polish 5.1-5.5 MNC-grade, Admin deferred.
3. Read Phase 4.3 + 4.4 + 4.5 tasks details from Tasks_Plan (Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies) and study what we have done up to now vs what remains.
4. Summarize: "Found 25/40 (Phase 7 AI Completed 7/7 → 7.6+7.7 paginator 10 enriched Org 20 rows backfill 2ced + 7.4 view hide Generate + 7.5 pending edit ✎, Phase 4 expanded 4.3-4.5 charts pending — Org 6 KPIs + 5 charts, Project 5 KPIs + 6 charts + Burndown, Sprints selector + stats). Next is Phase 4.3 Organization Dashboard 5h + 4.4 Project Overview 5h + 4.5 Sprints 3h (keep existing dashboards, only add below), then Phase 5 Polish 5.1-5.5, Admin deferred. Ready to start?"
5. ASK PERMISSION: "Do you want me to start Task 4.3? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after Proceed, code one task at a time, then update TASK_LOG.md 8-section (Why/What Used/Why Useful/What It Does/Achieved/Future Help) + Progress Overview. After Phase 4.3-4.5 + testing, move to Phase 5 Polish (5.1-5.5) MNC-grade, Admin deferred.

PHASE 7 COMPLETE (2026-09-11):
[✓] 7.1 AI Infra + AiUsageLogs (Gemini 3.5 Flash fixed env-fallback + Groq hidden display:none disabled backend 400 keep code, Redis 3/min per ai:{userId}:{model} 5 RPM global, AiUsageLogs 22 cols hash/preview 500 hash/preview only, separate AI folder Project.Service/Application/AI + Infrastructure/AI DIP, migration 20260911154735_AddAiLogs, YARP ai-route /api/ai/{**catch-all} → :5002)
[✓] 7.2 Draft (A) Issues header ✨ AI Draft prompt 10-500 model Gemini 3.5 Flash fixed (Groq hidden) POST /api/ai/draft → JSON {title,description,checklist→Suggested Steps,labels,priority,issueType,storyPoints} preview isDraft editable Suggested Steps + DueDate optional + Fibonacci 1-21 both modals pending until Create POST /tasks Backlog
[✓] 7.3 Enhance (B) task-detail Description ✨ Enhance (hidden readOnly) POST /api/ai/enhance {title,description} → diff Current vs AI Apply pending until Save PUT, description optional title-only 2048 tokens, Human Error Rule Sec10 hide Raw/LineNumber
[✓] 7.4 AcceptanceCriteriaJson 2000 nullable card under Description manual Add + ✨ Generate POST /api/ai/criteria → checkbox editable Apply pending until Save PUT, view hide Generate, edit ✎ delete 🗑, TasksController fix save via UpdateTaskCommand acceptanceCriteriaJson
[✓] 7.5 Breakdown (D) Subtasks card ✨ Breakdown (hidden readOnly) POST /api/ai/breakdown → checkbox editable Create Selected pending until Save PUT+POST batch + pending edit ✎ isDirty pending
[✓] 7.6 Org AI Usage Main sidebar ✦ AI Usage OrgAdmin only GET /api/ai/usage?orgId + summary GROUP BY tokens/cost model selector paginator 10 enriched Project/Workspace/Caller + Custom Role (backfill OrgId 20 rows 2ced)
[✓] 7.7 Project AI Usage Project sidebar ✦ AI Usage all members GET /api/ai/usage?projectId filtered paginator 10 enriched

FIXES (2026-09-11 to 2026-09-12):
- OrgId null fix SqlQueryRaw<Guid?> → OrgRow 4× Commands + backfill 20 rows org 2ced
- Unified Organization Activity GET /api/organizations/{id}/activities?includeProjects=true (default true UI) UNION OrgActivities + project ActivityLogs ORDER BY Occurred desc paginator 10 enriched 6 cols Time/Project/Workspace/Action/Actor — Name + Custom Role + Email/Details 120 without DB change (cross-schema SqlQueryRaw same flowboard DB) — extend existing endpoint keep backward includeProjects=false — COALESCE LEFT JOIN fix for historic NULL WorkspaceId via Projects.WorkspaceId (all SubTask/List/Board/Sprint/Project now show WorkspaceName)
- Project Activity consistent 6 cols paginator 10 — workspaceId fix COALESCE + shared workspaceName, backend ActivityDto WorkspaceId added, Board/Sprint/SubTask/Comment/Project logs now pass WorkspaceId, OrganizationActivityService COALESCE a.WorkspaceId,p.WorkspaceId
- Section 10 Human Error Rule never expose Raw/LineNumber/stack simple human error keep Retry-After
- Frontend 3-File Rule enforced, Signals input/computed OnPush firstValueFrom, paginator 10 enriched

PHASE 4 EXPANDED (2026-09-12 — charts two-level, keep existing dashboards only add below):
- 4.1 File.Service Cloudinary — [file].Attachments + OutboxMessages migration 20260910155026_InitialFile, CloudinaryDotNet 1.27.2 Image/Video/RawUploadParams per contentType (image w_300 eager, video, raw for doc/zip), folder flowboard/{org:8}/{ws:8}/{proj:8}/{task:8}/safeFileName:40 (public_id <255, fixes VS break), JWT HS256 15m, MassTransit fanout flowboard.events, Outbox 2s, HasDefaultSchema file, YARP file-attachments-route Order0 /api/tasks/{id}/attachments → :5003
- 4.2 Attachments UI Jira — attachment.service.ts getAttachments/upload/delete + getThumbUrl f_auto,q_auto,w_300/w_600 + download via fetch blob (for all types, fixes pdf 400 fl_attachment), getPreviewType image/video/pdf/office/archive/text, formatSize, task-detail-modal Attachments tab Grid/List toggle + All/Images/Videos/Documents filter + + Add (hidden when readOnly) + per-card Download (fetch) + Delete (OrgAdmin/SuperAdmin/uploader) + preview modal image/video/pdf(Download fallback, no iframe blank side text, no View original) + sanitizer getSafeUrl, close disabled while uploading
- 4.3 Organization Dashboard KPIs + Charts 5h Pending — Keep existing dashboard, add below: 6 KPI cards Total Workspaces/Projects/Members/Issues/Active Sprints/Completed Issues + 5 ApexCharts: Issues by Status Donut, Issues by Priority Bar, Issues by Workspace Bar, Activity Trend Line (14d), AI Usage Line/Bar (reuse AiUsageLogs summary) — Backend GET /api/organizations/{orgId}/stats + /chart-data via IOrganizationStatsService GROUP BY status/priority/workspace, cached 2m, Frontend dashboard.component + shared/charts/org-charts.component (3-file) + stats.service
- 4.4 Project Overview KPIs + Charts 5h Pending — Keep existing overview, add below: 5 KPI cards Issues/Completed/In Progress/Story Points/Active Sprint + 6 ApexCharts: Sprint Burndown Line/Area (shared, Remaining work from ActivityLogs + Sprints dates), Issues by Status Donut, Issues by Type Donut/Bar (Bug/Story/Task), Priority Bar, Assignee Workload Bar, Sprint Velocity Bar (story points per sprint) — Backend GET /api/projects/{projectId}/stats + /burndown + /chart-data via IProjectStatsService, Frontend overview.component + shared/charts/burndown.component (3-file, reused in Sprints) + project-charts.component
- 4.5 Sprints Page Detailed 3h Pending — Keep sprints list, add top: Sprint selector Active ▼ + shared burndown Total vs Remaining + stats row [Issues] [SP] [Completed] [Remaining] — Reuse burndown.component with sprintId input, TanStack queryKey ['burndown', sprintId]

FUTURE: Phase 4.3-4.5 charts above pending — after Phase 4 → Phase 5 Polish 5.1-5.5 (Rate limit 60/min + Serilog + Scalar, tests 70%, README/Postman/Lighthouse 100%, MonsterASP.net + Vercel) Admin deferred — PDF proxy + left part stale (Future_Tasks.md)

CURRENT DB (flowboard, same keys local/prod):
- [identity] 9 tables: Users, Organizations (BioTech 2ced), Workspaces (Development dec09265, General 745ea9fe), WorkspaceMembers(Role 0-3 + CustomRoleId), OrganizationMembers(1/2/3), OrganizationWorkspaceRoles, Permissions(35), RolePermissions, OrganizationActivities(5 org + N project merged), RefreshTokens — HasDefaultSchema identity, Ignore DomainEvents
- [project] 17 tables: Projects, Boards, BoardLists, Statuses, BoardColumnStatuses, Tasks(ListId/StatusId nullable + AcceptanceCriteriaJson 2000 nullable), SubTasks, Comments, ActivityLogs(WorkspaceId filled, backfill via COALESCE, AiUsageLogs), Sprints, Teams, ProjectMembers, Environments, OutboxMessages, AiUsageLogs(22 cols OrgId now filled) — HasDefaultSchema project
- [file] Attachments, OutboxMessages — HasDefaultSchema file
- [notification] Notifications

PROJECT CONTEXT:
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard backend/ + frontend/ siblings
- Stack: Angular 22.1.5 Standalone + TS 6.0 + Tailwind 3.4.17 + DaisyUI 4.12.14 (6 themes) + TanStack Query 5.62 experimental + Signals + CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 / SignalR 10.0, Backend .NET10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + FluentValidation 11.10 + MassTransit 8.3 + Upstash rediss:// + CloudAMQP amqps:// + Cloudinary + Brevo + Gemini 3.5 Flash (ApiKey rotated AQ.Ab8...) + Groq llama-3.1-8b hidden disabled (code kept)
- YARP Order 0: /api/ai/{**catch-all} → project :5002, /api/tasks/{taskId}/attachments Order0 → file :5003, /api/tasks/{**} Order1 → project :5002, etc., identity :5001, file :5003, notification :5004
- Roles compact 0-3 single source frontend shared/constants/roles.ts + backend SharedKernel/Roles.cs — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3 — Workspace custom via OrganizationWorkspaceRoles + WorkspaceMembers.CustomRoleId — Client view+comment+attach only (CanUpload false)

MNC RULES (never break):
- Clean DIP IApplicationDbContext/I*Service + ICommand/Query/Service: Controller IMediator only → Command/Query Validator → Handler I*Service → Infrastructure EF+cache+Brevo — never _db in controller/handler
- Frontend 3-File Rule: every component folder exactly 3 files html+ts+css (css empty), ts templateUrl only, CommonModule OnPush
- Signals input/computed OnPush firstValueFrom, shared roles single source compact 0-3, Redis CacheKeys + ICacheableRequest, Responsive p-3 sm:p-4, HasDefaultSchema, Ignore DomainEvents, workspaceId+Role claims, MassTransit fanout
- Same keys Local/Prod, YARP Order 0 specific, AiUsageLogs separate AI folder in Project.Service not new microservice (but DIP ready to extract), not store full Prompt/ResponseJson only hash/preview + Operation/Provider/Model/Tokens/Cost/Ids/Status/FailureReason
- Section 9 Permissions CRUD: Every new entity 4 perms, Section 10 Human Error: never expose Raw/LineNumber/stack simple human error keep Retry-After, description optional title-only 2048 tokens
- Keep existing dashboards: Organization and Project dashboards keep current content, only add new KPI + chart sections below (no removals)

RESTRICTIONS:
- One task at a time, permission before coding, professional log per task 8-section + Progress Overview
- Never simplicity even if boilerplate increases
- Strict manual: no auto statuses/columns/tasks
- UI placement: Org Dashboard below existing (KPI 6 + 5 charts), Project Overview below existing (KPI 5 + burndown + 2x2 grid + velocity), Sprints top selector + burndown + stats row

Reply 'Proceed 4.3' to continue Task 4.3 Organization Dashboard. I will not write/edit any code until you confirm.
```

(End of file)

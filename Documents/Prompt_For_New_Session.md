# FlowBoard — Universal Prompt for New Session (2026-09-11 - Post File Attachments + Compact Roles + Board Fixes)

> Copy-paste the entire code block below into any new `opencode` chat to resume with full context. Works at any progress (now 18/38, Phase 4.2 done, Phase 7 AI next). No Session ID needed — files are the session.

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME (MNC-GRADE — STRICT — NEVER SIMPLICITY):
1. Read in order: Documents/FlowBoard_System_Design.docx v1.3 + Appendix X Company-Centric + Appendix Boards/Sprints/Environments/Statuses (Jira company-managed), Documents/FlowBoard_Tasks_Plan.docx v1.3 (7 Phases 38 Tasks ~99h — Phase 7 AI 7.1-7.7), Documents/FlowBoard_Redis_Caching_Guide.docx v1.0, Documents/FlowBoard_Architecture_Rules.md (STRICT — includes Frontend 3-File Rule + Section 9 Permissions CRUD + Roles single-source compact 0-3), TASK_LOG.md (18/38: Phase0 5/5, Phase1 5/5, Phase2 5/5, Phase4 2/3 (4.1-4.2 done, 4.3 ApexCharts pending), Phase6 5/5, Phase3 0/3 code complete, Phase7 0/7 pending, Phase5 0/5 pending — Admin deferred), SESSION_RESUME.md, Documents/Prompt_For_New_Session.md, Documents/Credentials.md (superadmin@flowboard.local / Super666@lmp), Documents/Future_Tasks.md (PDF proxy + left part stale), Documents/Postman/FlowBoard_Auth_6Roles.postman_collection.json + FlowBoard_Project_2_5.postman_collection.json
2. Check TASK_LOG.md Progress Overview (18/38, Phase 7 pending). Find FIRST Pending/InProgress. Next is Phase 7: 7.1 AI Infrastructure + AiUsageLogs (Gemini 2.5 Flash fixed + Groq llama-3.1-8b selectable, Redis 3/min per user, 5 RPM total, AiUsageLogs project.AiLogs, not store full Prompt/ResponseJson only hash/preview, separate AI folder in Project.Service DIP).
3. Read Phase 7 tasks details from Tasks_Plan (Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies) and study what we have done up to now vs what remains.
4. Summarize: "Found 18/38 (Phase 7 AI next: 7.1 Infra → 7.5 Breakdown → 7.6 Org AI Usage → 7.7 Project AI Usage). Phase 4.3 ApexCharts after Phase 7, then Phase 5 Polish, Admin last. Ready to start?"
5. ASK PERMISSION: "Do you want me to start Task 7.1? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after Proceed, code one task at a time, then update TASK_LOG.md 8-section (Why/What Used/Why Useful/What It Does/Achieved/Future Help) + Progress Overview. After Phase 7 + Phase 4.3 + testing, move to Phase 5 Polish (5.1-5.5) MNC-grade, Admin deferred.

PHASE 4 COMPLETE (2026-09-10 to 2026-09-11):
[✓] 4.1 File.Service Cloudinary — [file].Attachments + OutboxMessages migration 20260910155026_InitialFile, CloudinaryDotNet 1.27.2 Image/Video/RawUploadParams per contentType (image w_300 eager, video, raw for doc/zip), folder flowboard/{org:8}/{ws:8}/{proj:8}/{task:8}/safeFileName:40 (public_id <255, fixes VS break), JWT HS256 15m, MassTransit fanout flowboard.events, Outbox 2s, HasDefaultSchema file, YARP file-attachments-route Order0 /api/tasks/{id}/attachments → :5003
[✓] 4.2 Attachments UI Jira — attachment.service.ts getAttachments/upload/delete + getThumbUrl f_auto,q_auto,w_300/w_600 + download via fetch blob (for all types, fixes pdf 400 fl_attachment), getPreviewType image/video/pdf/office/archive/text, formatSize, task-detail-modal Attachments tab Grid/List toggle + All/Images/Videos/Documents filter + + Add (hidden when readOnly) + per-card Download (fetch) + Delete (OrgAdmin/SuperAdmin/uploader) + preview modal image/video/pdf(Download fallback, no iframe blank side text, no View original) + sanitizer getSafeUrl, close disabled while uploading (header X + bottom Close/Save + backdrop), assignee dropdown Name - Custom Role - email (was org•ws), ActivityLog AttachmentUploaded/Deleted via INSERT [project].[ActivityLogs] → History tab

COMPACT ROLES (2026-09-10 — DB dropped/recreated, 35 perms):
- Roles single source BuildingBlocks/SharedKernel/Roles.cs + frontend shared/constants/roles.ts — SuperAdmin 0, Member 1, OrgAdmin 2, Client 3 (was 0/2/3/5 with gaps 1,4 ProjectManager/Viewer now custom via OrganizationWorkspaceRoles). Workspace roles custom only (Developer, Project Manager, Viewer etc. via OrganizationWorkspaceRoles + WorkspaceMembers.CustomRoleId). Frontend auth.service hydrate via SharedRoleMap, backend AuthService JWT now Roles.GetLabel(int) → "OrgAdmin" not "2", Roles.IsPrivilegedForManage handles numeric "2"→"OrgAdmin" via Normalize. Members GetOrgMembers now org-level Role via OrganizationMembers + WorkspaceRoleMap per workspace (Developer etc.) + Full access badge for OrgAdmin, invite/edit hide workspace list when OrgAdmin (full authority), per-workspace update correctly moves CustomRoleId. Project create now OrgAdmin/SuperAdmin via Roles.IsPrivilegedForManage (was ProjectManager) — Prashant OrgAdmin 2 now 201 TES-1.

JIRA REFACTOR + BOARD FIXES (2026-09-10 to 2026-09-11):
- Statuses [project].Statuses + BoardColumnStatuses, Tasks ListId/StatusId nullable, Issues independent → Backlog, Board per-board columns via statusIds, column modal status dropdown multiselect strict + HostListener outside-click + pendingMoveStatus modal for moving status (one status one column, one column many statuses) + backend BoardService Create/Update now RemoveRange alreadyMapped before Add (move), MoveTask now resolves targetStatusIds → newStatusId from BoardColumnStatuses (fixes 400 + double toast), task detail Status id-based, board List View mirrors sprint, Sprint View Board Scrum-guarded, YARP status routes 404 fixed, Tasks PUT listId ""→null, Status rename/delete normalized

PHASE 7 PLAN (next, after 4.2 — 7.1-7.7, then 4.3 Charts, then 5 Polish, Admin last):
- 7.1 AI Infra + AiLogs (3h) — separate AI folder Project.Service/Application/AI + Infrastructure/AI, AiUsageLog columns Id/OrgId/WsId/ProjectId/UserId/TaskId/Operation/Provider/Model/InputTokens/OutputTokens/TotalTokens/Cost/Status/FailureReason/FallbackUsed/DurationMs/CreatedAt (no full Prompt/ResponseJson, only hash/preview), Gemini 2.5 Flash fixed + Groq llama-3.1-8b free selectable via UI radio, Redis 3/min per ai:{userId}:{model}
- 7.2 AI Draft (A) 2.5h — Issues header ✨ AI Draft → modal prompt 10-500 → POST /ai/draft {prompt,model} → JSON {title,description,checklist,labels,priority,issueType,storyPoints} → preview temporary issue detail (no Id isDraft) editable → Create POST /tasks
- 7.3 AI Enhance (B) 2h — task-detail Description ✨ Enhance → POST /tasks/{id}/ai-enhance → diff Current vs AI Apply sets signal pending until Save PUT
- 7.4 AI Criteria (C) 2.5h — task-detail new Acceptance card under Description manual Add + ✨ Generate → POST ai-criteria → checkbox editable Apply pending until Save PUT acceptanceCriteriaJson nullable
- 7.5 AI Breakdown (D) 2.5h — Subtasks card ✨ Breakdown → POST ai-breakdown → subtasks list checkbox editable Create Selected pending until Save PUT + POST subtasks batch (manual Add subtask still immediate)
- 7.6 AI Usage Org Sidebar 1.5h — Main sidebar AI Usage OrgAdmin only GET /api/ai/usage?orgId GROUP BY tokens/cost model selector
- 7.7 AI Usage Project Sidebar 1.5h — Project sidebar AI Usage all members GET /api/ai/usage?projectId filtered

FUTURE: Phase 4.3 ApexCharts Burndown + Brevo (Sprint burndown from ActivityLogs) — after Phase 7
- Phase 5 Polish, Tests, Deploy (MonsterASP.net + Vercel)
- Deferred: PDF proxy GET /api/files/{id}/download (raw 401) + left part stale detail (Future_Tasks.md)

CURRENT DB (flowboard, same keys local/prod, after 2026-09-10 drop):
- [identity] 9 tables: Users, Organizations, Workspaces, WorkspaceMembers(Role 0-3 + CustomRoleId), OrganizationMembers(1/2/3), OrganizationWorkspaceRoles, Permissions(35), RolePermissions, OrganizationActivities, RefreshTokens — HasDefaultSchema identity, Ignore DomainEvents
- [project] 16 tables: Projects, Boards, BoardLists, Statuses, BoardColumnStatuses, Tasks(ListId/StatusId nullable), SubTasks, Comments, ActivityLogs(WorkspaceId, AiUsageLogs pending), Sprints, Teams, ProjectMembers, Environments, OutboxMessages — HasDefaultSchema project
- [file] Attachments, OutboxMessages — HasDefaultSchema file
- [notification] Notifications
- Seed 1 SuperAdmin superadmin@flowboard.local / Super666@lmp (System org, Users.IsSuperAdmin) — new orgs via Register Company Name

PROJECT CONTEXT:
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard backend/ + frontend/ siblings
- Stack: Angular 22.1.5 Standalone + TS 6.0 + Tailwind 3.4.17 + DaisyUI 4.12.14 (6 themes) + TanStack Query 5.62 experimental + Signals + CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 / SignalR 10.0, Backend .NET10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + FluentValidation 11.10 + MassTransit 8.3 + Upstash rediss:// + CloudAMQP amqps:// + Cloudinary + Brevo + Gemini 2.5 Flash 5 RPM + Groq llama-3.1-8b
- Roles compact 0-3 single source frontend shared/constants/roles.ts + backend SharedKernel/Roles.cs, OrgAdmin full authority (workspace list Full access), Workspace custom via OrganizationWorkspaceRoles, Client view+comment+attach only (CanUpload false)
- YARP Order 0: /api/workspaces/{wid}/projects/{**} Order0, /api/tasks/{taskId}/attachments Order0 → file :5003, /api/tasks/{**} Order1 → project :5002, etc., identity :5001, file :5003, notification :5004
- Frontend routes: '',/login,/register, / (Dashboard, W Workspaces, Projects, Activity/Members/Roles/System OrgAdmin + AI Usage org), /w/:wid/p/:pid (Overview, Boards, Board, Backlog, Sprints, Issues, Teams, Members, Statuses, Environments, Activity, Docs, Settings, AI Usage project), /roles, /roles/:id/permissions, /admin deferred
- Shared.Contracts TaskCreatedEvent ListId Guid? nullable, TaskMoved From/To Guid? nullable, Cloudinary Image for pdf public, Video/Raw separation, Outbox fanout flowboard.events, ActivityLogs for AttachmentUploaded/Deleted

CURRENT UI (as of 2026-09-11):
- Layout drawer + main sidebar collapsible, header theme 6 + bell, Register Company Name, Main sidebar AI Usage org-level OrgAdmin only, Project sidebar AI Usage project-level
- Members Org: table Role Member/OrgAdmin (not 1/2) + Workspace Full access for OrgAdmin else badges Development • Developer etc. per WorkspaceRoleMap, Add/Edit hide workspace list when OrgAdmin, per-workspace dropdown only custom
- Task detail: Assignee dropdown Name - Custom Role - email (e.g., Prashant kumar Verma - Developer - prashant@...), Attachments Jira Grid/List + filter All/Images/Videos/Documents + preview Download only (no View original, no side text), close disabled while uploading
- Boards: per-board columns, column modal status dropdown outside-click + move confirm modal (was behind checklist)
- Issues: independent → Backlog, Board is view

MNC RULES (never break):
- Clean DIP IApplicationDbContext/I*Service + ICommand/Query/Service: Controller IMediator only → Command/Query Validator → Handler I*Service → Infrastructure EF+cache+Brevo — never _db in controller/handler
- Frontend 3-File Rule: every component folder exactly 3 files html+ts+css (css empty), ts templateUrl only, CommonModule OnPush
- Signals input/computed OnPush firstValueFrom, shared roles single source compact 0-3, Redis CacheKeys + ICacheableRequest, Responsive p-3 sm:p-4, HasDefaultSchema, Ignore DomainEvents, workspaceId+Role claims, MassTransit fanout
- Same keys Local/Prod, YARP Order 0 specific, AiUsageLogs separate AI folder in Project.Service not new microservice (but DIP ready to extract), not store full Prompt/ResponseJson only hash/preview + Operation/Provider/Model/Tokens/Cost/Ids/Status/FailureReason
- Section 9 Permissions CRUD: Every new entity 4 perms view/create/update/delete (status 27→31→35, attachment 31→35)

RESTRICTIONS:
- One task at a time, permission before coding, professional log per task 8-section + Progress Overview
- Never simplicity even if boilerplate increases
- Strict manual: no auto statuses/columns/tasks

Reply 'Proceed' to continue Task 7.1. I will not write/edit any code until you confirm.
```

(End of file)


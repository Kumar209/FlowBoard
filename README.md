# FlowBoard - Enterprise Project Management SaaS

> Jira/Linear clone - Multi-tenant, Real-time, Event-Driven, Microservices (Modular) on .NET 10 + Angular 22

[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/) [![Angular 22](https://img.shields уровнях.shields.io/badge/Angular-22-DD0031)](https://angular.dev/) [![License](https://img.shields.io/badge/license-MIT-green)](LICENSE) [![Deploy](https://img.shields.io/badge/deploy-Vercel%20%7C%20MonsterASP.net-black)](https://flowboard.vercel.app)

**Live Demo:** Frontend `https://flowboard.vercel.app` (Vercel) | Gateway `https://gateway-xxxxx.monsterasp.net` (MonsterASP.net) - *WIP*

**Stack (100% Free):** Angular 22 Standalone + TypeScript 5.7 + Tailwind 3.4 + DaisyUI 4.12 + TanStack Query 5.62 + Angular Signals + CDK 22 + ng-apexcharts | ASP.NET Core 10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + MassTransit 8.3 + Upstash Redis + CloudAMQP (RabbitMQ) + Cloudinary + Brevo + Gemini 2.5 Flash | MonsterASP.net + Vercel

## Architecture

```
Angular 22 (frontend/flowboard-web) --HTTPS/JWT--> YARP Gateway (.NET10, :80)
  -> Identity.Service :5001 (JWT, 6 Roles, Brevo)
  -> Project.Service  :5002 (CQRS, EF10, Redis, Outbox, Gemini)
  -> File.Service     :5003 (Cloudinary)
  -> Notification.Service :5004 (MassTransit + SignalR 10.0 -> Angular)
Shared SQL Server (MonsterASP.net, 4 schemas) | Upstash Redis | CloudAMQP | Cloudinary | Brevo | Gemini (same keys local/prod)
```

**Repo Structure (sibling - correct enterprise):**
```
FlowBoard/
├── backend/  (.NET 10 sln: Gateway.YARP + BuildingBlocks + 4 Services)
├── frontend/flowboard-web/ (Angular 22 Standalone)
├── Documents/ (System Design v1.2 + Tasks Plan v1.0)
├── TASK_LOG.md (per-task log, one task at a time)
├── SESSION_RESUME.md (resume prompt)
└── .github/workflows/ci.yml (ubuntu-latest)
```

## Roles (6) - Verified Task 1.5

| Role | Can Create Project? | POST /api/workspaces/{wid}/projects | POST /tasks | Invite? | Permissions |
|------|---------------------|--------------------------------------|-------------|---------|-------------|
| SuperAdmin | All | **201** | **201** | Yes | All Orgs/Billing |
| OrgAdmin | Yes (any in workspace) | **201** | **201** | Yes (via Brevo) | Workspace, Members, Any Project |
| ProjectManager | **Yes** (multiple PMs) | **201** (Task 1.5 verified) | **201** | No | Create Projects/Lists/Tasks, Assign, AI Generate |
| Member | No | **403** | **201** | No | View/Comment/Move own Tasks, Upload |
| Client (External) | No | **403** (verified) | **403** (verified) | No | View assigned Projects/Tasks + Comment/Attach only |
| Viewer | No | **403** | **403** | No | View + Export (read-only) |

> **Task 1.5:** `Documents/Postman/FlowBoard_Auth_6Roles.postman_collection.json` (8 invites <300/day Brevo, same key `xkeysib-...` local/prod). PM token -> `POST /api/workspaces/{wid}/projects` **201**, Client token -> `403` Forbidden, Client `POST /tasks` **403** (Member **201**).

## Setup (Local - Same Keys as Prod, Only URLs Differ)

1. Clone: `git clone https://github.com/Kumar209/FlowBoard.git`
2. Fill keys in `backend/*/appsettings.Development.json` (Upstash, CloudAMQP, Cloudinary, Brevo, Gemini - same as prod, you provide)
3. Backend: `dotnet restore backend/FlowBoard.sln && dotnet build`
4. Frontend: `cd frontend/flowboard-web && npm ci && npm run start` (ng serve :4200)
5. Env: `frontend/src/environments/environment.ts` -> `apiUrl http://localhost:5000`, `environment.prod.ts` -> `https://gateway-xxxxx.monsterasp.net` (Vercel var `NG_APP_API_URL`)

## Tasks (Task 1.5 verified 05 Sep 2026)

26 tasks across 6 phases - see `Documents/FlowBoard_Tasks_Plan.docx` and `TASK_LOG.md`. **Postman:** `Documents/Postman/FlowBoard_Auth_6Roles.postman_collection.json` covers 6 roles + Brevo invites + PM 201 vs Client 403. **Swagger:** `http://localhost:5001/swagger` (Identity), `5002/swagger` (Project), `5003/swagger` (File), `5004/swagger` (Notification), `http://localhost:5000/health` (Gateway YARP).

## Docs

- `Documents/FlowBoard_System_Design.docx` v1.2 - Full SDD (Angular 22 + .NET10)
- `Documents/FlowBoard_Tasks_Plan.docx` v1.0 - 26 tasks breakdown
- `TASK_LOG.md` - Living log (Why/What Used/Why Useful/What It Does/Achieved/Future Help)

---
Built as MNC portfolio flagship - responsive (Mobile/Tablet/Laptop/Desktop) via Tailwind + DaisyUI.

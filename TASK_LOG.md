# FlowBoard - TASK LOG

> Enterprise Project Management SaaS (Jira Clone) - Angular 22 + .NET 10 + YARP + TanStack Query + Signals + DaisyUI + Gemini + Brevo + Upstash + CloudAMQP
> **Repo Root:** `X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard`
> **Stack:** `backend/` (.NET 10) + `frontend/` (Angular 22) as siblings | **Deploy:** MonsterASP.net + Vercel | **No Staging** - Local -> Production | **Keys:** Same for Local/Prod (Upstash, CloudAMQP, Cloudinary, Brevo, Gemini - you provide)
> **Rule:** One task at a time. After each task completion, this log is updated with Why / What Used / Why Useful / What It Does / What Achieved / Future Help. No bug-fix noise.

---

## How This Log Works

- Each task corresponds to `Documents/FlowBoard_Tasks_Plan.docx` (Phases 0-5).
- When a task is completed, a new section is appended below in sequential order.
- Each entry summarizes the task's purpose and outcome for MNC interview revision.
- Status: `Pending` -> `In Progress` -> `Completed`

---

## Progress Overview

| Phase | Tasks | Completed | Status |
|-------|-------|-----------|--------|
| Phase 0: Setup & Foundation | 0.1 - 0.5 | 3/5 | In Progress |
| Phase 1: Identity & Auth (6 Roles) | 1.1 - 1.5 | 0/5 | Pending |
| Phase 2: Project Core (CQRS) | 2.1 - 2.5 | 0/5 | Pending |
| Phase 3: Real-time & Messaging | 3.1 - 3.3 | 0/3 | Pending |
| Phase 4: Files, AI & Charts | 4.1 - 4.4 | 0/4 | Pending |
| Phase 5: Polish & Production Deploy | 5.1 - 5.4 | 0/4 | Pending |
| **Total** | **26 Tasks** | **3/26** | **In Progress** |

---

## Task 0.5: TASK_LOG.md + Tasks Plan Docx Creation

**Status:** `Completed` | **Date:** 04 Sep 2026 | **Phase:** 0 - Setup

**Why:**
Before coding, we need a single source of tracking for 26 tasks across 6 phases and a living log that gets updated after each completion. This establishes the "one task at a time" workflow, keeps documentation in sync with `System Design v1.2`, and provides a revision-ready summary for MNC interviews without scattering notes.

**What Used:**
- Markdown (`TASK_LOG.md` at repo root)
- Python-docx (`Documents/FlowBoard_Tasks_Plan.docx` generation)
- System Design v1.2 Phases 0-5 as source of truth
- Folder structure: `backend/` + `frontend/` siblings (outside coupling), `Documents/` for docs

**Why Useful:**
- Gives a clear roadmap from Git init to production deploy - no guessing what next.
- Each TASK_LOG entry will explain Why/What Used/Why Useful/What It Does/Achieved/Future Help - directly usable as interview talking points.
- Tasks Plan docx provides estimated hours, dependencies, and exit criteria for each task - keeps scope controlled and proves planning maturity to MNC reviewers.

**What It Does:**
- Creates `TASK_LOG.md` with overview table, per-phase tracker, and template for future entries.
- Creates `Documents/FlowBoard_Tasks_Plan.docx` with 26 detailed tasks under 6 phases, each with Objective, Key Actions, Deliverables, Exit Criteria, and Hours.

**What Achieved:**
- `TASK_LOG.md` created at `FlowBoard/TASK_LOG.md` with progress table (1/26 Completed) and Task 0.5 entry.
- `FlowBoard_Tasks_Plan.docx` created at `FlowBoard/Documents/FlowBoard_Tasks_Plan.docx` with all 26 tasks structured, version 1.0, same table/indent styling as System Design (0.15" indent, cell margins).

**Future Help:**
- Every upcoming task (0.1 -> 5.4) will append a new section here - by project end this log is a complete build diary for portfolio and interview Q&A ("Tell me how you built FlowBoard").
- Tasks Plan docx will be checked off sequentially - provides burndown visibility and prevents scope creep.

---

<!-- Next tasks will be appended below in order -->

## Task 0.1: Git Init + GitHub + Backend/Frontend Sibling Structure + README

**Status:** `Completed` | **Date:** 04 Sep 2026 | **Phase:** 0 - Setup

**Why:**
The project lacked a version-controlled root with correct sibling structure. Starting with `backend/` + `frontend/` as siblings (not nested) prevents coupling of deploys (MonsterASP.net vs Vercel) and matches enterprise MNC repo layout. Git must be initialized at `FlowBoard/` root before any code, so all future tasks have a single commit history and GitHub as backup/portfolio link for recruiters. This task blocks 0.2-0.3 scaffolds.

**What Used:**
- Git + GitHub (`https://github.com/Kumar209/FlowBoard.git`, branch `main`)
- `.gitignore` (Visual Studio + Angular + Env: bin/, obj/, node_modules/, dist/, .env, appsettings.Development.json)
- `backend/.gitkeep` + `frontend/.gitkeep` as sibling placeholders
- `README.md` with pitch, stack badges, live demo placeholders, architecture snippet, 6-role table, sibling structure
- Git config `user.name`/`user.email` (Prashant Kumar Verma)

**Why Useful:**
- Proves MNC-expected repo hygiene: correct `.gitignore` prevents secrets/bin leak, sibling structure shows you understand separate deploy pipelines (backend to MonsterASP.net via FTP, frontend to Vercel). GitHub link is the first thing recruiters open - a clean README with badges/architecture gives instant credibility. Sets up single source of truth for 26 tasks.

**What It Does:**
- Initializes `git` at `X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard` with remote `origin` to GitHub `Kumar209/FlowBoard`
- Creates `backend/` and `frontend/` empty sibling dirs (kept via `.gitkeep`), `Documents/` already holds SDD v1.2 + Tasks Plan v1.0
- Provides `README.md` with enterprise pitch (Angular 22 + .NET 10), YARP architecture, 6-role matrix (PM can create projects), sibling layout
- Provides `.gitignore` that ignores build artifacts and secret env files (same keys local/prod are not committed)

**What Achieved:**
- `git init` at FlowBoard root, `git remote add origin https://github.com/Kumar209/FlowBoard.git`, initial commit `b800a66 feat: Task 0.1 - Git init + backend/frontend sibling structure + README + docs` with 8 files (`.gitignore`, `README.md`, `Documents/*.docx`, `TASK_LOG.md`, `SESSION_RESUME.md`, `backend/.gitkeep`, `frontend/.gitkeep`)
- `git push -u origin main` succeeded - GitHub `main` now tracks local, `git status` clean
- Folder verified: `FlowBoard/backend/`, `FlowBoard/frontend/`, `FlowBoard/Documents/`, `TASK_LOG.md` (now 2/26)

**Future Help:**
- All next scaffolds (Task 0.2 .NET 10 sln in `backend/`, Task 0.3 Angular 22 in `frontend/flowboard-web`) will be created inside these siblings without nesting - enabling independent `dotnet build` and `ng build` + separate Vercel/MonsterASP.net deploys
- `.gitignore` ensures future `Upstash/CloudAMQP/Cloudinary/Brevo/Gemini` keys in `appsettings.Development.json` are not leaked
- GitHub `main` is ready for PR workflow (`feature/task-0-2-*` -> `main`) and GitHub Actions `ubuntu-latest` CI (Task 5.2)

---

## Task 0.2: Backend Solution Scaffold (.NET 10 + YARP 2.3 + BuildingBlocks)

**Status:** `Completed` | **Date:** 04 Sep 2026 | **Phase:** 0 - Setup

**Why:**
Before business logic, we need a buildable .NET 10 foundation with correct sibling solution layout. This establishes Clean Architecture boundaries (SharedKernel + Shared.Contracts) and YARP Gateway routing skeleton so all 4 future microservices can be added modularly without restructuring. This blocks Identity (1.1) and Project (2.1) domains.

**What Used:**
- .NET 10.0.400 SDK (slnx XML format - new for .NET 10)
- `dotnet new sln` -> `backend/FlowBoard.slnx`, `dotnet new webapi -f net10.0 --no-https` for 4 Services + Gateway
- `BuildingBlocks/SharedKernel` (Classlib net10.0): `BaseEntity`, `DomainEvent`, `Result<T>`, `IAggregateRoot`
- `BuildingBlocks/Shared.Contracts` (Classlib net10.0): `Events/TaskEvents.cs` (`TaskCreatedEvent`, `TaskMovedEvent`, `TaskCommentedEvent`, `FileUploadedEvent` + `IIntegrationEvent`)
- `Yarp.ReverseProxy 2.3.0` via `dotnet add package` (Gateway.YARP)
- `dotnet sln add` + `dotnet add reference` for inter-project references
- New minimal `Program.cs` per service (health endpoints `/health` + `/health/ready` + `/`)

**Why Useful:**
- Proves MNC-expected solution structure: `.slnx` (modern .NET 10), `BuildingBlocks` for shared kernel, Gateway with YARP (Microsoft's official reverse proxy 2.3, not deprecated Ocelot) - interviewers recognize YARP instantly. `BaseEntity` + `Result<T>` pattern is standard for enterprise DDD. `Shared.Contracts` pre-defines integration events so Project -> Notification communication is type-safe from day one.

**What It Does:**
- Creates 7-project solution: `Gateway.YARP` (yarp.json with 11 routes -> Identity/Project/File/Notification clusters at localhost:5001-5004), `SharedKernel`, `Shared.Contracts`, `Services/Identity.Service`, `Project.Service`, `File.Service`, `Notification.Service` (each minimal Web API with CORS for `http://localhost:4200` + `https://flowboard.vercel.app`, health checks)
- `yarp.json` maps `/api/auth/*` -> Identity, `/api/projects/*`/`/api/tasks/*` -> Project, `/api/files/*` -> File, `/api/notifications/*` + `/hubs/*` -> Notification, plus `/health` pass-through
- `Gateway Program.cs` loads `yarp.json` via `AddJsonFile` + `AddReverseProxy().LoadFromConfig()`, CORS, health endpoints, reverse proxy mapping

**What Achieved:**
- `dotnet build backend/FlowBoard.slnx -c Release` -> `Build succeeded 0 Warning(s) 0 Error(s)` - 7 projects restored and compiled, `Gateway.YARP` references YARP 2.3, Services reference SharedKernel/Shared.Contracts correctly
- `backend/.gitkeep` removed (now has content), `backend/` contains `FlowBoard.slnx`, `Gateway.YARP/yarp.json`, `BuildingBlocks/*`, `Services/*`
- Commit `3c0c24d` pushed to `origin/main` (`35 files changed`)

**Future Help:**
- Task 0.3 Angular scaffold will sit in `frontend/flowboard-web` as sibling - keeps `backend/` clean for `dotnet build`
- Task 1.1 will add `Identity.Service/Domain` + `EF Core 10` DbContext onto this skeleton (BaseEntity already provides Id/CreatedAt/UpdatedAt/DomainEvents)
- Task 1.2 will add MediatR handlers that publish `Shared.Contracts.Events` to CloudAMQP via MassTransit
- Task 3.1 will add `MassTransit 8.3` to Project/Notification using the same `Shared.Contracts` events
- YARP will be extended in Task 5.1 with rate limiting + Serilog + CorrelationId - current skeleton already routes to :5001-5004

---

## Task 0.3: Frontend Scaffold (Angular 22 Standalone + Tailwind + DaisyUI + TanStack Query + ApexCharts + SignalR)

**Status:** `Pending` | **Date:** - | **Phase:** 0

*To be updated after completion.*

---

## Task 0.4: Environment Setup (Same Keys Local/Prod - Upstash, CloudAMQP, Cloudinary, Brevo, Gemini)

**Status:** `Pending` | **Date:** - | **Phase:** 0

*To be updated after completion.*

---

## Task 1.1: Identity Domain + EF Core 10 Schema (5 Tables, 6 Roles)

**Status:** `Pending` | **Date:** - | **Phase:** 1

*To be updated after completion.*

---

<!-- Template for future entries - copy and fill -->

<!--
## Task X.Y: Title

**Status:** `Completed` | **Date:** DD MMM YYYY | **Phase:** X

**Why:**
[Reason this task was needed, what problem it solves, why order matters]

**What Used:**
[Technologies, libraries, versions - e.g., ASP.NET Identity 10.0, Angular Signals, Brevo API]

**Why Useful:**
[Industry relevance, MNC interview value, what principle it demonstrates]

**What It Does:**
[Concrete behavior - endpoints, UI, flow]

**What Achieved:**
[Deliverable, metrics - e.g., 5 endpoints working locally, 70% coverage, Lighthouse 100%]

**Future Help:**
[How this unlocks next tasks, reuse in later phases]

---
-->

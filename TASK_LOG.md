# FlowBoard - TASK LOG

> Enterprise Project Management SaaS (Jira Clone) - Angular 22 + .NET 10 + YARP + TanStack Query + Signals + DaisyUI + Gemini + Brevo + Upstash + CloudAMQP
> **Repo Root:** `X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard` | **Stack:** `backend/` (.NET 10) + `frontend/` (Angular 22) siblings | **Deploy:** MonsterASP.net + Vercel | **Env:** Same keys Local/Prod (Upstash, CloudAMQP, Cloudinary, Brevo, Gemini - you provide) | **Rule:** One task at a time, permission before coding, professional log per task

---

## How This Log Works

- **Source:** `Documents/FlowBoard_Tasks_Plan.docx` defines 26 tasks across 6 Phases (0-5). This log is the living execution diary.
- **Flow:** `Pending` -> `In Progress` -> `Completed` (with Date + Commit). Update `Progress Overview` after each completion.
- **Structure:** Every task uses the same 8-section professional template below - designed so any developer (or MNC interviewer) can understand purpose, tech, implementation, verification, and next steps without reading code.
- **Push:** After each completed task, commit log + code and push to `origin/main`.

---

## Progress Overview

| Phase | Task Range | Completed | Status |
|-------|------------|-----------|--------|
| Phase 0: Setup & Foundation | 0.1 - 0.5 | 4/5 | In Progress |
| Phase 1: Identity & Auth (6 Roles) | 1.1 - 1.5 | 0/5 | Pending |
| Phase 2: Project Core (CQRS) | 2.1 - 2.5 | 0/5 | Pending |
| Phase 3: Real-time & Messaging | 3.1 - 3.3 | 0/3 | Pending |
| Phase 4: Files, AI & Charts | 4.1 - 4.4 | 0/4 | Pending |
| Phase 5: Polish & Production Deploy | 5.1 - 5.4 | 0/4 | Pending |
| **Total** | **0.1 - 5.4** | **4/26** | **In Progress** |

---

## Template - Use for Every Future Task (Copy Exactly)

<!--
## Task X.Y: Title

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | DD MMM YYYY | X - Name | hash | Xh | Feature/Chore/Docs |

### 1. Overview
[1-2 line executive summary - what and why in plain language]

### 2. Objectives
- [Objective 1 - measurable deliverable]
- [Objective 2]

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| e.g., Gateway | Yarp.ReverseProxy | 2.3.0 | Routing |

### 4. Implementation Details
[Step-by-step what was done, key decisions, architecture choices. Use bullets or numbered steps.]

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|
| backend/FlowBoard.slnx | Created | Solution file (.NET 10 slnx) |

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|
| Build | Passed | `dotnet build -c Release 0 Warning(s)` |

### 7. Enterprise Relevance (MNC Value)
[Why an MNC interviewer cares, what principle/pattern this proves, how it maps to JD]

### 8. Next Steps & Dependencies
- Unlocks: Task X+1 will build on this by...
- Depends on: Task X-1
- Follow-up: ...

---
-->

---

## Task 0.5: TASK_LOG.md + Tasks Plan Docx Creation

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | 04 Sep 2026 | 0 - Setup | pending docs | 1.5h | Docs |

### 1. Overview
Created the execution tracking system before coding - a markdown log (`TASK_LOG.md`) and a detailed tasks plan docx (`FlowBoard_Tasks_Plan.docx`) that define 26 tasks across 6 phases with objectives, actions, deliverables, and exit criteria.

### 2. Objectives
- Establish single source of tracking for 26 tasks (Phases 0-5) to enable "one task at a time" workflow
- Create `TASK_LOG.md` at repo root with progress table and per-task template
- Generate `Documents/FlowBoard_Tasks_Plan.docx` (v1.0) with 26 detailed task breakdowns

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| Docs | Markdown | - | TASK_LOG.md living log |
| Docs | python-docx | 1.2.0 | Tasks Plan docx generation |
| Source | FlowBoard_System_Design.docx v1.2 | 1.2 | Phases 0-5 as source of truth |

### 4. Implementation Details
- Designed 6 Phases: 0 Setup (5 tasks), 1 Identity (5), 2 Project Core (5), 3 Realtime (3), 4 Files/AI/Charts (4), 5 Polish/Deploy (4) = 26 tasks
- Each task in docx contains: Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies
- Applied same table styling as SDD (tblW 8500, indent 300, margins 2.54cm, cell margins 60/120) for consistency
- Structure: `FlowBoard/Documents/` holds both SDD v1.2 and Tasks Plan v1.0 for portability

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|
| TASK_LOG.md | Created | Progress table (1/26), 6-phase tracker, Task 0.5 entry, template for future tasks |
| Documents/FlowBoard_Tasks_Plan.docx | Created | 58KB, 10 sections, 26 tasks, dependency graph, hourly estimator (85h) |

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|
| File exists | Passed | `TASK_LOG.md` at `FlowBoard/TASK_LOG.md` (5,256 bytes) |
| File exists | Passed | `FlowBoard_Tasks_Plan.docx` at `FlowBoard/Documents/` (58,294 bytes, 30 tables) |
| Structure | Passed | Progress table shows 1/26, Phase 0 In Progress |

### 7. Enterprise Relevance (MNC Value)
Proves planning maturity - MNCs (TCS/Infosys/Accenture) expect SDD + task breakdown before code. The log's structured entries (Overview -> Next Steps) serve directly as interview talking points ("Tell me how you built FlowBoard") without extra preparation. Prevents scope creep across 26 tasks.

### 8. Next Steps & Dependencies
- Unlocks: Task 0.1 Git Init will use this log as commit history baseline; every future task (0.1->5.4) appends here
- Depends on: System Design v1.2 (source)
- Follow-up: Keep log updated sequentially - by project end it is a complete build diary for portfolio

---

## Task 0.1: Git Init + GitHub + Backend/Frontend Sibling Structure + README

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | 04 Sep 2026 | 0 - Setup | b800a66 | 1.5h | Chore |

### 1. Overview
Initialized version control at `FlowBoard/` root with correct sibling folder layout (`backend/` + `frontend/` not nested) and pushed initial docs to GitHub - the foundation for all code tasks.

### 2. Objectives
- `git init` at `X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard` with remote `origin https://github.com/Kumar209/FlowBoard.git` (branch `main`)
- Create `backend/` + `frontend/` sibling directories (via `.gitkeep`) to separate deploy pipelines (MonsterASP.net vs Vercel)
- Add `.gitignore` (Visual Studio + Angular + Env) and `README.md` with enterprise pitch
- Push first commit to GitHub for recruiter visibility

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| VCS | Git + GitHub | - | Version control, portfolio host |
| Env | .gitignore | - | Ignore bin/obj/node_modules/dist/.env/appsettings.Development.json |
| Docs | Markdown (README) | - | Stack badges, architecture, 6-role table |

### 4. Implementation Details
- Ran `git init` in FlowBoard root (not inside backend/frontend), `git remote add origin https://github.com/Kumar209/FlowBoard.git`
- Created `backend/.gitkeep` and `frontend/.gitkeep` as placeholders - ensures `backend/` and `frontend/` are tracked as siblings
- Wrote `.gitignore` covering .NET (`bin/`, `obj/`, `.vs/`), Angular (`node_modules/`, `dist/`, `.angular/`), Secrets (`.env`, `appsettings.Development.json`, `secrets.json`), OS (`.DS_Store`)
- Wrote `README.md` with title, badges (.NET 10, Angular 22), live demo placeholders, YARP architecture snippet, 6-role permission table (PM can create projects), sibling structure diagram
- Configured `user.name`/`user.email` (Prashant Kumar Verma), `git add .gitignore README.md backend frontend Documents TASK_LOG.md SESSION_RESUME.md`

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|
| .git/ | Created | Git repo at FlowBoard root, branch `main` tracking `origin/main` |
| .gitignore | Created | 40 lines - .NET + Angular + Env + OS ignores |
| backend/.gitkeep | Created | Placeholder for sibling structure (later removed when sln created) |
| frontend/.gitkeep | Created | Placeholder (kept until Angular scaffold in 0.3) |
| README.md | Created | Enterprise pitch, stack table, architecture, 6-role matrix, sibling layout |
| Documents/ | Existing | SDD v1.2 + Tasks Plan v1.0 already present, now tracked |

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|
| Git init | Passed | `.git/` at `FlowBoard/`, `git remote -v` shows `origin https://github.com/Kumar209/FlowBoard.git` |
| Commit | Passed | `b800a66 feat: Task 0.1 - Git init + backend/frontend sibling structure + README + docs` (8 files, 302 insertions) |
| Push | Passed | `git push -u origin main` -> `* [new branch] main -> main`, `git status` clean, GitHub `main` up to date |
| Structure | Passed | `FlowBoard/backend/` and `FlowBoard/frontend/` exist as siblings at root |

### 7. Enterprise Relevance (MNC Value)
Repo hygiene is the first MNC filter - a correct `.gitignore` (no bin/secrets leak) and sibling `backend/`/`frontend/` layout proves you understand separate deploy pipelines (backend FTP to MonsterASP.net, frontend Git to Vercel). A clean README with badges/architecture is the first page recruiters open on GitHub and gives instant credibility. This is the single source of truth for 26 tasks.

### 8. Next Steps & Dependencies
- Unlocks: Task 0.2 will create `backend/FlowBoard.slnx` inside `backend/`; Task 0.3 will create `frontend/flowboard-web` (Angular 22) inside `frontend/` without nesting - both rely on this sibling foundation
- Depends on: Task 0.5 (log + plan define why this order)
- Follow-up: `backend/.gitkeep` will be removed when sln is created (0.2); `frontend/.gitkeep` remains until Angular scaffold (0.3); GitHub `main` ready for `feature/* -> main` PR workflow and `ubuntu-latest` CI (Task 5.2)

---

## Task 0.2: Backend Solution Scaffold (.NET 10 + YARP 2.3 + BuildingBlocks)

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | 04 Sep 2026 | 0 - Setup | 3c0c24d | 2h | Feature |

### 1. Overview
Created a buildable .NET 10 foundation - `FlowBoard.slnx` (new slnx XML format), YARP Gateway skeleton, shared kernel/contracts, and 4 minimal microservices - so all future domains can be added modularly without restructuring.

### 2. Objectives
- Generate `backend/FlowBoard.slnx` (.NET 10.0.400) and 7 projects: `Gateway.YARP` (YARP 2.3), `BuildingBlocks/SharedKernel`, `BuildingBlocks/Shared.Contracts`, `Services/Identity.Service`, `Project.Service`, `File.Service`, `Notification.Service` (each `net10.0` webapi `--no-https`)
- Add YARP routing skeleton (`yarp.json` with 11 routes -> :5001-5004 clusters) and inter-project references
- Ensure `dotnet build -c Release` passes with 0 warnings before business logic

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| SDK | .NET SDK | 10.0.400 | Build, slnx XML (new .NET 10) |
| Gateway | Yarp.ReverseProxy | 2.3.0 | Reverse proxy (Microsoft official, not Ocelot) |
| BuildingBlocks | Classlib | net10.0 | SharedKernel + Shared.Contracts |
| Services | ASP.NET Core Web API | net10.0 | 4 minimal services with health checks |

### 4. Implementation Details
- Ran `dotnet new sln -n FlowBoard` at `backend/` -> produced `FlowBoard.slnx` (new XML slnx for .NET 10); `dotnet new webapi -f net10.0 --no-https` for Gateway + 4 Services; `dotnet new classlib -f net10.0` for SharedKernel + Shared.Contracts
- `dotnet sln FlowBoard.slnx add` all 7 projects; `dotnet add reference` SharedKernel to all Services + Gateway, Shared.Contracts to Project + Notification (type-safe events)
- `dotnet add Gateway.YARP package Yarp.ReverseProxy --version 2.3.0` + restore
- Removed default `WeatherForecast*` files; created `BuildingBlocks/SharedKernel/BaseEntity.cs` (Id, CreatedAt, UpdatedAt, DomainEvents), `DomainEvent.cs` (abstract record), `Result.cs` (generic Result pattern), `IAggregateRoot.cs`; created `Shared.Contracts/Events/TaskEvents.cs` (4 records: TaskCreated, TaskMoved, TaskCommented, FileUploaded + IIntegrationEvent)
- Created `Gateway.YARP/yarp.json` with 11 routes (e.g., `/api/auth/{**catch-all}` -> identity-cluster :5001, `/api/projects/{**catch-all}` -> project-cluster :5002) + 4 clusters; updated `Gateway.YARP/Program.cs` to `AddJsonFile("yarp.json")` + `AddReverseProxy().LoadFromConfig()` + CORS (`http://localhost:4200`, `https://flowboard.vercel.app`) + `/health`, `/health/ready`, `/`, `MapReverseProxy()`
- Wrote minimal `Program.cs` for 4 Services (AddEndpointsApiExplorer, HealthChecks, CORS, `/` + `/health` + `/health/ready`)

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|
| backend/FlowBoard.slnx | Created | Solution file (slnx XML, .NET 10) - 7 projects listed |
| backend/Gateway.YARP/Gateway.YARP.csproj | Created | Web API net10.0 + PackageReference YARP 2.3.0 |
| backend/Gateway.YARP/yarp.json | Created | 11 routes + 4 clusters (Identity :5001, Project :5002, File :5003, Notification :5004) |
| backend/Gateway.YARP/Program.cs | Rewritten | YARP load, CORS, health, reverse proxy |
| backend/BuildingBlocks/SharedKernel/ | Created | BaseEntity.cs, DomainEvent.cs, Result.cs, IAggregateRoot.cs |
| backend/BuildingBlocks/Shared.Contracts/ | Created | Shared.Contracts.csproj + Events/TaskEvents.cs |
| backend/Services/Identity.Service/ etc. | Created | 4 Services each with Program.cs (minimal health), appsettings.json, launchSettings.json |
| backend/.gitkeep | Deleted | Removed - backend now has content |

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|
| Restore | Passed | `dotnet restore` for 7 projects succeeded |
| Build Release | Passed | `dotnet build FlowBoard.slnx -c Release` -> `Build succeeded 0 Warning(s) 0 Error(s)` (7 dlls: Gateway.YARP, SharedKernel, Shared.Contracts, 4 Services) |
| Gateway references | Passed | `Gateway.YARP.csproj` has `PackageReference Yarp.ReverseProxy 2.3.0` |
| Inter-project refs | Passed | Identity/Project/File/Notification -> SharedKernel; Project/Notification -> Shared.Contracts |
| Commit | Passed | `3c0c24d feat: Task 0.2 - Backend .NET10 scaffold ... build passing` (35 files, 512 insertions) pushed to `origin/main` |

### 7. Enterprise Relevance (MNC Value)
YARP is Microsoft's official gateway (2.3, not deprecated Ocelot) - MNC .NET interviewers recognize it instantly. `BaseEntity` + `Result<T>` + `DomainEvent` is the standard DDD/Clean Architecture kernel used in Infosys/Accenture enterprise projects. `Shared.Contracts` pre-defines integration events so Project -> Notification communication is type-safe from day one. The `.slnx` XML format proves you are on latest .NET 10. Modular `Gateway` + `BuildingBlocks` + `Services` layout allows adding EF Core, MediatR, MassTransit in later phases without restructuring.

### 8. Next Steps & Dependencies
- Unlocks: Task 0.3 Angular 22 scaffold will sit in `frontend/flowboard-web` as sibling - keeps `backend/` clean for `dotnet build`; Task 1.1 will add `Identity.Service/Domain` + `EF Core 10` DbContext onto `BaseEntity`; Task 1.2 will add MediatR handlers publishing `Shared.Contracts.Events` to CloudAMQP via MassTransit; Task 3.1 will add `MassTransit 8.3` to Project/Notification using same events; Task 5.1 will extend YARP with rate limiting + Serilog + CorrelationId
- Depends on: Task 0.1 (sibling structure must exist before sln)
- Follow-up: `frontend/.gitkeep` remains until Angular scaffold; keep `yarp.json` clusters at `localhost:5001-5004` for local dev (prod will map to `monsterasp.net` URLs in 16.2)

---

## Task 0.3: Frontend Scaffold (Angular 22 Standalone + Tailwind + DaisyUI + TanStack Query + ApexCharts + SignalR)

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | 04 Sep 2026 | 0 - Setup | 05d83f9 | 2h | Feature |

### 1. Overview
Created the Angular 22 standalone frontend foundation with Tailwind + DaisyUI responsive system, TanStack Query for server state, Signals for client state, and enterprise libs (CDK, SignalR, ApexCharts) - buildable and responsive on Mobile/Tablet/Laptop/Desktop.

### 2. Objectives
- `npx @angular/cli@22 new flowboard-web --standalone --routing --style=css` in `frontend/flowboard-web` (Angular 22.1.5 + TypeScript 6.0)
- Install and configure Tailwind CSS 3.4.17 + DaisyUI 4.12.14 (responsive), TanStack Query experimental 5.62.2, SignalR 8.0.7, CDK 22.1.5, ApexCharts 3.49 + ng-apexcharts 1.8.0
- Create sibling structure `frontend/` + `src/app/core|shared|features` + `src/environments` + `vercel.json` for Vercel deploy
- Ensure `ng build --configuration production` passes with DaisyUI themes

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|
| Framework | Angular | 22.1.5 Standalone + TS 6.0.3 | SPA, routing, signals |
| Styling | Tailwind CSS | 3.4.17 | Utility-first, responsive prefixes (md:, lg:) |
| UI | DaisyUI | 4.12.14 | Prebuilt responsive components (btn, card, drawer, 3 themes) |
| Server State | TanStack Query | 5.62.2 experimental | Server cache, optimistic updates, SignalR invalidation |
| Client State | Angular Signals | built-in | Auth, workspace, UI state (no NgRx) |
| Realtime | @microsoft/signalr | 8.0.7 | Board sync (compatible with Server SignalR 10.0) |
| DnD | Angular CDK | 22.1.5 | Kanban drag-drop (Task 3.3) |
| Charts | ng-apexcharts + apexcharts | 1.8.0 + 3.49.0 | Burndown/activity (Phase 4) |
| Node | Node.js | 22.22.3 | Required for Angular 22 (upgraded from 22.18.0 via MSI) |

### 4. Implementation Details
- Upgraded Node.js 22.18.0 -> 22.22.3 via `node-v22.22.3-x64.msi` (Angular 22 requires >=22.22.3, failed first attempt with EBADENGINE)
- Ran `npx @angular/cli@22 new flowboard-web --standalone --routing --style=css --skip-git --skip-install` in `frontend/` (removed `frontend/.gitkeep` placeholder)
- `npm install --legacy-peer-deps` for base (377 packages), then `npm install tailwindcss daisyui @microsoft/signalr` + `npm install @angular/cdk` (22.1.5) + `npm install apexcharts ng-apexcharts` + `npm install @tanstack/angular-query-experimental` (correct package - `@tanstack/angular-query` 404, experimental is Angular version)
- `npx tailwindcss init` -> configured `tailwind.config.js` with `content: ["./src/**/*.{html,ts}"]`, `plugins: [require("daisyui")]`, `daisyui: {themes: ["light","dark","corporate"]}`; updated `src/styles.css` with `@tailwind base/components/utilities` + global font
- Created folder structure: `src/app/core/interceptors|guards|services`, `src/app/shared/components|pipes`, `src/app/features/auth|dashboard|board|list-view|activity|members`, `src/environments` (environment.ts local `http://localhost:5000`/`5004` + environment.prod.ts prod `https://gateway-xxxxx.monsterasp.net`)
- Created `vercel.json` with `rewrites` SPA fallback, `buildCommand npm run build`, `outputDirectory dist/flowboard-web/browser`, `framework angular`, `installCommand npm install --legacy-peer-deps`
- Handled peer conflicts with `--legacy-peer-deps` (lucide-angular 0.511 only supports Angular 13-19 - skipped, will use alternative icons later)

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|
| frontend/flowboard-web/ | Created | Angular 22 standalone app (angular.json, package.json 0.0.0, tsconfig, src/main.ts, etc.) |
| frontend/flowboard-web/tailwind.config.js | Created/Modified | Content globs + daisyui plugin + 3 themes |
| frontend/flowboard-web/src/styles.css | Modified | Added Tailwind directives + global font |
| frontend/flowboard-web/src/environments/environment.ts | Created | Local: apiUrl `http://localhost:5000`, hubUrl `http://localhost:5004/hubs/board` |
| frontend/flowboard-web/src/environments/environment.prod.ts | Created | Prod: apiUrl `https://gateway-xxxxx.monsterasp.net`, hubUrl `https://notify-xxxxx.monsterasp.net/hubs/board` |
| frontend/flowboard-web/vercel.json | Created | Vercel Angular preset, SPA rewrite |
| frontend/flowboard-web/package.json | Modified | Added 7 deps: cdk, signalr, tanstack-experimental, apexcharts, daisyui, tailwindcss |
| Documents/FlowBoard_Backend_Structure_Explained.docx | Created | 45KB backend files explained (Task 0.2 docs, committed together) |
| frontend/.gitkeep | Deleted | Removed - frontend now has content |

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|
| Node version | Passed | `node --version` -> `v22.22.3` (was 22.18.0, upgraded via MSI) |
| Base install | Passed | `npm install --legacy-peer-deps` -> `added 377 packages, found 0 vulnerabilities` |
| Additional | Passed | `tailwind + daisyui + signalr` -> `added 79 packages`; `cdk@22.1.5` -> `added 1`; `apexcharts` -> `added 11` |
| Tailwind init | Passed | `npx tailwindcss init` -> `Created tailwind.config.js` |
| Build prod | Passed | `npx ng build --configuration production` -> `daisyUI 4.12.14 3 themes added` + `Application bundle 229.10 kB (62.36 kB transfer)` -> `dist/flowboard-web/browser` with `main-*.js`, `styles-*.css`, `index.html` |
| Git | Passed | Commit `05d83f9` (29 files, 9891 insertions) pushed to `origin/main`, `git status` clean, `.gitignore` correctly ignored `node_modules/` + `dist/` |

### 7. Enterprise Relevance (MNC Value)
Angular 22 Standalone + Signals is the 2026 MNC standard for .NET shops (Infosys/Accenture use Angular, not React, with .NET 10). Tailwind + DaisyUI gives premium responsive UI with minimal custom CSS - recruiters see polished UI instantly on Mobile/Tablet/Laptop (12-col grid, DaisyUI drawer for mobile sidebar). TanStack Query experimental 5.62 is the modern server-state manager (replaces NgRx Data) - shows you know latest, not legacy NgRx Store. Upgrading Node to 22.22.3 and handling `--legacy-peer-deps` for peer conflicts (lucide-angular, cdk) demonstrates real-world frontend dependency management. Vercel config proves you understand SPA fallback and separate deploy pipelines (frontend Vercel vs backend MonsterASP.net).

### 8. Next Steps & Dependencies
- Unlocks: Task 0.4 will add environment handling for same keys local/prod (Upstash/CloudAMQP/Cloudinary/Brevo/Gemini you provide) using these `environment.ts` files; Task 1.4 will build Angular Auth pages (Signals + TanStack) in `src/app/features/auth` + `core/services/auth.service`; Task 3.3 will use CDK DragDrop for Kanban, Task 4.4 will use ng-apexcharts for burndown
- Depends on: Task 0.2 (backend sibling must exist to keep `backend/` + `frontend/` parallel - now both siblings present)
- Follow-up: `frontend/flowboard-web/node_modules/` and `dist/` are gitignored; keep `tailwind.config.js` content globs in sync when adding new features; TanStack package is `@tanstack/angular-query-experimental` (not `@tanstack/angular-query`) - import from `experimental`

---

## Task 0.4: Environment Setup (Same Keys Local/Prod - Upstash, CloudAMQP, Cloudinary, Brevo, Gemini)

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Pending | - | 0 - Setup | - | 1h | Chore |

*To be updated after completion.*

---

## Task 1.1: Identity Domain + EF Core 10 Schema (5 Tables, 6 Roles)

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Pending | - | 1 - Identity | - | 4h | Feature |

*To be updated after completion.*

---

<!-- Future tasks follow same 8-section template - copy block below -->

<!--
## Task X.Y: Title

| Status | Date | Phase | Commit | Hours | Type |
|--------|------|-------|--------|-------|------|
| Completed | DD MMM YYYY | X - Name | hash | Xh | Feature/Chore/Docs |

### 1. Overview
[1-2 line summary]

### 2. Objectives
- ...

### 3. Technical Stack
| Layer | Technology | Version | Purpose |
|-------|------------|---------|---------|

### 4. Implementation Details
...

### 5. Files & Changes
| Path | Action | Description |
|------|--------|-------------|

### 6. Verification & Results
| Check | Result | Evidence |
|-------|--------|----------|

### 7. Enterprise Relevance (MNC Value)
...

### 8. Next Steps & Dependencies
...

---
-->

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
| Phase 0: Setup & Foundation | 0.1 - 0.5 | 1/5 | In Progress |
| Phase 1: Identity & Auth (6 Roles) | 1.1 - 1.5 | 0/5 | Pending |
| Phase 2: Project Core (CQRS) | 2.1 - 2.5 | 0/5 | Pending |
| Phase 3: Real-time & Messaging | 3.1 - 3.3 | 0/3 | Pending |
| Phase 4: Files, AI & Charts | 4.1 - 4.4 | 0/4 | Pending |
| Phase 5: Polish & Production Deploy | 5.1 - 5.4 | 0/4 | Pending |
| **Total** | **26 Tasks** | **1/26** | **In Progress** |

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

**Status:** `Pending` | **Date:** - | **Phase:** 0

*To be updated after completion.*

---

## Task 0.2: Backend Solution Scaffold (.NET 10 + YARP 2.3 + BuildingBlocks)

**Status:** `Pending` | **Date:** - | **Phase:** 0

*To be updated after completion.*

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

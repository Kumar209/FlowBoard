# FlowBoard - Session Resume Guide

> Use this file to resume this exact project session in a new chat. This chat does NOT have a persistent Session ID that survives app restart - your files ARE the session memory.

## How to Resume (2 steps)

### Step 1: Start OpenCode in your project folder
```powershell
cd "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard"
opencode
```

### Step 2: Paste this UNIVERSAL prompt into the new chat (works at ANY progress):

```
Continue FlowBoard project from "X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard".

UNIVERSAL RESUME INSTRUCTIONS:
1. Read these files in order: Documents/FlowBoard_System_Design.docx v1.2, Documents/FlowBoard_Tasks_Plan.docx v1.0, TASK_LOG.md, SESSION_RESUME.md
2. Check TASK_LOG.md Progress Overview table and all Task sections to determine which tasks are Completed vs Pending - DO NOT assume Task 0.1 is next. Find the FIRST task with Status Pending or In Progress.
3. Read the corresponding task details from FlowBoard_Tasks_Plan.docx (Objective, Key Actions, Deliverables, Exit Criteria, Hours, Dependencies).
4. Summarize: "Found X/26 tasks completed. Next is Task Y.Z: [Title] - [Objective]. Ready to start?"
5. ASK PERMISSION: "Do you want me to start Task Y.Z? Reply 'Proceed' to begin. I will not write/edit any code until you confirm."
6. Only after user replies 'Proceed', start coding Task Y.Z one step at a time, then update TASK_LOG.md with Why / What Used / Why Useful / What It Does / What Achieved / Future Help (no bug noise) and update Progress Overview X/26.
7. Never skip tasks, never do 2 tasks at once, never edit code without explicit Proceed.

Context (constant):
- Folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard with backend/ + frontend/ siblings
- Stack: Angular 22 Standalone + TypeScript 5.7 + Tailwind 3.4.17 + DaisyUI 4.12.14 + TanStack Query 5.62 + Angular Signals + Angular CDK 22 + ng-apexcharts 1.8 + @microsoft/signalr 8.0.7 (JS) / SignalR 10.0 (Server), Backend .NET 10 + YARP 2.3 + EF Core 10 + MediatR 12.4 + MassTransit 8.3 + Upstash Redis + CloudAMQP + Cloudinary + Brevo (300/day) + Gemini 2.5 Flash (15 RPM, 1M TPM, 1500 RPD) - Same keys local/prod, only URLs differ
- 6 Roles: SuperAdmin, OrgAdmin, ProjectManager (CAN create Projects), Member, Client (External view+comment), Viewer - State: TanStack Query + Angular Signals ONLY (Option A)
- No staging - Local (localhost:5000/4200) -> Production (monsterasp.net/vercel.app)
```

## What Was Completed So Far (as of 04 Sep 2026)

- System Design v1.2 at Documents/FlowBoard_System_Design.docx (70,508 bytes, 23 sections, tables fixed)
- Tasks Plan v1.0 at Documents/FlowBoard_Tasks_Plan.docx (58,274 bytes, 26 tasks, tables fixed to 8500 dxa + 2.54cm margins)
- TASK_LOG.md at root (5,256 bytes, Task 0.5 Completed, 1/26 done)
- This SESSION_RESUME.md
- Project folder: X:\Projects + coding\Dot Net\Full Stack Projects\FlowBoard
- Pending keys from you: Upstash Redis URL, CloudAMQP URL, Cloudinary (CloudName/ApiKey/ApiSecret), Brevo ApiKey, Gemini ApiKey (will use same local/prod)

## Why No Session ID?

OpenCode / Muse Spark sessions are in-memory and do not provide a reusable numeric Session ID that survives restart like a database. Your persistent state is the 3 docs + TASK_LOG.md. Any new agent that reads them can resume exactly where we left off - this file is your "Session ID".

## Tip

Keep this file + TASK_LOG.md committed to Git - so even if you reinstall, the resume context is on GitHub.

---
Last Updated: 04 Sep 2026 | Universal Prompt - Asks permission before coding, reads TASK_LOG.md to find next Pending task (works at any progress: 1/26, 10/26, 25/26)
Next on initial creation: Task 0.1 (TASK_LOG shows 1/26 Completed)

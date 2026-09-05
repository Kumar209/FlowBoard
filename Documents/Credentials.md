# FlowBoard — Test Credentials (5 Roles)

> Generated via Gateway :5000 → Identity :5001 invite flow. Same passwords local/prod (only URLs differ). **Do not commit to public repo** — this file is gitignored in production, kept locally for testing.

## How to use
```bash
# Login via Gateway (local)
POST http://localhost:5000/api/auth/login
Body: { "email": "<email>", "password": "<password>" }
# Returns { accessToken, user } + HttpOnly refresh cookie
# Use Bearer token for YARP → Project :5002 etc.
```

## Accounts

| # | Role | Email | Password | Workspace | Can Create Project | Can Create Task | Note |
|---|------|-------|----------|-----------|-------------------|-----------------|------|
| 1 | **OrgAdmin** (2) | `prashantkumarlmp2001@gmail.com` | `Kumar666@lmp` | `Personal Workspace` `609a5462-dbb1-45e4-b345-8884dc7638ce` + `Marketing` `94192290-2f0c-4245-9660-985df5d6a9ae` | ✅ 201 | ✅ 201 | Owner of org `f7aa00b3-c062-49c5-91db-3a2b32e518e0` — can `POST /api/workspaces`, `POST /invite`, `PUT role`, `System` health |
| 2 | **ProjectManager** (1) | `manager@flowboard.local` | `Manager666@lmp` | `Marketing` `94192290-2f0c-4245-9660-985df5d6a9ae` (ProjectManager) + personal `0440f19d-99ac-499d-8d96-ae96764419d9` (OrgAdmin) | ✅ 201 in `Marketing` | ✅ 201 | Invited by OrgAdmin to Marketing — sees only Marketing + personal workspaces |
| 3 | **Member** (0) | `member@flowboard.local` | `Member666@lmp` | `Marketing` `94192290-2f0c-4245-9660-985df5d6a9ae` (Member) + personal `1f90dff6-ca8d-41c4-98cf-9e25d06bcfcd` (OrgAdmin) | ❌ 403 | ✅ 201 | Can create/move tasks, comment — cannot create project |
| 4 | **Client** (3) External | `client@flowboard.local` | `Client666@lmp` | `Marketing` `94192290-2f0c-4245-9660-985df5d6a9ae` (Client) + personal `359f5656-3255-4221-913c-f4f02d7a4737` (OrgAdmin) | ❌ 403 | ❌ 403 (can comment 201) | View + comment only — `POST /tasks` 403 verified |
| 5 | **Viewer** (4) | `viewer@flowboard.local` | `Viewer666@lmp` | `Marketing` `94192290-2f0c-4245-9660-985df5d6a9ae` (Viewer) + personal `985c7ba8-0b30-4da7-b6b2-13bde57870f8` (OrgAdmin) | ❌ 403 | ❌ 403 | View only — no comment, no create |

## Verification (Postman / curl)

**PM can create project 201**
```
POST http://localhost:5000/api/workspaces/94192290-2f0c-4245-9660-985df5d6a9ae/projects
Authorization: Bearer <manager token>
Body: { "name": "PM Test" } -> 201 { key: PMA-5 }
```

**Client/Member/Viewer 403**
```
POST same -> Bearer <client token> -> 403 { error: "Forbidden - Need OrgAdmin/ProjectManager..." }
POST same -> Bearer <viewer token> -> 403
```

**Workspaces filtered**
```
GET http://localhost:5000/api/workspaces
Bearer <manager> -> 2 workspaces (Marketing + personal)
Bearer <member> -> 2
Bearer <client> -> 2
```

## Frontend role UI
- Sidebar `System` only for OrgAdmin/SuperAdmin (`AuthService isOrgAdmin computed`).
- `+ New Workspace` only OrgAdmin (`/w`).
- `+ New Project` only PM/OrgAdmin (`/w/:wid` + `/projects` via modal `app-project-modal`).
- `Add task` hidden for Client/Viewer (`BoardComponent canCreateTask computed`).
- `⋮ Edit/Delete` with `Delete warning modal` → `Cancel/Confirm` + `ToastService`.

## Notes
- All users created via `POST /api/auth/register` then `POST /api/workspaces/94192290.../invite {email, role}` by OrgAdmin (Brevo best-effort).
- `accessToken` 15m + `HttpOnly refreshToken` 7d (`sessionStorage` holds accessToken + memberships for UI, refresh is HttpOnly cookie).
- Marketing workspace `marketing-eaec` slug hash gradient ensures distinct icon for same name.
- To test Client 403 for tasks: `POST http://localhost:5000/api/tasks { projectId, listId, title }` -> Client/Viewer 403.

*Last updated: 2026-09-06 — Gateway :5000, Identity :5001, Project :5002 via YARP Order0*

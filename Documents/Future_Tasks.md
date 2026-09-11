# Future Tasks - Deferred Bugs

## Task: Fix PDF attachment download (raw 401) — deferred after Phase 4

**Status:** Pending (deferred after Phase 4 — 2026-09-10)

**Description:** Old PDFs uploaded as `image` with `public_id` `flowboard/4×36+100` >255 or new `raw` private (`401`) fail `GET https://res.cloudinary.com/.../raw/upload/...pdf` `401` and `fl_attachment` `400`. New short `flowboard/{8}/{8}/{8}/{8}/safeName:40` + `RawUploadParams` fixes new, old remain broken. Need `GET /api/files/{id}/download` proxy — backend `HttpClient` fetch Cloudinary `secure_url` with `ApiSecret` signing, `Content-Disposition: attachment`, bypass `raw` 401 and `fl_attachment` 400, no CORS `fetch` needed. Also handle `GET /api/files/{id}/preview` for `pdf` as `image`.

**Next Steps:** Implement proxy endpoint in `File.Service` with `Cloudinary` signing, add `Download` via `a.download` fallback to proxy if `fetch` fails, run one-off `UPDATE [file].[Attachments] SET Url = REPLACE...` or re-upload.

---

## Task: Fix left part detail data attaching in issue modal

**Status:** Pending (deferred from 2026-09-07)

**Description:** When opening issue detail modal, the left part (title, description, subtasks, activity) sometimes shows stale data due to caching (board query 5m, taskDetailQuery). After updating issue and reopening, it still shows old data. Need to ensure fresh fetch via GET /api/tasks/{id}/detail with staleTime 0 and proper invalidateQueries for ['board', 'task-detail'] on save, and that task input is updated from fresh data, not stale board list.

**Next Steps:** Investigate task-detail-modal.component.ts taskDetailQuery and boardQuery invalidation, ensure effect correctly syncs from fresh data.


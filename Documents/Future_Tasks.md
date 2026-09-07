# Future Tasks - Deferred Bugs

## Task: Fix left part detail data attaching in issue modal

**Status:** Pending (deferred from 2026-09-07)

**Description:** When opening issue detail modal, the left part (title, description, subtasks, activity) sometimes shows stale data due to caching (board query 5m, taskDetailQuery). After updating issue and reopening, it still shows old data. Need to ensure fresh fetch via GET /api/tasks/{id}/detail with staleTime 0 and proper invalidateQueries for ['board', 'task-detail'] on save, and that task input is updated from fresh data, not stale board list.

**Next Steps:** Investigate task-detail-modal.component.ts taskDetailQuery and boardQuery invalidation, ensure effect correctly syncs from fresh data.


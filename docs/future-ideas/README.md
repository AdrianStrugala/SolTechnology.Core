# Future Ideas

Backlog of ideas considered during production adaptation work but **deferred** from current scope.
Each file is a self-contained brief with context, rationale for parking, and conditions to unpark.

## Index

| # | Idea | Status |
|---|---|---|
| [001](001-outbound-webhooks.md) | Outbound webhook delivery | ⏸️ Parked |
| [002](002-priority-background-jobs-hangfire.md) | Priority background jobs (Hangfire) | ⏸️ Parked |
| [003](003-deployment-slot-gating.md) | Deployment-slot gating | ⏸️ Parked |
| [004](004-leader-elected-poller.md) | Leader-elected polling service base | ⏸️ Parked |
| [005](005-medallion-lock-backends.md) | Medallion.Threading Postgres + SqlServer lock backends | ⏸️ Parked |

## How to promote

1. Create `docs/features/YYYY-MM-DD-<kebab-name>.md` with `status: planning`.
2. Remove the row from this index.
3. Reference this idea from the feature's `Context`.


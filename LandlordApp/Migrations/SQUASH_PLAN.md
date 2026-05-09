# Migration squash plan

## Current state (May 2026)
~52 migration files across 11 bounded contexts:
`Applications`, `Analytics`, `Communications`, `Listings`, `Notification`,
`Payments`, `Reviews`, `Roommates`, `SavedSearches`, `SearchRequests`, `Users`.

## Why squash?
- `dotnet ef migrations list` becomes noisy with 50+ entries
- Cold-start migration time grows linearly with migration count
- Designer files for old migrations waste repo space

## How to squash (scheduled maintenance window only)

> ⚠️ Never squash on a live production DB without a full backup and a maintenance window.

### Step-by-step

1. **Take a DB backup.**

2. For each bounded context, generate a new "baseline" migration that captures the
   current schema snapshot:
   ```
   dotnet ef migrations add Baseline_YYYYMMDD \
       --context <Context> \
       --output-dir Migrations/<Folder>
   ```

3. Open the generated `Baseline_YYYYMMDD.cs` and replace its `Up()` body with
   the contents of the context's `*ModelSnapshot.cs` translated to `Create*`
   calls — or use `Script-Migration` to dump the current schema SQL and wrap it
   in a `migrationBuilder.Sql(...)` call.

4. Delete all migration files **older than** the new baseline from the folder.
   Keep the `*ModelSnapshot.cs` file — it is always regenerated in place.

5. In `__EFMigrationsHistory` table, delete all rows and insert a single row:
   ```sql
   DELETE FROM [__EFMigrationsHistory];
   INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
   VALUES ('<timestamp>_Baseline_YYYYMMDD', '<ef-core-version>');
   ```
   Run this as part of the same maintenance-window script.

6. Run `dotnet ef database update` to confirm EF sees the DB as up-to-date.

7. Remove the old migration `.cs` / `.Designer.cs` files from source control.

## Blocked on
- Shared production DB is live 24/7 — squash requires a maintenance window.
- All contexts must be squashed in the same window or `__EFMigrationsHistory`
  will be in an inconsistent state across contexts.

## Target: ≤ 1 migration per bounded context after squash.

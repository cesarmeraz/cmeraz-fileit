# Database deployment

The `FileIt.Database` SQL project is the source of truth for the schema. Every
schema change goes in a `.sql` file under `FileIt.Database\Tables\` (or
appropriate subfolder) and is deployed via DACPAC. Manual `CREATE TABLE` /
`ALTER TABLE` in SSMS is not allowed.

## Workflow

1. Edit or add a `.sql` file under `FileIt.Database\`.
2. Build the DACPAC:
```powershell
   dotnet build .\FileIt.Database\FileIt.Database.sqlproj
```
3. Set the SQL login password in your shell (single quotes matter if the
   password contains `$`):
```powershell
   $env:FILEIT_DB_PASSWORD = 'your_password_here'
```
4. Preview what would change without applying:
```powershell
   .\scripts\deploy-database.ps1 -ReportOnly
```
   Open `db-deploy-report.xml`. Look for `<Alerts />` (no warnings) and review
   any `<Operation>` entries before applying.
5. Apply:
```powershell
   .\scripts\deploy-database.ps1
```

The script is idempotent. Running it against a database already at the desired
state is a no-op.

## Promotion to other environments

Same DACPAC, different connection. Examples:

```powershell
# UAT
.\scripts\deploy-database.ps1 -Server uat-host.database.windows.net -Database FileIt

# Production
.\scripts\deploy-database.ps1 -Server prod-host.database.windows.net -Database FileIt
```

Set `FILEIT_DB_PASSWORD` to the credential appropriate for the target.

## CHECK constraint authoring rule

SQL Server normalizes `CHECK (col IN ('a','b'))` into an OR chain in the
catalog. sqlpackage diffs against the catalog form, so authoring with `IN`
causes the constraint to be dropped and recreated on every publish. Author
CHECK constraints as explicit OR chains in the order SQL Server stores them:

```sql
CONSTRAINT CK_Status CHECK ([Status]='Discarded' OR [Status]='Resolved' OR ...)
```

To find the canonical order, query the catalog:

```sql
SELECT name, definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('dbo.YourTable');
```

## Why DACPAC

The dead-letter table on 2026-04-28 surfaced the gap: `DeadLetterRecord.sql`
existed in source, the build produced a DACPAC, but nothing pushed the DACPAC
to the database. The table had to be created by hand in SSMS. This workflow
closes that gap. Issue #3.

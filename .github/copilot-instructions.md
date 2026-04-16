# GitHub Copilot Instructions for This Project

## User Account Information

- **GitHub username**: `schwartzberg`
- **Azure email**: `paulschwartzberg@outlook.com`
- **Azure subscription**: `2026 Assignments_Paul_Schwartzberg`
- **Azure subscription ID**: `1ba1c9cd-efc8-4259-a0a3-8a64e1ae9482`
- **Azure tenant ID**: `d607a394-7dc0-4c7d-8b9e-a9ed69c728b9`

## General Principles

### When Assisting with This Project

**1. Clarification First, Execution Second**
- Do not make assumptions about requirements, configurations, or desired outcomes.
- Ask clarifying questions when the request is ambiguous or when there are multiple valid approaches.
- Break the HW5 work into small parts and confirm behavior before implementing each part.

**2. Context Gathering Before Code Generation**
- Always read the current HW5 files before changing them.
- Reuse patterns from:
  - `HW4NoteKeeper`
  - `HW4NoteKeeperEx1`
  - `AzureExternalIdEFRazorPagesDemoSolution`
- Treat the assignment PDF/DOCX as orientation only, not as something to implement directly without user direction.

**3. Prompt Tracking Is Mandatory**
- Always update `MyPrompts.md` in this project for every relevant user prompt.
- File location:
  `C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\assignments\05-Assignment\HW5NoteKeeper\MyPrompts.md`
- Keep the entries in chronological order.
- Include the prompt text, context, and the resolution or implementation result when known.

**4. Stop If Progress Stalls**
- If work appears to be looping or a step is taking too long, stop and ask the user for help instead of continuing to thrash.
- Pause periodically during longer implementation stretches so the user can add corrective prompts.
- If the live Azure SQL migration or database update fails, stop, surface the error clearly, and let the user inspect it and suggest the next move before trying risky recovery steps.

**5. Work Collaboratively**
- Treat the user as an active troubleshooting partner, especially for Azure, database, and deployment issues.
- When something operational fails, prefer a short handoff with the concrete error over repeated blind retries.
- On configuration, identity, migration, and environment problems, pause quickly, share the exact blocker, and let the user guide the next step instead of looping through speculative fixes.
- If the user is actively working in Visual Studio, SSMS, or Azure while the task is underway, coordinate with that workflow and avoid duplicating risky reset operations without confirming first.
- Never copy passwords, API keys, or other secrets into source-controlled files, even when they appear in prompts or screenshots.

## HW5 Project Overview

This is an ASP.NET Core 10.0 Razor Pages application that uses:
- Microsoft Entra External ID for sign-in
- Entity Framework Core with Azure SQL
- Managed identity for Azure resource access
- Azure Storage for note attachments
- Azure OpenAI / Foundry-backed tag generation

The app is multi-tenant at the data level:
- Each signed-in user only sees and manages their own notes
- The ownership discriminator is `UserRealmId`
- `UserRealmId` is populated from the signed-in user's Entra object identifier via `GetObjectIdentifier()`

## Reusable Lessons from Earlier Work

### Managed identity and passwordless access
- Prefer `Authentication=Active Directory Default` for Azure SQL connection strings.
- For Azure-hosted execution, restrict `DefaultAzureCredential` to cloud-hosted credentials.
- For local development, scope developer credentials to the expected tenant when possible.

### Storage seeding patterns
- Keep protected Azure-managed containers configurable instead of hardcoding them.
- Treat queue names and similar operational values as configuration.
- Reuse the same attachment seed set and mapping unless the user explicitly changes it.

### Testability patterns
- Keep `Program` public so `WebApplicationFactory` can host the app in tests.
- Prefer a separate test project for verification work.
- Add focused unit tests for seeding logic and only a thin end-to-end trigger test for first-request behavior.

### EF migrations and `Update-Database` from PMC

- `dotnet ef database update` from the CLI **will fail** with a `SELECT permission denied on __EFMigrationsHistory` error when the local CLI identity does not have sufficient Azure SQL rights.
- The correct approach is to run `Update-Database` from the **Visual Studio Package Manager Console** (Tools → NuGet Package Manager → Package Manager Console).
- Before running `Update-Database` from PMC, add the design-time SQL login credentials to **`secrets.json`** (user secrets) so EF tools can authenticate at design time.
  - The connection string in `secrets.json` should use the SQL login (username/password) that has `db_owner` or equivalent rights on the Azure SQL database.
  - Example key in `secrets.json`: `"ConnectionStrings:DefaultConnection"` with a connection string using `User ID=...;Password=...` instead of `Authentication=Active Directory Default`.
  - This secret is **never committed** to source control; it lives only in the local user secrets store.
- In PMC: set **Startup Project** = `HW5NoteKeeper`, set **Default Project** = `HW5NoteKeeper`, then run `Update-Database`.
- After `Update-Database` succeeds, you can revert `secrets.json` to use managed identity for normal app operation.

### Documentation patterns
- Update `ProjectNotes.md` as HW5-specific Azure resources and behaviors are introduced.
- Do not place secrets in source-controlled documentation.

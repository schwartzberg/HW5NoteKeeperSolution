# Project Notes

## 1. Homework Assignment Title and Number

**Assignment #5 – HW5 Multi-Tenant Note Keeper (HW05A)**

---

## 2. Student Information

**Name:** Paul Schwartzberg  
**Email:** paulschwartzberg@outlook.com

---

## 3. Notes for the TA

This is an ASP.NET Core 10.0 Razor Pages application secured with **Microsoft Entra External ID (CIAM)**.

### How to Install, Set Up, and Run

1. **Prerequisites:** .NET 10 SDK, Azure CLI (logged in), access to the Azure subscription `2026_Assignments_Paul_Schwartzberg`.
2. **User Secrets:** The following must be set via `dotnet user-secrets` (or are already configured in the Azure App Service environment variables):
   - `AzureAd:ClientSecret`
   - `AzureOpenAI:ApiKey`
   - `ConnectionStrings:DefaultConnection` (with SQL Auth credentials)
3. **Run locally:** `dotnet run` from the `HW5NoteKeeper` project directory.
4. **Login:** Use the Entra External ID sign-in. On first login for a new user, seed data (notes, tags, and attachments) are automatically created.
5. **Database:** Uses Azure SQL with EF Core. Migrations are applied automatically on startup via `Database.Migrate()`.
6. **Storage:** Uses Azure Blob Storage (`st4hw3`) for note attachments. Blob containers are created per-note (container name = note GUID).

---

## 4. Extra Credit Options Implemented

- **Extra Credit 1 – Attachment Management:** Full implementation of upload, download, delete, and list attachments per note, stored in Azure Blob Storage. Maximum 3 attachments per note. Attachment blobs are seeded on first login for each user. UI is integrated on the Note Details page.

---

## 5. Attribution Regarding the Use of AI

### AI Tools Used
- **GitHub Copilot CLI** (Claude Opus 4.6) — used as an interactive coding assistant throughout development for scaffolding, implementation, debugging, and test authoring.
- Custom instructions (`.github/copilot-instructions.md`) included in the solution.
- All prompts used are recorded in **MyPrompts.md** (included in the solution).

### What AI Assisted With
- Project scaffolding and Razor Pages CRUD generation
- Pattern comparison against HW4 reference implementation
- EF Core concurrency debugging and tag generation refactoring
- Azure Blob Storage service implementation
- Unit and integration test authoring (144 tests)
- XML documentation generation
- Project rename and ProjectNotes.md drafting

### What I Coded by Hand and/or Modified Considerably
- All Azure resource configuration and wiring
- Authentication flow design and Entra External ID CIAM setup
- Final implementation decisions, architectural choices, and deployment
- Manual testing and validation of all features

---

## 6. Microsoft AI Foundry Hub and Project Name

**Microsoft AI Foundry Hub:** `ai-cscie94-foundry`  
**Foundry Project Name:** `note-keeper`

1. **Target URI Azure OpenAI Service:** `https://ai-cscie94-foundry.openai.azure.com/`
2. **Deployment name of gpt-5-mini model:** `gpt-5-mini`

---

## 7. API Key

**Azure OpenAI API Key:** `RRstukpYN6jp0vB8NjBTMrcAeVmOaTtgl0claJJeZk4inGed3Ty6JQQj99CBACfhMk5XJ3w3AAAAAC0GDfnJ`

---

## 8. Azure SQL Database Information

1. **Azure SQL Server Name:** `sql-cscie94-2026-ps`
2. **Azure SQL Database Name:** `sqldb-cscie94-2026_hw5`
3. **URL to Azure SQL Server with Database:**  
   `Server=tcp:sql-cscie94-2026-ps.database.windows.net,1433;Initial Catalog=sqldb-cscie94-2026_hw5;Encrypt=True;TrustServerCertificate=False;`
4. **SQL Authentication credentials:**
   - **Username:** `dbadmin@paulschwartzberg.onmicrosoft.com`
   - **Authentication:** Active Directory (Entra ID) — configured via `Authentication=Active Directory Interactive` (local) or `Active Directory Default` (App Service managed identity)

---

## 9. Azure Storage Account

**Storage Account Name:** `st4hw3`  
**Blob Endpoint:** `https://st4hw3.blob.core.windows.net/`  
**Table Endpoint:** `https://st4hw3.table.windows.net/`  
**Tenant ID:** `d607a394-7dc0-4c7d-8b9e-a9ed69c728b9`

---

## 10. Custom Azure Resource Abbreviations

| Resource Type | Abbreviation | Example Name |
|---|---|---|
| Resource Group | `rg` | `rg_hw5` |
| App Service | `app` | `app-notekeeper-cscie94-ps-hw5` |
| App Service Plan | `asp` | `asp-cscie94` |
| SQL Server | `sql` | `sql-cscie94-2026-ps` |
| SQL Database | `sqldb` | `sqldb-cscie94-2026_hw5` |
| Storage Account | `st` | `st4hw3` |
| AI Foundry | `ai` | `ai-cscie94-foundry` |

These follow the [Azure abbreviation recommendations](https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-abbreviations).

---

## 11. Azure App Service Website URL

**Azure Web App Name:** `app-notekeeper-cscie94-ps-hw5`  
**Default Domain:** `app-notekeeper-cscie94-ps-hw5-g0e6axbraafudccb.swedencentral-01.azurewebsites.net`  
**Production URL:** `https://app-notekeeper-cscie94-ps-hw5-g0e6axbraafudccb.swedencentral-01.azurewebsites.net`  
**Runtime Stack:** Dotnetcore 10.0
**Operating System:** Linux  
**Region:** Sweden Central  
**App Service Plan:** `asp-cscie94 (B2: 1)`

---

## 12. Azure Function

No Azure Function is used in HW5. (Azure Functions were used in HW4 only.)

---

## 13. Additional Resource Names and Configuration (Extra Credit)

**Extra Credit 1 – Attachment Management:**
- Uses Azure Blob Storage account `st4hw3`
- Container name per note = Note ID (GUID, lowercase)
- Blob name per attachment = new GUID generated on upload
- Blob metadata keys: `noteid`, `originalfilename`
- Maximum attachments per note: 3 (configured in `NoteLimits.MaxAttachments`)
- Seed attachments per note (uploaded on first-login seeding):
  - "Running grocery list" → MilkAndEggs.png, Oranges.png
  - "Gift supplies notes" → WrappingPaper.png, Tape.png
  - "Valentine's Day gift ideas" → Chocolate.png, Diamonds.png, NewCar.png
  - "Azure tips" → AzureLogo.png, AzureTipsAndTricks.pdf

**Application Insights:**
- Name: `app-notekeeper-cscie94-ps-hw5`
- Region: Sweden Central

---

## 14. HTTP Status Codes Used

| Scenario | HTTP Status Code |
|---|---|
| Page loads successfully | 200 OK |
| Successful form submission (upload, delete) | 302 Redirect (Post-Redirect-Get) |
| Note not found | 404 Not Found |
| Access to another user's note | 404 Not Found (by design, to not reveal existence) |
| Upload validation failure (no file, limit exceeded) | 200 OK (re-renders page with error message) |
| Unauthenticated access | 302 Redirect to login |

---

## 15. Design Notes

- The note table is named **`NoteMultiTenant`**; the tag table is named **`Tag`**
- `NoteMultiTenant` includes a nullable **`UserRealmId`** column (`nvarchar(max)`) storing each user's Entra object ID
- Multi-tenant isolation: users only see and access their own notes
- First-login seeding creates each user's personal copy of default notes, tags, and attachments
- Tag generation uses Azure OpenAI (`gpt-5-mini`) with retry logic (3 attempts, exponential backoff, 429 handling)
- Cascade delete: Note → Tags via `DeleteBehavior.Cascade`

---

## 16. Compiled Output

The `bin/`, `obj/`, and `publish/` directories are **not** included in the submission.

---

## 17. Testing

- **144 automated tests** (unit + integration) using xUnit, FluentAssertions, Moq, and `WebApplicationFactory`
- Integration tests use `TestAuthHandler` for simulated authentication
- In-memory Azure Storage service used in test infrastructure
- Build and test command: `dotnet test HW5NoteKeeper.Tests`

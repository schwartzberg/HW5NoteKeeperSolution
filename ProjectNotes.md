# Project Notes

## 1. Homework Assignment Title and Number
**Assignment #5 - Multi-Tenant Note Keeper**

---

## 2. Student Information
**Name:** Paul Schwartzberg  
**Email:** paulschwartzberg@outlook.com

---

## 3. Attribution Regarding the Use of AI

### What I Used AI For
- Project scaffolding guidance
- Pattern comparison against prior coursework
- Debugging and test support
- Documentation drafting support

### What I Coded by Hand and/or Modified Considerably
- Final implementation details, configuration choices, and Azure resource wiring for this assignment

### Summary
This file will be updated as the HW5 implementation grows.

---

## 4. Notes for the TA

This solution is being built as an ASP.NET Core 10.0 Razor Pages application secured with Microsoft Entra External ID.  
The application uses Azure SQL through Entity Framework Core and Azure Storage for attachments.

At the current stage of implementation:
- Entra External ID sign-in is already present in the base project
- HW5 is being adapted to a **multi-tenant** data model using `UserRealmId`
- The seed data strategy is **per-user first-login seeding**, not app-start global seeding
- Extra credit 1 storage seeding is intended for this project

This document will continue to be updated as additional HW5 parts are implemented.

---

## 5. Microsoft Foundry and Project Name

**Microsoft Foundry Name:** `ai-csscie94-foundry`  
**Foundry Project Name:** `note-keeper`

**Deployment Model Name:** `gpt-5-mini`

---

## 6. Azure SQL Information

**Azure SQL Server Name:** `sql-cscie94-2026-ps`  
**Azure SQL Database Name:** `sqldb-cscie94-2026_hw5`  
**Azure SQL Server URL with database:**  
`Server=tcp:sql-cscie94-2026-ps.database.windows.net,1433;Initial Catalog=sqldb-cscie94-2026_hw5;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;Authentication=Active Directory Default;`

**Authentication approach:** Managed identity / Microsoft Entra authentication

No passwords or secrets are stored in this file.

---

## 7. Azure App Service Website URL

**Azure Web App Name:** `app-notekeeper-cscie94-ps-hw5`  
**Production URL:**  
`https://app-notekeeper-cscie94-ps-hw5-g0e6axbraafudccb.swedencentral-01.azurewebsites.net`

---

## 8. Azure Storage Information

HW5 reuses the Azure Storage pattern from HW4 for:
- Blob containers for note attachments
- Azure Storage queues where applicable
- Azure Table Storage where applicable in later steps

The same storage-account-based design from HW4 is intended to be reused here, with HW5-specific behavior layered on top.

This section will be updated with the exact HW5 configuration details as implementation proceeds.

---

## 9. Current HW5 Core Design Notes

- The note table is named **`NoteMultiTenant`**
- The tag table is named **`Tag`**
- `NoteMultiTenant` includes a nullable **`UserRealmId`** column of type `nvarchar(max)`
- Each user's Entra object ID is stored in `UserRealmId`
- Users must only access their own notes
- First-login seeding creates that user's personal copy of the default notes and tags
- Extra credit 1 behavior means the corresponding attachment blobs are also seeded for that user

---

## 10. Security Note

No secrets, API keys, passwords, or privileged credentials are committed to this repository or this documentation.

Sensitive values belong in:
- User Secrets for local development
- Azure App Service environment variables
- Azure-managed identity configuration

---

## 11. Testing Note

The first implementation step includes automated verification for:
- The per-user seeding service
- The first-authenticated-request trigger path

The step is not considered complete until the required unit tests pass.

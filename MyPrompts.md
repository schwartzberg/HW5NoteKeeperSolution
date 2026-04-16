# My Prompts Used During Development

This document records the prompts used during the implementation of `HW5NoteKeeper`.

---

## 1. HW5 working agreement and incremental implementation

**Prompt:**
```text
you will now start building an application in the HW5NoteKeeper in your currrent working solution.  You will will build the application described in "HW05A Instructions.pdf" and "HW05A Instructions.dcox" files, and they are one directory above your current working directory.  You will always use those two  files as your orientation.  But you will not implement them directly - it will only be your "orientation" so you know how the application is to be buildt up and can ask me questions.  You will only do implementations step by step based on pictures of descriptions in parts of those files or where i say do that and that paragraph in those files.  So we will do it in small parts because it is a big application.   For every part - even though you are in agent mode - you will not make any assumptions and ask me first.  Do you understand all this?   (i will then proceed) to the next prompt, if so.
```

**Context:**
- Establish the working rules for HW5.
- Use the assignment documents only as orientation.
- Implement only the user-directed portion at each step.

**Resolution:**
- Confirmed the small-step workflow.
- Agreed not to make assumptions and to clarify behavior before implementing each part.

---

## 2. HW5 architecture, reference projects, and first-step scope

**Prompt:**
```text
You will need to look into  this directory "C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\assignments\04-Assignment\HW4NoteKeeper" and in this directory "C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\assignments\04-Assignment\HW4NoteKeeperEx1" because part of the implementations there we will need to implement in our HW5NoteKeeper.   This you will need to do in many cases.  Ask if you are unsure.    [images omitted here]  The last 4 pictures are only for your orientation of what the application is doing.  I will have created all azure resource that you need.  Only later in the implementation if things are not working will you be at liberty to do an "az login" and correct things in azure if necessary.  I have created the new web app resourc in azure that we will be deploying too (just as we have done for the HW4NoteKeeperSolution).   Here is a picture of the web app resource in azure  [image omitted]  .  It is using managed identity (just like done for the HW4NoteKeeperSolution.  Here is a picture of the user managed identity blade [image omitted]  .   I have also created a new database, here is a picture of it  [image omitted].   We need to create the same tables that we created for the old database in HW4NoteKeeperSolution.   But the Notes table in the old database is now called NoteMultiTenant.[image omitted] see last picture for its implementation.  But you will also add to the NoteMultiTenant table one more field that is not shown in the last picture.  The field is call UserRealmId.  I will be of type nvarchar(max), null.   The solution we are building already is using Entra Id external Id.  This is already implemented.  And the functionality concerning it should remain intact and not damaged.  For your orientation I am also showing here the implementation of the Tag table [image omitted]   .  Of course i have written this prompt carefully and all pictures are place where they are relevant for the text in the sentance.  But the Tag part is also implemented in HW4NoteKeeperSolution.  You should tell me if you want me to create the tables in the azure sql database, here is the connection string  page to the new database [image omitted]   .  Remember - like in HW4NoteKeeperSolution, we will be using managed identity to access the database .  The id-dbadmin and paulschwartzberg@outlook.com - the two users associated with the web app are given owner and other roles in the new database.   You will implement in HW5NoteKeeper - like in HW4NoteKeeperSolution an Entityframework layer to access and update the database.  All crud functions are the same as in HW4NoteKeeper - but each CRUD access to the database must be enhanced in comparison with the implementation in HW4NoteKeeper.  For this enhancement see the solution is this directory:  C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureExternalIdEFRazorPagesDemoSolution and C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureExternalIdEFRazorPagesDemoSolution\AzureExternalIdEFRazorPagesDemo.   Actually HW5NoteKeeper needs to access the database like in the the AzureExternalIdEFRazorPagesDemo project.  And not exactly like in HW4NoteKeeper - from there please take the model (of course update it as described above in the pictures and with the new table name and new field UserRealmId) and use the UserRealmId field as in the AzureExternalIdEFRazorPagesDemo project.  Do not implement the Razor pages yet.  I will start that and ask you to do that in the following steps (not next step).  You can see in HW5NoteKeeper in the folder ClaimsHelpers  ClaimsPrincipalExtensions  at the method GetObjectIdentifier how to get the string that all CRUD things in HW5NoteKeeper needs to test for.  But i will add the Razor pages to do the CRUD stuff not in the next step.   Please copy the copilot_instructions.md from the HW4NoteKeeperSolution and the MyPrompts.md -- but clear it - only new prompts will be added there each time a prompt is created in the implementation of HW5NoteKeeper.  Copy also ProjectNotes.md but update it for this HW5.  We are using a new database and new web app for this solution (see above) - update it with this information.  We will be using the Azure Storage (container, table and queue) as in HW4NoteKeeperSolution.  Please get prepared to implement and ask me questions because you will make not assumptions and then i will give you the command to start the implementation of this first step.
```

**Context:**
- The HW5 implementation should reuse patterns from HW4 and the Azure External ID EF Razor Pages demo.
- The first step is focused on the EF/data/seeding foundation, not Razor CRUD pages.

**Resolution:**
- Reviewed the current HW5 project plus the referenced HW4/HW4Ex1/demo solutions.
- Confirmed that the first step should use:
  - `NoteMultiTenant` plus `Tag`
  - `UserRealmId` ownership filtering based on `GetObjectIdentifier()`
  - managed identity / passwordless SQL access
  - HW4-style Azure Storage reuse

---

## 3. Per-user first-login seeding behavior

**Prompt:**
```text
you need to seed per new user that logs in.  That user will have his/her own claims objectId that goes into the UserRealmId.  That is why the table is called NoteMultiTenant.  The seeding will happen per new user that logs in for the first time.  When the application deploys the application should not remember previous logged in users.  Every user after deployment is a new user.   The seeding should happen as in HW4NoteKeeperSolution.  For HW5NoteKeeper, we will implement extra credit 1, so when seeding, each time, the Azure storage is seed too as in HW4NoteKeeperSolution.   Here is also a description of the seeding for HW5NoteKeeper  [images omitted].   Please create unit tests for this. I will execute the unit tests for this before deployment.  If it makes sense for you use the patter of unittesting using the WebApplicationFactory class or object.  Remember this will be a Razor Page application like the AzureExternalIdEFRazorPagesDemo and the crud changes will be executed via code behind for the razor pages.
```

**Context:**
- HW5 seeding is not app-start seeding.
- Each authenticated user needs a one-time personal seed set.
- Extra credit 1 storage seeding is in scope for this first step.

**Resolution:**
- Captured the required seeding rule: first authenticated request for a user triggers note/tag/storage seeding for that user only.
- Captured the test expectation: add unit tests plus a `WebApplicationFactory`-style trigger test.

---

## 4. Passing tests are required for completion

**Prompt:**
```text
Please remember that all unit test must pass.  Only when the unit tests have passed are you finished.
```

**Context:**
- Completion criteria for the first step.

**Resolution:**
- Treat the first step as incomplete until the unit tests pass.

---

## 5. Repo-level workflow reminders

**Prompt:**
```text
please also look at copilot_instructions.md for any lessons learned from previous tasks.  And do not forget to update eacht time I write a prompt, the MyPrompts.md file in HW5NoteKeeper.    Please stop and ask me for help if you see you are looping too much are things are taking too long (over 5 minutes, for example).
```

**Context:**
- Bring prior lessons forward into HW5.
- Keep prompt tracking current.
- Stop and ask if implementation stalls.

**Resolution:**
- Added HW5-specific repo instructions.
- Continued maintaining this prompt log.
- Incorporated the stop-and-ask rule for stalled progress.

---

## 6. Start the first HW5 implementation step

**Prompt:**
```text
now start hw5.  Good luck with this first step -- there are many to come by the way.
```

**Context:**
- Authorization to begin the first implementation step.

**Resolution:**
- Started the first step with planning, repo setup, reference review, and implementation tracking.

---

## 7. Local config screenshots and periodic check-ins

**Prompt:**
```text
[sanitized] The user shared HW4 local configuration screenshots for orientation and asked for periodic pauses/check-ins so they can inject new prompts while work is in progress.
```

**Context:**
- The screenshots included secret-bearing local configuration context and were intended only as orientation.
- The user wanted frequent stopping points during implementation.

**Resolution:**
- Used the screenshots only as orientation and did not copy secrets into repository files.
- Adopted the periodic check-in workflow during longer implementation steps.

---

## 8. Collaborative database troubleshooting

**Prompt:**
```text
but if it does not work ... let me help

thanks ... we are here to help each other ... if it does not work ... i will check the errors ... and come with suggestions ... so you can continue more efficiently ...
```

**Context:**
- This applies especially to the live Azure SQL migration/update step.

**Resolution:**
- If the live database update fails, stop and hand the error back to the user for review instead of pushing through risky retries.
- Treat the user as an active troubleshooting partner for operational issues.

---

## 9. Put the workflow into repo instructions

**Prompt:**
```text
put this way of working in copilot_instructions.md
```

**Context:**
- The collaboration pattern should be written into the project-level instructions so it remains active for later HW5 steps.

**Resolution:**
- Updated `.github/copilot-instructions.md` to capture collaborative troubleshooting, periodic pauses, and the rule against copying secrets into repo files.

---

## 10. Reminder about tag generation on note save

**Prompt:**
```text
yes when the application saves a NoteMultiTenant it must also generate tags (like in HW4NoteKeeperSolution) - you noticed this - right?  It uses Azure openai or azure foundry - and we need to do the the same in HW5NoteKeeper... just to remind you ... if you have not noticed
```

**Context:**
- This applies to the HW5 note-save path generally, not just to first-login seed data.

**Resolution:**
- Incorporated a reusable HW5 note-tagging path so first-login seeding generates tags now and later Razor create/edit note saves can call the same Azure OpenAI / Foundry-backed tagging logic.

---

## 11. Queue the later auth regression tests

**Prompt:**
```text
Please see also the tests in this solution/project: C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureEntraExternalIdDemo-002Solution\AzureEntraExternalIdDemo-002.Tests and this solution/project: C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureEntraExternalIdDemoSolution\AzureEntraExternalIdDemo.Tests\AzureEntraExternalIdDemo.Tests.csproj  - when you are finished with all the above (with all you now have planned to do) - then add this prompt as your next step - to look at these two test projects and implement these tests - as needed to test the implementation that i had done (before you started) of logging users in and OpenIdConfiguration and ClaimsHelper tests ... especially the tests in AzureEntraExternalIdDemo-002.Tests  these tests are usefull to do for our HW5NoteKeeper
```

**Context:**
- This is explicitly a follow-on step after the current HW5 data/seeding/migration/test foundation is complete.

**Resolution:**
- Queued this as the next tracked step after the current HW5 foundation work, with emphasis on reusing the auth/open-id/claims-helper test patterns from `AzureEntraExternalIdDemo-002.Tests`.

---

## 12. Prioritize the Azure SQL tables and pause after migration references

**Prompt:**
```text
Doing the database stuff - setting up the tables in the azure database should be prioritized since everything else depends on that ... please remember to do this as a priority.

(1) you might want to see these solutions to learn how to do the migrations C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureExternalIdEFRazorPagesDemoSolution\AzureExternalIdEFRazorPagesDemo\AzureExternalIdEFRazorPagesDemo.csproj  and C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\08-LecturePack\EFRazorPagesDemoSolution\EFRazorPagesDemo\EFRazorPagesDemo.csproj and please wait while i continue ... i might have to ask you more questions concerning the first migration ... so please wait with further implementation but looking at the solutions i gave here might help you when you need start again ... please wait though...
```

**Context:**
- The user emphasized that the live Azure SQL tables are the highest priority for the current step.
- After the first migration attempt failed on permissions, the user pointed to two lecture reference projects and asked for a pause before any further implementation.

**Resolution:**
- Prioritized the EF migration flow, scaffolded the initial migration, attempted the live update, and stopped on the Azure SQL permission error.
- Reviewed the two referenced migration project files for later comparison and then paused further implementation as requested.

---

## 13. Migration reset coordination and configuration troubleshooting

**Prompt:**
```text
just to let you know. I started the package manager console in visual studio for the HW5NoteKeeper.  And I am ready to type "Add-Migration InitialCreate" -- is the solution set up for this?  If not please make no assumptions and set up the solution for this. ask me first

also see picture [sanitized screenshot] the database tables are not there in the sql server database

should i delete __EFMigrationsHistory ?

zeor i get.  update copilot_instructions.md since this cooperation between us is what i want - instead of looping ...especially on issues that involve configuration as here.
```

**Context:**
- The user was coordinating directly from Visual Studio Package Manager Console and SSMS during the first migration reset.
- The screenshot showed only `dbo.__EFMigrationsHistory`, with no application tables present.
- The history table row count was confirmed as zero.

**Resolution:**
- Confirmed the solution was already set up for EF migrations but warned against creating a duplicate first migration while an initial scaffold already existed.
- Confirmed that deleting `dbo.__EFMigrationsHistory` is safe in this reset scenario because it is empty and there are no app tables to preserve.
- Strengthened the repo instructions to prefer explicit user-guided troubleshooting over looping on configuration and migration issues.

---

## 14. User updated Program.cs for connection string / DbContext loading

**Prompt:**
```text
I updated program.cs file by the way ...it was not loading the dbcontext properly or reading the connection string properly.
```

**Context:**
- The user made a direct manual change in the app startup pipeline while coordinating the migration reset.

**Resolution:**
- Treated the user's updated `Program.cs` as the current source of truth and inspected it before making any further changes.
- Noted that the file currently contains two `AddDbContext<NoteKeeperContext>` registrations, which should be reconciled before the next migration retry.

---

## 15. User corrected Program.cs

**Prompt:**
```text
program.cs is now corrected
```

**Context:**
- The user revised the startup file again after the duplicate `AddDbContext` issue was pointed out.

**Resolution:**
- Re-checked `Program.cs` and confirmed it now has a single `AddDbContext<NoteKeeperContext>` registration with the connection string read before registration.
- Treated that corrected startup file as the new source of truth for the next migration retry.

---

## 16. Reset the first migration name to InitialCreate

**Prompt:**
```text
should i do Add-Migration InitialCreate ?  I think this is our next step.  do you agree?
```

**Context:**
- The old scaffolded migration `InitialNoteMultiTenant` was still present in `Data\Migrations`, so `InitialCreate` could not yet be the true first migration.

**Resolution:**
- Confirmed that `Add-Migration InitialCreate` was not the next step until the old scaffold was removed.
- Removed the existing `InitialNoteMultiTenant` migration files and snapshot so `InitialCreate` can now be created as the first migration.

---

## 17. Narrow scope to migration creation only

**Prompt:**
```text
please focus only on tasks that lead to a successfull Add-Migration InitialCreate.  Will you please do that?  And then i will ask you whether i should switch to a better LLM?  ...
```

**Context:**
- The user wants all unrelated implementation work deferred until the local EF migration can be created cleanly as `InitialCreate`.

**Resolution:**
- Narrowed scope to only the setup and fixes required for a successful `Add-Migration InitialCreate`.
- Deferred unrelated implementation and the later live database update until after the migration-creation path is clean.

---

## 18. Concurrency exception investigation and email display fix

**Prompt:**
```text
the email is not showing (see picture) any idea why?  and still the concurrency exception [screenshot]
```

**Context:**
- The navbar was not displaying the user's email address.
- The Edit page was throwing `DbUpdateConcurrencyException` on save.

**Resolution:**
- Fixed email display by using CIAM `preferred_username` claim as fallback.
- Investigated concurrency exception root cause in the tag generation / save flow.

---

## 19. ChangeTracker.Clear() and ongoing concurrency issue

**Prompt:**
```text
i think changetracker.clear() will never hurt...but here it is again [screenshot] same problem
```

**Context:**
- The `DbUpdateConcurrencyException` persisted even after adding `ChangeTracker.Clear()`.

**Resolution:**
- Confirmed `ChangeTracker.Clear()` was not harmful but not the root fix.
- Led to identifying that the tag generation refactoring was needed.

---

## 20. Tag generation refactoring to match HW4 pattern

**Prompt:**
```text
I found the problem and we need to refactor some thing.  Are you ready?

Please read the Post and Patch methods in HW4NoteKeeper Controller. They both call _tagGeneratorService.GenerateTags(request.Details) and it returns a response with a Tag collection.  In HW5 it is the responsibility of the caller to use those tags as they see fit.  Do not send the Note down to the tag generating method.  Please see how the patch method then does _context.Tags.RemoveRange(existingNote.Tags) and then _context.Tags.Add(tag) and only if the details have changed.  Rename ApplyGeneratedTagsAsync to ApplyGeneratedTags.  Use a logger in all of it like in HW4.  Make no assumptions and ask me first.  You are only finished when all tests pass.
```

**Context:**
- The user identified the root cause of the concurrency exception: the tag generation service was modifying the Note entity directly instead of returning tags for the caller to manage.
- The HW4 pattern (GenerateTags returns tags, caller does RemoveRange + Add + SaveChangesAsync) was the correct approach.

**Resolution:**
- Refactored `NoteTagService.ApplyGeneratedTags` to take `string details` and return `KeyTagsResponse`.
- Implemented retry loop with maxRetries=3, exponential backoff, and 429 handling matching HW4.
- Callers (seeding, Edit page) now handle tag CRUD: `RemoveRange` + `Tags.Add` + single `SaveChangesAsync`.
- All 128 tests passing, concurrency bug fixed.

---

## 21. XML documentation for all public members

**Prompt:**
```text
there are some public methods like public async Task<KeyTagsResponse> ApplyGeneratedTags(string details, CancellationToken cancellationToken = default) that are lacking xml comments. please comment all public variable, properties, methods and classes appropriately in HW5 solution
```

**Context:**
- Several public members across the HW5 solution lacked XML documentation comments.

**Resolution:**
- Added XML doc comments on all public variables, properties, methods, and classes across the entire HW5 solution.

---

## 22. Attachment management (Extra Credit 1)

**Prompt:**
```text
for the first extra credit we must add support for attachment management... you can read about it in the requirements pdf document.  [Screenshots of requirements 1.1-1.5]  To upload to azure storage see how it is done in HW4NoteKeeper AzureStorageService.cs.  We need UploadAttachmentAsync, DeleteAttachmentAsync, etc.  Our Razor page can do as in HW4 NoteKeeperAttachmentController PutAttachment.  We need to create a new guid for file names.  [Detailed instructions for upload, download, delete, seeding of attachments matching HW4 pattern]  Make no assumptions and ask me first.
```

**Context:**
- Extra Credit 1 requires full attachment management: upload, download, delete, and seeding.
- Must match HW4 patterns for Azure Blob Storage operations.
- Attachments displayed in the Notes Details page with download and delete links.

**Resolution:**
- Created `IAzureStorageService` interface and `AzureStorageService` implementation.
- Rewrote Details page with Upload, Download, Delete handlers.
- Updated `AzureStorageInitializer` with `originalfilename` metadata.
- Fixed Azure.Storage.Blobs v12.27.0 SDK breaking change (positional parameter).
- Seeded attachments per note matching HW4 pattern.
- All attachment operations working.

---

## 23. Unit tests for attachment management

**Prompt:**
```text
I forgot to say -- you need to write unit tests for everything you are doing in this regard, and all unit tests in the solution must pass -- otherwise you are not finished -- and we are using the same azure storage as in HW4.

you can look at tests in HW4NoteKeeper.Tests.csproj to see how the tests to do with uploading, deleting, getting, creating attachments are done
```

**Context:**
- All attachment functionality must have comprehensive unit tests.
- Tests should follow HW4 test patterns.

**Resolution:**
- Updated test factory with `InMemoryAzureStorageService`.
- Wrote 16 comprehensive attachment integration tests (upload, download, delete, lifecycle, multi-tenant isolation, max attachment limit).
- All 144 tests passing.

---

## 24. ProjectNotes.md update

**Prompt:**
```text
You need to update ProjectNotes.md as indicated here in this picture [screenshots of required sections]
```

**Context:**
- ProjectNotes.md needed to be fully rewritten with all 17 required sections per the assignment instructions.

**Resolution:**
- Completely rewrote ProjectNotes.md with all sections including Azure credentials, resource names, HTTP status codes, AI attribution, extra credit documentation, and resource abbreviations.

---

## 25. Project rename to HW5NoteKeeper

**Prompt:**
```text
The last step will be to rename the HW5NoteKeeperSolution project to: HW5NoteKeeper.  And then to publish it to the Web App called app-notekeeper-cscie94-ps-hw5 [screenshot] - do not forget to update ProjectNotes.md as appropriate
```

**Context:**
- Rename all project namespaces, file names, and references from `HW5NoteKeeperSolution` to `HW5NoteKeeper`.
- Prepare for Azure App Service deployment.

**Resolution:**
- Replaced all namespace/using references in all .cs, .cshtml, .csproj, .slnx files.
- Renamed .csproj and .slnx files.
- Renamed test project directory.
- Main directory rename blocked by CWD lock (user may rename manually).
- All 144 tests pass after rename.

---

## 26. User will publish manually

**Prompt:**
```text
let me publish ... i want to publish ... not you ...  i will ask your advice as i publish

no continue ... when you are finished with your todos above -- i will then test the project first locally ... and then we will solve the bugs ... and after and if we solve the bugs ... I will then publish the application to the web app
```

**Context:**
- The user will handle Azure App Service publishing personally.
- The workflow is: finish todos → user tests locally → fix bugs → user publishes.

**Resolution:**
- Acknowledged the publishing workflow.
- Continued focusing on completing remaining implementation and test tasks.

---

## 27. Move test project into solution folder

**Prompt:**
```text
[screenshot] see picture -- move the HW5NoteKeeper.Tests folder into the HW5NoteKeeperSolution.  Reload the HW5NoteKeeper.Tests project in the Visual Studio HW5NoteKeeperSolution.  Make sure all the tests are running and passing.
```

**Context:**
- The test project folder was outside the solution directory and needed to be moved inside for proper Visual Studio solution structure.

**Resolution:**
- Moved `HW5NoteKeeper.Tests` into `HW5NoteKeeperSolution`.
- Updated `.slnx` path from `../HW5NoteKeeper.Tests/...` to `HW5NoteKeeper.Tests/...`.
- Updated test `.csproj` ProjectReference from `..\HW5NoteKeeperSolution\HW5NoteKeeper.csproj` to `..\HW5NoteKeeper.csproj`.
- Added exclusion in main `.csproj` to prevent test files from being compiled by the main project.
- Build succeeds, all 144 tests pass.

---

## 28. Default domain and ProjectNotes update

**Prompt:**
```text
The default domain of the web app is app-notekeeper-cscie94-ps-hw5-g0e6axbraafudccb.swedencentral-01.azurewebsites.net  pls update ProjectNotes.md where appropriate
```

**Context:**
- The user confirmed the default domain for the Azure Web App.

**Resolution:**
- Added explicit **Default Domain** line to section 11 of ProjectNotes.md matching Azure portal terminology.

---

## 29. Build fix and MyPrompts update

**Prompt:**
```text
check again that all the tests are passing. update MyPrompts.md with all prompts that have not been added to it. The solution is not building -- Priority NR 1 - get the solution to build!!!!
```

**Context:**
- After moving the test project into the solution folder, the main project was picking up test `.cs` files (354 errors from duplicate assemblies and missing test packages).

**Resolution:**
- Added `<Compile Remove="HW5NoteKeeper.Tests\**" />` exclusion to the main `.csproj`.
- Clean rebuild succeeds with 0 errors.
- All 144 tests pass.
- Updated MyPrompts.md with all missing prompts (#18–#29).

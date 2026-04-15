# My Prompts Used During Development

This document records the prompts used during the implementation of `HW5NoteKeeperSolution`.

---

## 1. HW5 working agreement and incremental implementation

**Prompt:**
```text
you will now start building an application in the HW5NoteKeeperSolution in your currrent working solution.  You will will build the application described in "HW05A Instructions.pdf" and "HW05A Instructions.dcox" files, and they are one directory above your current working directory.  You will always use those two  files as your orientation.  But you will not implement them directly - it will only be your "orientation" so you know how the application is to be buildt up and can ask me questions.  You will only do implementations step by step based on pictures of descriptions in parts of those files or where i say do that and that paragraph in those files.  So we will do it in small parts because it is a big application.   For every part - even though you are in agent mode - you will not make any assumptions and ask me first.  Do you understand all this?   (i will then proceed) to the next prompt, if so.
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
You will need to look into  this directory "C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\assignments\04-Assignment\HW4NoteKeeper" and in this directory "C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\assignments\04-Assignment\HW4NoteKeeperEx1" because part of the implementations there we will need to implement in our HW5NoteKeeperSolution.   This you will need to do in many cases.  Ask if you are unsure.    [images omitted here]  The last 4 pictures are only for your orientation of what the application is doing.  I will have created all azure resource that you need.  Only later in the implementation if things are not working will you be at liberty to do an "az login" and correct things in azure if necessary.  I have created the new web app resourc in azure that we will be deploying too (just as we have done for the HW4NoteKeeperSolution).   Here is a picture of the web app resource in azure  [image omitted]  .  It is using managed identity (just like done for the HW4NoteKeeperSolution.  Here is a picture of the user managed identity blade [image omitted]  .   I have also created a new database, here is a picture of it  [image omitted].   We need to create the same tables that we created for the old database in HW4NoteKeeperSolution.   But the Notes table in the old database is now called NoteMultiTenant.[image omitted] see last picture for its implementation.  But you will also add to the NoteMultiTenant table one more field that is not shown in the last picture.  The field is call UserRealmId.  I will be of type nvarchar(max), null.   The solution we are building already is using Entra Id external Id.  This is already implemented.  And the functionality concerning it should remain intact and not damaged.  For your orientation I am also showing here the implementation of the Tag table [image omitted]   .  Of course i have written this prompt carefully and all pictures are place where they are relevant for the text in the sentance.  But the Tag part is also implemented in HW4NoteKeeperSolution.  You should tell me if you want me to create the tables in the azure sql database, here is the connection string  page to the new database [image omitted]   .  Remember - like in HW4NoteKeeperSolution, we will be using managed identity to access the database .  The id-dbadmin and paulschwartzberg@outlook.com - the two users associated with the web app are given owner and other roles in the new database.   You will implement in HW5NoteKeeperSolution - like in HW4NoteKeeperSolution an Entityframework layer to access and update the database.  All crud functions are the same as in HW4NoteKeeper - but each CRUD access to the database must be enhanced in comparison with the implementation in HW4NoteKeeper.  For this enhancement see the solution is this directory:  C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureExternalIdEFRazorPagesDemoSolution and C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureExternalIdEFRazorPagesDemoSolution\AzureExternalIdEFRazorPagesDemo.   Actually HW5NoteKeeper needs to access the database like in the the AzureExternalIdEFRazorPagesDemo project.  And not exactly like in HW4NoteKeeper - from there please take the model (of course update it as described above in the pictures and with the new table name and new field UserRealmId) and use the UserRealmId field as in the AzureExternalIdEFRazorPagesDemo project.  Do not implement the Razor pages yet.  I will start that and ask you to do that in the following steps (not next step).  You can see in HW5NoteKeeperSolution in the folder ClaimsHelpers  ClaimsPrincipalExtensions  at the method GetObjectIdentifier how to get the string that all CRUD things in HW5NoteKeeperSolution needs to test for.  But i will add the Razor pages to do the CRUD stuff not in the next step.   Please copy the copilot_instructions.md from the HW4NoteKeeperSolution and the MyPrompts.md -- but clear it - only new prompts will be added there each time a prompt is created in the implementation of HW5NoteKeeperSolution.  Copy also ProjectNotes.md but update it for this HW5.  We are using a new database and new web app for this solution (see above) - update it with this information.  We will be using the Azure Storage (container, table and queue) as in HW4NoteKeeperSolution.  Please get prepared to implement and ask me questions because you will make not assumptions and then i will give you the command to start the implementation of this first step.
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
you need to seed per new user that logs in.  That user will have his/her own claims objectId that goes into the UserRealmId.  That is why the table is called NoteMultiTenant.  The seeding will happen per new user that logs in for the first time.  When the application deploys the application should not remember previous logged in users.  Every user after deployment is a new user.   The seeding should happen as in HW4NoteKeeperSolution.  For HW5NoteKeeperSolution, we will implement extra credit 1, so when seeding, each time, the Azure storage is seed too as in HW4NoteKeeperSolution.   Here is also a description of the seeding for HW5NoteKeeper  [images omitted].   Please create unit tests for this. I will execute the unit tests for this before deployment.  If it makes sense for you use the patter of unittesting using the WebApplicationFactory class or object.  Remember this will be a Razor Page application like the AzureExternalIdEFRazorPagesDemo and the crud changes will be executed via code behind for the razor pages.
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
please also look at copilot_instructions.md for any lessons learned from previous tasks.  And do not forget to update eacht time I write a prompt, the MyPrompts.md file in HW5NoteKeeperSolution.    Please stop and ask me for help if you see you are looping too much are things are taking too long (over 5 minutes, for example).
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
yes when the application saves a NoteMultiTenant it must also generate tags (like in HW4NoteKeeperSolution) - you noticed this - right?  It uses Azure openai or azure foundry - and we need to do the the same in HW5NoteKeeperSolution... just to remind you ... if you have not noticed
```

**Context:**
- This applies to the HW5 note-save path generally, not just to first-login seed data.

**Resolution:**
- Incorporated a reusable HW5 note-tagging path so first-login seeding generates tags now and later Razor create/edit note saves can call the same Azure OpenAI / Foundry-backed tagging logic.

---

## 11. Queue the later auth regression tests

**Prompt:**
```text
Please see also the tests in this solution/project: C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureEntraExternalIdDemo-002Solution\AzureEntraExternalIdDemo-002.Tests and this solution/project: C:\Users\schwa\Documents\H_DCE\cloud_computing_openai_e_94\09-LecturePack\AzureEntraExternalIdDemoSolution\AzureEntraExternalIdDemo.Tests\AzureEntraExternalIdDemo.Tests.csproj  - when you are finished with all the above (with all you now have planned to do) - then add this prompt as your next step - to look at these two test projects and implement these tests - as needed to test the implementation that i had done (before you started) of logging users in and OpenIdConfiguration and ClaimsHelper tests ... especially the tests in AzureEntraExternalIdDemo-002.Tests  these tests are usefull to do for our HW5NoteKeeperSolution
```

**Context:**
- This is explicitly a follow-on step after the current HW5 data/seeding/migration/test foundation is complete.

**Resolution:**
- Queued this as the next tracked step after the current HW5 foundation work, with emphasis on reusing the auth/open-id/claims-helper test patterns from `AzureEntraExternalIdDemo-002.Tests`.

# MediAssist Development Progress

MediAssist is a C# and Blazor prototype for preparing and tracking medical assistance applications. Milestone 1 and Milestone 2 phases A–C, D1, D2 and D3 are committed. D3 includes simulated document uploads, the correction workflow and prerequisite fixes P1–P3. Administrator decisions, patient management and runner actions are still planned. **D4 (patient profiles and simulated authorization requests) is committed as `d9fd22a` and pushed. D5 (hospital directory and search) is committed as `b42be6d` and pushed. D6 (optional runner requests) is committed as `5ecfb68` and pushed. D7 (notification read/unread and links) is implemented and verified but not yet committed.**

## Current snapshot

| Field | Verified state |
|---|---|
| Reviewed | 13 November 2026, Asia/Manila (D7 implementation review) |
| Branch | `design/premium-civic-imagery` |
| Branch HEAD | `5ecfb68` — feat(patient): implement optional runner requests (pushed; `origin/design/premium-civic-imagery` in sync) |
| Latest application commit | `5ecfb68aa55c2b496368bf700820801249f0ecad` — D6 |
| Latest implementation | D7 — In-App Notifications & Read/Unread Management (**implemented, uncommitted**) |
| Application changes awaiting commit | D7: 10 application files (all modified) — see “D7 In-App Notifications” below |
| Documentation awaiting commit | (1) the post-commit D6 hash/push record, preserved; (2) this D7 status, features, verification and log update |
| Existing untracked paths | `.agents/`, `.claude/`, `skills-lock.json`; excluded from progress claims and preserved |
| Overall status | Milestone 2 in progress; A–C and D1–D6 committed and pushed; D7 implemented and verified, awaiting commit approval |
| Next development task | Commit D7 (with this document) once approved; then D8 — User Profile & Settings (efficient mode) |

**Evidence standard:** “Completed” below means implemented in the inspected source and present in the listed commit. It does not imply production readiness or complete browser testing. D3 and its pre-commit documentation were committed as `8c0554e`; this subsequent hash/status entry is uncommitted because a commit cannot contain its own hash. Remote synchronization was not checked; no push or merge was performed.

## Evidence and verification

- Concept and business rules: [Phase 1 specification](MediAssist_Phase1_Documentation.docx), version 1.0. It records concept planning through Step 1.23 and describes Phase 2 implementation priorities.
- Phase D roadmap: the owner's agreed D1–D10 task list supplied on 12 November 2026. This defines the numbering below; implementation status is checked independently against source and Git.
- History: `git log` and relevant commit file summaries, checked against current source. Milestone 1 is identified explicitly in commit `5e2db14`.
- Application: [project](../src/MediAssist.Web/MediAssist.Web.csproj), [startup](../src/MediAssist.Web/Program.cs), routed pages, services, models, styles and navigation.
- Design documentation: [image slots](../src/MediAssist.Web/wwwroot/images/README.md) and [fonts](../src/MediAssist.Web/wwwroot/fonts/README.md). The image README still groups application pages among pages being built; actual D1 routes take precedence for implementation status.
- Build on this review: an isolated `dotnet build` succeeded with **0 errors and 2 NU1900 warnings** because the package vulnerability service was unreachable. The ordinary output-directory build failed with MSB3027/MSB3021 because a running app locked `MediAssist.Web.exe`; the running session was left intact.
- D3 verification (12 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**. A scripted browser suite passed **248 of 248** page checks (62 routes × 320, 768, 1024 and 1440 px, with stored drafts, submissions and uploads present): no horizontal overflow, 44 px targets, 13 px minimum text, one h1, at most one filled button, no duplicate IDs or broken ARIA references, heading order and AA text contrast. Flow checks are listed under D3 below.
- D3 commit verification (13 November 2026): fresh `dotnet build src/MediAssist.Web/MediAssist.Web.csproj --nologo -p:OutputPath=<temporary-directory>` passed with **0 errors, 0 warnings**; staged diff check passed and the exact 16 application files plus this document were committed. Prior browser results above were retained as historical evidence, not rerun during this commit task.
- D4 verification (13 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `git diff --check` clean. Scripted browser suite passed **276 of 276** page checks (69 routes × 320, 768, 1024 and 1440 px, with stored profiles, requests, drafts, submissions and uploads present): overflow, 44 px targets, 13 px text, one h1, at most one filled button, duplicate IDs, ARIA references, heading order and AA contrast. Flow checks are listed under D4 below.
- D5 verification (13 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `git diff --check` clean. Scripted browser suite passed **272 of 272** page checks (68 routes × 320, 768, 1024 and 1440 px) with the same accessibility and layout checks as D4. Flow checks are listed under D5 below.
- D6 verification (13 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `git diff --check` clean. Scripted browser suite passed **232 of 232** page checks (58 routes × 320, 768, 1024 and 1440 px, with own and seeded runner requests stored), same checks as D4–D5 plus textarea target size. Flow checks are listed under D6 below. The version 3 → 4 snapshot upgrade uses the same generic upgrade path verified with real older builds in D3 and D4; it was not re-run with a D5 build.
- D7 verification (13 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `git diff --check` clean. Scripted browser suite passed **224 of 224** page checks (56 routes × 320, 768, 1024 and 1440 px, with stored read states and a new local notification). Flow checks are listed under D7 below. The version 4 → 5 snapshot upgrade uses the same generic upgrade path verified with real older builds in D3 and D4.
- No dedicated test project exists in the tracked solution. The browser checks above are scripted in the review session, not committed tests.

To repeat verification from the repository root in PowerShell, without stopping a running app:

```powershell
$progressBuildPath = Join-Path $env:TEMP ('MediAssist-progress-' + [IO.Path]::GetRandomFileName())
dotnet build src/MediAssist.Web/MediAssist.Web.csproj --nologo --artifacts-path $progressBuildPath
```

## Progress table

| Item | Status | Delivery state | Evidence or remaining scope |
|---|---|---|---|
| Phase 1 concept planning | Completed | Committed specification | Scope, roles, workflows, statuses and prototype boundaries |
| Milestone 1 — patient dashboard foundation | Completed | Committed | `5e2db14`; initial .NET project, dashboard, navigation and mock data |
| Civic design and portal expansion | Completed | Committed | `c2ac2b3`, `4cc5769`, `57c4ee0`; shared shell, staff dashboards and image slots |
| Milestone 2 Phase A — A1–A4 refinements | Completed | Committed | `14ad90c`; section labels, error/404 recovery, reconnect UI and compact mobile heroes |
| Milestone 2 Phase B — UI refinements | Completed | Committed | `a168482`; dashboard hierarchy, shared journey stages, compact wizard and wording |
| Milestone 2 Phase C — cleanup | Completed | Committed | `3130302`; status display mappings, unused UI removal, styles and favicon |
| D1 — Application Tracking | Completed | Committed | `7df1c28`; list/detail pages, filters, timeline and activity history |
| D2 — Wizard and Draft Management | Completed | Committed | `8a98432`; validation, save/resume, browser persistence and simulated submission |
| D3 — Simulated Document Uploads & Correction Workflow | Completed | **Committed** | `8c0554e`; simulated uploads, validation, replacement/versions, history, counts, correction return-to-review; prerequisite fixes P1–P3 |
| Milestone 2 as a whole | In Progress | Partially delivered | Remaining modules and integration defects below |
| D4 — Patient Profile Management & Authorization Requests | Completed | **Committed** | `d9fd22a`; profile list/details/add/edit, reusable profiles in the wizard, simulated authorization requests, D9-ready request data |
| D5 — Hospital Directory & Search | Completed | **Committed** | `b42be6d`; searchable directory, hospital details, wizard and Programs integration |
| D6 — Optional Medical Runner Requests | Completed | **Committed** | `5ecfb68`; request list, request form, cancel before assignment, detail-page links and history; application status untouched |
| D7 — In-App Notifications & Read/Unread Management | Implemented and verified | **Uncommitted** | Read/unread per notification, mark all read, All/Unread filters, links to related items; bell and dashboard follow |
| D8–D10 | Pending | Agreed roadmap | Some read-only foundations exist; remaining workflows are not complete |
| Database, authentication and production deployment | Pending | Concept/planned | No EF Core persistence or authentication implementation in the current project |

“In Progress” describes the overall milestone, not uncommitted feature code. Phase A–C descriptions summarize verified diffs; they are not a reconstructed original subtask specification.

## Relevant commit history

All hashes below are reachable from the inspected branch HEAD.

| Date | Commit | Commit subject |
|---|---|---|
| 2026-11-11 | `5e2db14` | Add Patient Portal dashboard foundation (Phase 2, Milestone 1) |
| 2026-11-11 | `c2ac2b3` | Redesign patient portal: Philippine civic identity + digital bayanihan |
| 2026-11-11 | `4cc5769` | Redesign all portals from the Digital Bayanihan reference |
| 2026-11-11 | `57c4ee0` | Add premium civic layout with image slots |
| 2026-11-12 | `14ad90c` | fix(ui): complete milestone 2 phase A refinements |
| 2026-11-12 | `a168482` | refactor(ui): complete milestone 2 phase B refinements |
| 2026-11-12 | `3130302` | refactor(ui): complete milestone 2 phase C cleanup |
| 2026-11-12 | `7df1c28` | feat(patient): implement application tracking and status history |
| 2026-11-12 | `8a98432` | feat(patient): implement application wizard drafts and submission |
| 2026-11-12 | `6b5791e` | docs: add MediAssist development progress tracker |
| 2026-11-13 | `8c0554e` | feat(patient): implement simulated document uploads and corrections |
| 2026-11-13 | `d9fd22a` | feat(patient): implement patient profiles and authorization requests |
| 2026-11-13 | `b42be6d` | feat(patient): implement hospital directory and search |
| 2026-11-13 | `5ecfb68` | feat(patient): implement optional runner requests |

## Completed features and workflows

### Portal foundation and Phase A–C

- Shared `PortalLayout`, masthead, role-specific navigation, sidebar rail/expanded modes, mobile drawer and bottom navigation. Role selection follows the URL; it is not an authorization boundary.
- Patient dashboard with current application, document actions, recent applications, program discovery, notifications, household summary and optional runner help.
- Administrator dashboard with seeded review queue and runner requests; runner dashboard with seeded tasks and progress. Their linked management/task pages remain placeholders.
- Programs page with keyword search and selected-program requirements; checklist and notification display pages.
- Branded error, not-found and reconnect states; static server rendering for the excluded error route; shared section labels.
- Reusable status mappings, compact progress displays, sticky wizard navigation and responsive layouts. Known regressions remain listed below.

### D1 Application Tracking

| Component or model | Responsibility |
|---|---|
| `Applications.razor` | `/patient/applications`; stage/patient filters, counts, empty states and priority sorting |
| `ApplicationPage.razor` | `/patient/applications/{Reference}`; next action, documents, details and runner context |
| `ApplicationTable` / `ApplicationRow` | Shared application ledger used by the list and dashboard |
| `StatusTimeline` / `ActivityHistory` | Reached-stage dates and timestamped events, including reviewer notes |
| `ApplicationTracking.cs` | Detail/event read models and shared stage filters |
| `FilterChips` / `ApplicationsHref` | Active filter links and consistent application URLs |

The list prioritizes needs-correction applications, then drafts, then other active applications, with cancelled applications last; each group sorts by latest update. Detail queries select household applications, combine program/checklist/history/runner data, and show a recovery state for an unknown reference. Application statuses describe internal processing, not external financial approval.

### D2 Application Wizard and Draft Management

1. Choose a seeded patient, hospital and assistance program.
2. View the document requirements; missing documents do not block submission.
3. Review choices, revisit reached steps, and confirm the information before submission.
4. Continue or manually save to create a draft; later choices auto-save once a draft exists. Draft and step URLs support resuming, including the seeded draft.
5. Submit to create a local Submitted application, checklist, history entry and notification. A newly created application gets a generated reference; a resumed seed draft retains its source reference.
6. Open the confirmation/detail page. Workspace change events refresh the dashboard, applications, checklists, notifications and bell count.

Key files: `NewApplication.razor`, `Drafts.cs`, `ApplicationWorkspace.cs`, `WorkspaceStorage.cs`, `WorkspaceAwarePage.cs`, and `MockPatientPortalDataService.cs`. Submission is simulated and stored in the browser; it sends nothing to an office.

### D3 Simulated Document Uploads & Correction Workflow

Committed on 13 November 2026 as `8c0554e866647fdf50897a2319788ee783566d78` with message `feat(patient): implement simulated document uploads and corrections` (16 application files and the progress document).

**Prerequisite fixes**

| # | Fix | Evidence |
|---|---|---|
| P1 | Restored `openDrawer` / `closeDrawer` in `wwwroot/js/portalLayout.js` (removed in `3130302`) | Phone drawer opens, closes with its button and a real Escape key (Menu returns to `aria-expanded="false"`), and closes on navigation |
| P2 | Programs → wizard: “Start an application for this program” links to `/new?program=<id>`; the wizard pre-selects that program on a new application and still requires patient and hospital first | `/new?program=dialysis` opens step 1 with Dialysis pre-selected |
| P3 | A resumed seed draft reuses the seed checklist only while its program is unchanged; otherwise the chosen program's documents are listed | `ApplicationWorkspace.ChecklistFor` compares the seed application's `ProgramId` |

**Workflow**

1. Every checklist row that is missing or sent back offers **Upload** / **Upload a new copy**; a document the user uploaded and that is still being reviewed offers **Replace**. Links keep the existing `/patient/documents?upload=<document>&application=<reference>` contract, plus an optional same-site `return=` (only `/patient/…` paths are followed).
2. The Documents page opens an upload panel: application context, where to get the document, the reviewer's note when correcting, and four **fictional sample files** per document. Nothing is read from the user's device, so no real file or file name is stored.
3. Validation: a file must be chosen; PDF, JPG or PNG only; at most 10 MB. Two samples are invalid on purpose (HEIC, 18.4 MB). Errors use `role="alert"`, `aria-describedby` and `aria-invalid`.
4. A saved upload marks the document **Being reviewed** (reviewer completeness stays separate: “Ready” still counts reviewer-accepted documents only), shows the file, size, version and time, and adds a history entry (“uploaded”, “Corrected … uploaded”, “… replaced (version 2)”). Replacements keep earlier versions.
5. When every document a reviewer sent back has a new copy, the application returns to **Under review** (Phase 1 rule): a “Correction sent for review” history entry and a notification are added, and the dashboard action and “Upload corrected document” button disappear. This is applied to the user's local copy only; no reviewer is involved.
6. Wizard step 4 offers the same uploads for drafts and returns to the wizard; draft uploads move to the new reference on submission (“Sent with 1 of 4 documents”).
7. Success is reported only after the browser save succeeds; a failed save rolls the change back and shows “We couldn’t save that upload in this browser. Try again.” Submission and draft saving report failures the same way.

**Data model:** `Models/Uploads.cs` (`DocumentUpload` with version and `ReplacesId`, `UploadReviewState` Pending/Accepted/Rejected reserved for D9 reviews, `SampleFile`, `SimulatedFiles` rules); `WorkspaceSnapshot` version 2 adds `Uploads`, and a version 1 (D2) snapshot is **upgraded, not discarded**. New UI: `Features/Applications/UploadPanel.razor`; `DocumentChecklist` shows uploads, Replace and “N being reviewed”.

**Flow checks performed (browser, 12 November 2026):** each validation error; a valid upload and reload; replacement (version 2); draft upload carried to `MA-2026-00453` on submit; correcting `MA-2026-00418` returned it to Under review with history, notification and dashboard updates; upload refused for an accepted document and for another household's application; unsafe `return=` values ignored; a snapshot saved by the D2 build (`8a98432`) opened in D3 with its draft intact; the drawer and Programs pre-selection.

### D4 Patient Profile Management & Authorization Requests

Committed on 13 November 2026 as `d9fd22aad6d0c7704e1ef5692ef075bdf83b8868` with message `feat(patient): implement patient profiles and authorization requests` (29 application files and the progress document), then pushed: `origin/design/premium-civic-imagery` moved from `57c4ee0` to `d9fd22a`, which also published the previously unpushed Phase A–C and D1–D3 commits. `git pull --rebase` beforehand reported the branch up to date, so no commit was rewritten.

**Workflows**

1. `/patient/patients` lists the household: the account holder first, seeded family members, then added patients, each with relationship, age, city, an authorization badge and the next step. “Add a patient” is the view's filled action.
2. `/patient/patients/new` and `/patient/patients/{key}/edit` share `PatientProfileForm`: full name (2–80 characters, letters), relationship (fixed list), date of birth (required, not in the future, within 120 years) and optional city (≤ 60). Validation runs at the workspace boundary; each error is under its field with `aria-describedby`/`aria-invalid`, plus an announced summary. The account holder stays “You”; a relationship is locked while authorization is approved or being reviewed. A fictional-data notice is shown.
3. `/patient/patients/{key}` shows what to do next (Request authorization, or Start an application), profile details, authorization requests with any reviewer note, and the patient's applications (shared D1 table). Saved/requested confirmations are announced.
4. `/patient/patients/{key}/authorization` requests authorization: a basis (parent of a minor, signed letter, legal guardian), one simulated supporting document (the D3 sample files and rules) and a confirmation. “Parent of a minor” is refused for patients 18 or older. A saved request makes the patient “Being reviewed” and adds a notification. Only “Authorization needed” or “Not approved” patients can request.
5. Wizard: step 1 lists the merged household and offers “Add a patient” (returns with the new patient selected). Patients without authorization (needed / not approved) cannot continue — linked error plus a “Request authorization” link; “Being reviewed” remains allowed, as in the seed data. `?patient=` (key or id) pre-selects a patient; the Programs page now carries the program search's patient into the wizard together with the program.
6. Patient URL keys: the earliest household member with a first name keeps the plain key (`jose`), later duplicates get a short suffix (`jose-a3e5`), so existing D1 `?patient=` links are unchanged.

**Data and services:** `Models/Profiles.cs` (`AuthorizationRequest`, `AuthorizationBasis`, `AuthorizationRequestStatus` — Approved/Rejected, `DecidedAt` and `ReviewerNote` reserved for D9 — and `ProfileRules`). `PatientProfile` gains optional `DateOfBirth`, `City`, `CreatedAt`; `AuthorizationStatus` gains `NotRequested` and `Rejected` (appended). Workspace snapshot **version 3** adds local profiles (new and edited seed profiles) and requests; versions 1–2 are upgraded. `ApplicationWorkspace.Household` merges seed and local profiles and replaced the five direct seed-household reads (submission, scoping, draft display/checklists). Seeded requests: Jose (approved, with reviewer note) and Lia (pending). `IStaffPortalDataService.GetAuthorizationRequestsAsync` returns seeded requests for D9; the admin queue's authorization flag now derives from them.

**Flow checks performed (browser, 13 November 2026):** list; add with empty/future-date errors and a valid save; wizard blocks an unauthorized patient and pre-selects from `?patient=`; authorization request errors (no basis/confirmation, minor basis for an adult, HEIC file) and a valid request (badge, request history, bell +1); application submitted for an added patient (`MA-2026-00453`) and shown on the profile and under its My applications chip; relationship lock and an edit saved; account holder relationship fixed; “Add a patient” from the wizard returning with the patient selected; duplicate first name key (`jose-a3e5`) with `?patient=jose` still the seeded Jose; Lia's and Jose's seeded requests; admin flag unchanged (MA-2026-00395); a snapshot saved by the D3 build (`8c0554e`, draft with an upload) opened in D4 intact.

### D5 Hospital Directory & Search

Committed on 13 November 2026 as `b42be6df58a1c6d9241ec271df271209ba0f5df6` with message `feat(patient): implement hospital directory and search` (12 application files and the progress document, which also carried the D4 hash record), then pushed: `d9fd22a..b42be6d`. `git pull --rebase` beforehand reported the branch up to date.

Built in the owner's efficient mode (short plan and build in one turn). Choices made, open to change: the directory **replaces** the wizard's seed-derived hospital list, and a program a hospital does not list is shown as a **note, not a block** (flexible submission).

1. `/patient/hospitals`: a plain GET search (`?q=` for name, place, type or service; `?program=` for programs handled), so it works before the live connection and can be shared. Every word of the query must match; an unknown program means “any”. Results show type, district, city and the programs handled; each card links (whole card) to the hospital. An empty search offers “Show all hospitals”.
2. `/patient/hospitals/{id}`: “Start an application here” (the view's one filled action), programs handled (linked to Programs), the household's applications at the hospital (shared D1 table), the social service office's hours and location with a “confirm before you go” note, and services. Unknown ids show a recovery state.
3. Wizard: step 2 lists the directory (6 hospitals); `?hospital=` (id or name) pre-selects a hospital on a new application; step 3 adds “not listed at <hospital>” to programs that hospital does not handle.
4. Programs: the selected program lists “Handled at:” with links to each hospital.

**Data:** `Models/Hospitals.cs` (`Hospital` with type, district, city, services, program ids, office hours/location; search matching). `MockPortalStore.Hospitals` seeds six fictional hospitals; the first three are the hospitals named in the seeded applications, so every existing application has a directory entry. `IPatientPortalDataService.GetHospitalsAsync` now returns `Hospital` records (was names) and `GetHospitalAsync(idOrName)` was added; applications still store the hospital **name**, so existing drafts and submissions are unaffected.

**Flow checks performed (browser, 13 November 2026):** all six hospitals; `dialysis` (2 results); `pasig` + Diagnostic Procedure Aid (2); no-match empty state; unknown program ignored; details for San Isidro (3 programs, 3 household applications, office facts); a hospital with no applications; unknown id; wizard from `?hospital=riverbend-kidney` with the hospital pre-selected and “not listed at” notes on step 3; Programs “Handled at”. Fixed during verification: missing spaces after the “Handles:”/“Handled at:” labels, and the search row's fields misaligned at desktop widths (the hint now sits on its own row, and under its field on phones).

### D6 Optional Medical Runner Requests

Committed on 13 November 2026 as `5ecfb68aa55c2b496368bf700820801249f0ecad` with message `feat(patient): implement optional runner requests` (15 application files and the progress document, which also carried the D5 hash record), then pushed: `b42be6d..5ecfb68`. `git pull --rebase` beforehand reported the branch up to date.

Built in efficient mode. Choices made, open to change: **one open request per application**, and seeded requests (staff data) **cannot be cancelled** by the patient.

1. `/patient/runner`: “Request runner help” (the view's one filled action), what runners do (the four permitted errands) and never do (cash, medicine, medical or financial decisions), and the household's requests — the user's own first (newest first), then seeded ones — with status badge, place, date and time of day, the application, runner name, notes and a declined-runner note. The user can cancel their own request while it waits for a runner, after a “Cancel this request? Yes, cancel it / Keep it” confirmation.
2. `/patient/runner/new` (`?application=` pre-selects): application (only Submitted, Under review, Needs correction or Ready for processing), errand (deliver documents, submit a corrected document, pick up a document or voucher, return a signed form), needed-by date (tomorrow to 30 days), morning/afternoon window, optional notes (≤ 200 characters, “don't include medical details”) and a confirmation that runner help is optional. Validation at the workspace boundary with field-linked errors; an application with a request in progress is refused. The place is the application's hospital social service office (D5 directory).
3. A saved request is “Waiting for a runner” and adds a notification. Assignment is D9 and running the errand is D10. **The application's status never changes** with a runner request (checked: MA-2026-00418 stayed “Needs correction” through request and cancellation).
4. Application detail: runner requests now include the user's own; “Request runner help” appears when the application is eligible and has no open request (including after a declined or cancelled one); the runner panel links straight to the form for eligible applications. History gains “Runner help requested” and “Runner request cancelled”.

**Data and services:** `Models/RunnerRequests.cs` (`RunnerErrand`, `RunnerRules`). `RunnerRequestItem` gains optional kind, time window, notes, requested and closed times (appended, so seeds and older data still read). Workspace snapshot **version 4** adds runner requests. `ApplicationWorkspace` adds `RunnerRequests` (merged), `RequestRunnerAsync`, `CancelRunnerRequestAsync` and `RunnerEvents`; `IPatientPortalDataService.GetRunnerRequestsAsync` was added. `RunnerTaskStatusDisplay` gains badge tone and icon. Staff views still read seeded requests only.

**Flow checks performed (browser, 13 November 2026):** seeded list (declined and accepted, no cancel button); empty-form errors (5); request refused for an application with an open request and for today's date; date beyond 30 days refused; valid request for MA-2026-00418 (confirmation, own request listed first, bell +1, cancel button); detail page shows the request and hides “Request runner help” while it is open; eligible application without a request links to the form; declined-only application offers a new request; draft shows the general runner panel; `?application=` pre-selection; cancel with confirmation; history shows requested and cancelled; application status unchanged; admin runner requests unchanged. Fixed during verification: own requests were listed after seeded ones (seeded requests have no request time), and the D1 detail page showed future “needed by” dates as “Today” — it now shows the date.

### D7 In-App Notifications & Read/Unread Management (uncommitted)

Built in efficient mode. Decisions: read state covers **seeded and the user's own** notifications; every notification links to the related **application, patient profile or runner page**.

1. `/patient/notifications`: All / Unread filter links (`?show=unread`, counts announced as “notifications”), “Mark all as read” (shown only while something is unread), and per notification “Mark as read” / “Mark as unread”. Each title links to its item; opening it marks it read and still navigates. Every change is announced (“… marked as read.”); a failed save says so and changes nothing. An empty Unread view shows “You're all caught up”.
2. The bell count and the dashboard's latest notifications follow the read state; dashboard titles link to their item and mark it read on open.
3. Links: seeded notifications carry links; new ones (submission, correction, authorization, runner request) store theirs; notifications saved before D7 get a link worked out from their id.

**Data:** `PortalNotification` gains optional `Href`; new `NotificationReadState`. Workspace snapshot **version 5** adds the user's read/unread choices, which override each notification's default (`ApplicationWorkspace.WithReadState`, `SetReadAsync`). `FilterChips` gains a `Noun` parameter so screen-reader counts name what they count.

**Flow checks performed (browser, 13 November 2026):** initial 3 unread (bell 3); mark one read (bell 2, Unread chip 2, row moves down); mark a read one unread (bell 3); mark all read (bell cleared, “You are up to date.”); Unread filter with one item and the active chip; opening that item's link navigated to MA-2026-00431 and cleared it (“You're all caught up”); state kept across reloads; dashboard shows read state and links; a new runner-request notification arrived unread with its link. Fixed during verification: filter chips announced “applications” instead of “notifications”.

## Current architecture

### Server side

- One `Microsoft.NET.Sdk.Web` project targeting **.NET 10**, with nullable reference types and implicit usings. `MediAssist.slnx` contains the web project.
- ASP.NET Core hosts Blazor Razor components with Interactive Server rendering. `Program.cs` configures dependency injection, static assets, antiforgery, HTTPS redirection, production exception handling and not-found re-execution.
- `Components/Pages` contains routes; `Features` contains application/dashboard composition; `UI` contains shared primitives; `Layout` contains the portal shell. `Models`, `Navigation` and `Services` separate display records, URL/navigation rules and data access.
- `IPatientPortalDataService` and `IStaffPortalDataService` expose asynchronous UI-facing reads. No separate API/controller layer, EF Core `DbContext`, migrations or database package is present.
- Authentication, account registration and enforced role/record authorization remain future work. Mock household filtering demonstrates scope but does not establish authenticated identity.

### Client side and design system

- The browser displays server-rendered HTML and uses a Blazor server circuit for interactive wizard actions. JavaScript supports sidebar preferences, viewport changes and native-dialog behavior.
- Global CSS loads in order: fonts, tokens, base, forms and pages, followed by isolated component CSS. There is no separate React or Node frontend.
- Civic styling uses navy `#183153`, warm cream `#F6EFE1`, gold actions and red correction states. Public Sans and Source Serif 4 are self-hosted. Tokens define spacing, type, focus rings, motion, radii and layout sizes.
- Shared primitives include `AppButton`, `Panel`, `Badge`, `StatusBadge`, `SectionHeader`, `EmptyState`, `GuidedActions`, `ChevronStepper`, `StepMeter`, `DocumentChecklist`, `SplitHero` and `ImageSlot`.
- Accessibility provisions include skip links, heading focus on navigation, English/Filipino helper text, status words, live regions, labelled controls, focus styles and reduced-motion rules. These provisions are not a completed accessibility audit.
- `ImageSlots` defines dimensions, alt text and fallback keys; `ImageSlot` looks for supplied assets and otherwise shows a decorative stand-in. At inspection, `wwwroot/images` contained documentation only, so actual photographs are still pending. `?slots=1` exposes slot specifications for review.

### Mock data and state

| Service | Lifetime and data | Boundary |
|---|---|---|
| `MockPortalStore` | Singleton, read-only fictional seed records; dates calculated relative to the current day | Patient and staff services share the seeded story |
| `ApplicationWorkspace` | Scoped to the Blazor circuit; drafts, submissions, notifications, (D3) simulated uploads and (D4) patient profiles and authorization requests; snapshot version 3; `Household` merges seed and local profiles | User work is separate from the singleton seed store |
| `ProtectedLocalWorkspaceStorage` | ASP.NET Core protected browser `localStorage`, key `mediassist.workspace` | Data Protection protects the serialized snapshot; no database persistence |
| `MockPatientPortalDataService` | Scoped; merges local submissions/drafts over matching seed references | Patient views include local changes |
| `MockStaffPortalDataService` | Scoped; reads the seed store only (including seeded authorization requests for D9) | Staff views do not receive browser-only submissions, uploads, profiles or requests |
| `WorkspaceAwarePage` | Subscribes/unsubscribes to workspace changes | Re-queries page data after interactive storage load or mutation |

The layout loads browser storage after interactivity starts. Version 1 and 2 snapshots are upgraded to version 3; an unknown version or unreadable protected content resets the workspace. Storage is browser/origin-specific, not a signed-in multi-device account store. PostgreSQL and EF Core are planned in the Phase 1 concept, not implemented.

## Known issues and technical limitations

The following findings are source-verified unless explicitly described as build observations. Runtime reproduction and fixes are still required.

| Finding | Evidence and impact | Follow-up |
|---|---|---|
| ~~Mobile drawer interop mismatch~~ — **fixed in D3 (`8c0554e`)** | Both exports were removed as "unused" in `3130302`; restored and verified (open, close, Escape, navigation) | None |
| ~~Patient context from program search is dropped~~ — **fixed in D4 (`d9fd22a`)** | Programs now passes the search's patient into the wizard with the program | None |
| ~~Resumed seed draft can retain stale checklist context~~ — **fixed in D3 (`8c0554e`)** | The seed checklist is reused only while the program is unchanged | None |
| Local submissions, uploads, profiles, authorization and runner requests are not visible to administrators | Staff service reads only `MockPortalStore`; everything the user creates (D2–D4) lives in their browser. The D3 correction return-to-review happens on the user's local copy without a reviewer; D4 requests stay “Being reviewed” and D6 runner requests stay “Waiting for a runner” | Choose shared workflow persistence before D9 administrator reviews |
| Upload reviews are not decided | D3 uploads stay `Pending`; nothing marks a document accepted or rejected, so “Ready” counts only change in seed data | D9: reviewer accept/reject with reason, using `UploadReviewState` and `ReviewNote` |
| Management pages are placeholders | Patient profiles (D4, `d9fd22a`), hospitals (D5, `b42be6d`) and runner requests (D6, `5ecfb68`) are implemented. The settings page and staff management/task routes still use placeholder components; `/admin/authorizations` awaits D9 | Implement the pending modules below |
| ~~Notification read state cannot be changed~~ — **fixed in D7 (uncommitted)** | Read/unread per notification and mark-all, saved in the user's browser. Staff notification pages remain placeholders | Staff notifications with D9/D10 |
| Persistence and recovery are limited | Browser storage has no account isolation, multi-device sync or cross-tab conflict handling. D3 now reports save failures to the user and rolls back uploads/submissions that were not saved | Idempotency and eventual database persistence |
| Stored data is tied to the app's build location | Observed during D3 verification: ASP.NET Core Data Protection isolates keys per application location by default, so a build run from a different folder could not decrypt data saved by another (the workspace safely reset). Moving or republishing the app to a new folder would reset users' stored drafts | Set a fixed Data Protection application name and key location before any shared deployment (would reset existing prototype data once) |
| References are local | Generator considers known seed/local references only | Enforce unique references transactionally when adding shared persistence |
| Verification is not automated | No dedicated test project; D3 and D4 were verified with scripted browser checks in the review session (not committed). The owner's running app can lock the default build output | Add focused behavioral tests (validation, versioning, migration, references, profile rules); repeat vulnerability audit with connectivity |
| Profiles cannot be removed | D4 adds and edits profiles; there is no archive/remove flow, and an added patient with applications must stay for history | Decide archive rules (D8 or later) |
| Authorization is not identity verification | Requests use fictional sample files; nothing verifies a real relationship or identity, and approvals only exist in seed data until D9 | D9 review actions; real verification needs authentication and shared persistence |
| Hospital directory is fictional and static | Six seeded hospitals with invented hours; no administrator editing, geolocation or live availability | Staff-managed directory with shared persistence (later milestone) |

Only fictional records and simulated documents belong in this prototype. Real medical records, official submissions, payment processing, real dispatch, AI/OCR, SMS/email and production compliance readiness are outside the delivered scope.

## Remaining Phase D checklist

**Agreed roadmap:** The owner supplied the following D3–D10 scope. Existing display components are foundations, not completion of these workflows. Every unchecked item is pending; D3 is committed; D4 is committed (`d9fd22a`); D5 is committed (`b42be6d`); D6 is committed (`5ecfb68`); D7 is implemented but not yet committed.

- [x] **D3 — Simulated Document Uploads & Correction Workflow:** implemented and committed as `8c0554e`. Upload metadata, validation, replacement/versions, history, counts and correction return-to-review; fictional sample files only. Handoff to D9: `UploadReviewState` and `ReviewNote` are reserved for reviewer decisions.
- [x] **D4 — Patient Profile Management & Authorization Requests:** implemented, verified and committed as `d9fd22a` (pushed). Profile list/details/add/edit, reusable profiles in the wizard (add-and-return, pre-selection, authorization gate), simulated authorization requests with a supporting sample document. Handoff to D9: `AuthorizationRequest` status/decision/note fields and `IStaffPortalDataService.GetAuthorizationRequestsAsync`.
- [x] **D5 — Hospital Directory & Search:** implemented, verified and committed as `b42be6d` (pushed). Searchable directory and hospital details; the wizard uses the directory (pre-selection, “not listed at” notes) and Programs lists where each program is handled.
- [x] **D6 — Optional Medical Runner Requests (Patient Portal):** implemented, verified and committed as `5ecfb68` (pushed). Request list and form for permitted errands on eligible applications, cancel before assignment, detail-page integration and history; runner and application statuses stay independent. Assignment remains D9, execution D10.
- [x] **D7 — In-App Notifications & Read/Unread Management:** implemented and verified (**uncommitted**, awaiting approval). Read/unread and mark-all for seeded and own notifications, All/Unread filters, links to the related item; bell and dashboard follow. Staff notification pages remain placeholders.
- [ ] **D8 — User Profile & Settings:** replace settings placeholders with the agreed profile/settings flows. Seed account names and sidebar preferences exist; account/profile editing does not.
- [ ] **D9 — Administrator Workflows: Review Queue, Document Reviews, Authorization Approval & Runner Assignment:** extend the seeded dashboard into actionable reviews, correction reasons, authorization approval, readiness transitions and manual runner assignment/reassignment. Management routes remain placeholders; choose shared workflow persistence first.
- [ ] **D10 — Medical Runner Workflows: Assignments, Task Details, Status Updates & History:** replace runner task/history placeholders with assigned-task details, accept/decline, progress, completion and history. The dashboard currently displays seeded tasks; runner and application statuses must remain independent.

Integration testing, mobile/accessibility checks and the known-issue fixes apply across all phases. Authentication and PostgreSQL/EF Core remain explicit architecture prerequisites for a real multi-user service; their delivery milestone is not assigned by this roadmap.

## Next development task and prerequisites

**Next step: commit D7** (application changes and this document, including the preserved D6 hash record) once the owner approves. Then **D8 — User Profile & Settings**, in efficient mode.

D7 decisions recorded:

- [x] Read state is the user's choice, saved in their browser (snapshot version 5), and applies to seeded and own notifications.
- [x] Opening a notification marks it read; notifications link to their application, profile or runner page.

Before D8: decide which account fields are editable in a prototype without authentication, and whether settings include notification and language preferences.

## Updating this document

After each phase:

1. Inspect branch, `git status`, the relevant diff and commit history. Keep unrelated/untracked paths outside progress claims.
2. Update the reviewed date, application HEAD and snapshot. Record implemented-but-uncommitted work separately until a commit exists.
3. Add the commit subject/hash and revise only the affected progress rows and feature descriptions.
4. Check off tasks only when implementation and verification evidence support completion; keep acceptance gaps visible.
5. Update known issues, the next task, prerequisites and build/test results. Keep agreed roadmap scope separate from verified delivery.

### Update log

| Date | Change | Application baseline | Delivery state |
|---|---|---|---|
| 2026-11-12 | Initial source/history review through D2; recorded the owner's agreed D3–D10 roadmap and integration limitations | `8a98432` | Committed as `6b5791e` |
| 2026-11-12 | Snapshot refresh: branch HEAD `6b5791e`, docs commit added to history, drawer regression traced to `3130302`, D3 plan proposed for review | `8a98432` (app), `6b5791e` (HEAD) | Uncommitted (included with D3) |
| 2026-11-12 | D3 implemented and verified: simulated uploads, correction workflow, prerequisite fixes P1–P3; known issues updated (drawer, stale checklist fixed; Data Protection location finding added) | `6b5791e` + working tree | Uncommitted, awaiting approval |
| 2026-11-13 | Committed D3, P1–P3 and progress documentation; build passed with 0 warnings/errors; exact message: `feat(patient): implement simulated document uploads and corrections` | `8c0554e866647fdf50897a2319788ee783566d78` | D3 committed; this actual-hash/status update is uncommitted |
| 2026-11-13 | D4 implemented and verified: patient profiles, wizard reuse, simulated authorization requests, D9 request data; program-search patient carried into the wizard; known issues updated | `8c0554e` + working tree | Committed as `d9fd22a` |
| 2026-11-13 | Committed D4 and progress documentation (exact message: `feat(patient): implement patient profiles and authorization requests`), then pushed the branch (`57c4ee0..d9fd22a`) | `d9fd22aad6d0c7704e1ef5692ef075bdf83b8868` | D4 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-13 | D5 implemented and verified (efficient mode): hospital directory and search, hospital details, wizard and Programs integration | `d9fd22a` + working tree | Committed as `b42be6d` |
| 2026-11-13 | Committed D5 and progress documentation (exact message: `feat(patient): implement hospital directory and search`), then pushed (`d9fd22a..b42be6d`) | `b42be6df58a1c6d9241ec271df271209ba0f5df6` | D5 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-13 | D6 implemented and verified (efficient mode): optional runner requests, cancel before assignment, detail-page links and history; D1 needed-by date display fixed | `b42be6d` + working tree | Committed as `5ecfb68` |
| 2026-11-13 | Committed D6 and progress documentation (exact message: `feat(patient): implement optional runner requests`), then pushed (`b42be6d..5ecfb68`) | `5ecfb68aa55c2b496368bf700820801249f0ecad` | D6 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-13 | D7 implemented and verified (efficient mode): notification read/unread, mark all, filters and links | `5ecfb68` + working tree | Uncommitted, awaiting approval |

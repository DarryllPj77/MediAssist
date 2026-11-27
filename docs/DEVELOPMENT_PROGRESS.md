# MediAssist Development Progress

MediAssist is a C# and Blazor prototype for preparing and tracking medical assistance applications. Milestone 1 and Milestone 2 phases A–C, D1, D2 and D3 are committed. D3 includes simulated document uploads, the correction workflow and prerequisite fixes P1–P3. Administrator decisions, patient management and runner actions are still planned. **D4 (patient profiles and simulated authorization requests) is committed as `d9fd22a` and pushed. D5 (hospital directory and search) is committed as `b42be6d` and pushed. D6 (optional runner requests) is committed as `5ecfb68` and pushed. D7 (notification read/unread and links) is committed as `7f57383` and pushed. D8 (user profile and settings) is committed as `a5311c9` and pushed. D9a (shared workflow store and administrator reviews) is committed as `b81d258` and pushed. D9b (authorization approval and runner assignment) is committed as `22846b1` and pushed. D10 (medical runner workflows) is committed as `9a50d89` and pushed.**

## Current snapshot

| Field | Verified state |
|---|---|
| Reviewed | 26 November 2026, Asia/Manila (M3.4a implementation review) |
| Branch | `design/premium-civic-imagery` |
| Branch HEAD | `3eaf5fd` — feat(auth): check roles in the shared service and record who did what (pushed; in sync; CI green) |
| Latest application commit | `3eaf5fd69afee05dfc72ca480046b1315528da71` — M3.4b |
| Latest implementation | M3.5 — Data ownership (**implemented, uncommitted**) |
| Application changes awaiting commit | M3.5 — see “M3.5 Data Ownership” below |
| Documentation awaiting commit | The M3.4b hash/CI record and this M3.5 update |
| Existing untracked paths | `.agents/`, `.claude/`, `skills-lock.json`; excluded from progress claims and preserved |
| Overall status | Milestone 2 committed. Milestone 3: M3.1–M3.4 committed and pushed; M3.5 implemented, awaiting commit approval |
| Next development task | Commit M3.5 once approved; then M3.6 — database readiness (migrations for the workflow store, concurrency token, tolerant loading) |

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
- D8 verification (13 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `git diff --check` clean. Scripted browser suite passed **160 of 160** page checks (40 routes, including the three new settings pages, × 320, 768, 1024 and 1440 px): overflow, 44 px targets, 13 px text, one h1, at most one filled button, duplicate ids, ARIA references, heading order and AA contrast. The suite was rebuilt for D8; its first run flagged row-title links whose whole row is the click area (stretched links, unchanged since earlier phases), so the target rule now skips links with a stretched click area. Flow checks are listed under D8 below. The version 5 → 6 snapshot upgrade uses the same generic upgrade path verified with real older builds in D3 and D4.
- D9a verification (17 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `dotnet test` **16 of 16** passed (new `tests/MediAssist.Web.Tests`); `git diff --check` clean. Scripted browser suite at 320, 768, 1024 and 1440 px over 47 routes (including the queue, its filters and six review pages): **188 of 188** page checks pass. The first run found 10 small-text failures (the reference in the review page's eyebrow at 320 and 768 px); after the fix those pages were rechecked. Flow checks are listed under D9a below.
- D9b verification (17 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `dotnet test` **23 of 23** passed (7 new); `git diff --check` clean. Scripted browser suite at 320, 768, 1024 and 1440 px over 49 routes (including both new pages, their filters and the `?request=` highlight): **196 of 196** page checks pass. Flow checks are listed under D9b below.
- D10 verification (17 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `dotnet test` **35 of 35** passed (12 new); `git diff --check` clean. Scripted browser suite at 320, 768, 1024 and 1440 px over 51 routes (including every runner page, their filters and five errand pages): **204 of 204** page checks pass. Flow checks are listed under D10 below.
- M3.2 verification (25 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**; `dotnet test` **38 of 38** passed (3 new workspace-level regressions; one rule test updated); `git diff --check` clean. Scripted browser suite at 320, 768, 1024 and 1440 px over 51 routes: **204 of 204** page checks pass. Flow checks are listed under M3.2 below.
- M3.3 verification (25 November 2026): Debug and Release builds of the solution, **0 errors, 0 warnings**; `dotnet test tests/MediAssist.Web.Tests` **57 of 57** passed (19 new); `dotnet test tests/MediAssist.Web.E2E` **4 of 4 widths passed (204 page checks, 5 min 18 s)** against the real app with a throwaway database, in headless Microsoft Edge; `git diff --check` clean. The first CI run (GitHub Actions run 45627532729, ubuntu-latest, Chromium) passed every step: 57 of 57 unit and workflow tests and 4 of 4 browser page-check widths (5 min 24 s).
- M3.4a verification (26 November 2026): `dotnet build`, **0 errors, 0 warnings**; `dotnet test tests/MediAssist.Web.Tests` **61 of 61** passed (4 new: Identity seeding and role mapping); `dotnet test tests/MediAssist.Web.E2E` **17 of 17 passed (page checks for 4 accounts at 4 widths, 220 page checks, and 13 access tests; 5 min 51 s)**, signed in as each role's demo account; `git diff --check` clean. Flow checks are listed under M3.4a below.
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
| D7 — In-App Notifications & Read/Unread Management | Completed | **Committed** | `7f57383`; read/unread per notification, mark all read, All/Unread filters, links to related items; bell and dashboard follow |
| D8 — User Profile & Settings | Completed | **Committed** | `a5311c9`; patient account details (synced with the “You” profile), display preferences for every role, clear saved data; staff settings read-only |
| D9a — Shared workflow store & administrator reviews | Completed | **Committed** | `b81d258`; SQLite (EF Core) shared store, review queue and page, document decisions and status changes, xUnit tests |
| D9b — Authorization approval & runner assignment | Completed | **Committed** | `22846b1`; shared authorization and runner requests; `/admin/authorizations` and `/admin/runners`; application status independent |
| D10 — Medical runner workflows | Completed | **Committed** | `9a50d89`; one list of runner requests and tasks; runner picker; accept/decline, on my way, done, hand back; application status independent |
| M3.1 — Architecture & security audit | Completed | Plan only | Findings and approved Milestone 3 roadmap below |
| M3.2 — Workflow fixes (F1–F3) | Completed | **Committed** | `3d834c1`; notifications scoped to the family; review and runner help blocked after a rejected authorization; clearing keeps added patients with shared records |
| M3.3 — Wider automated tests and CI | Completed | **Committed** | `6f7841d`; workspace, failure, concurrency and validation tests; Playwright page checks; GitHub Actions CI |
| M3.4a — Sign-in and roles | Completed | **Committed** | `4aa960d`; Identity in its own SQLite file with migrations; demo accounts (Development); every portal requires its role; layout and runner from the signed-in account |
| M3.4b — Who did what, service role checks, account name | Completed | **Committed** | `3eaf5fd`; shared actions record the account; the service refuses the wrong role; patient name on the account |
| M3.5 — Data ownership | Implemented and verified | **Uncommitted** | Patient data saved per account on the server; per-account household; owner on every shared record |
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
| 2026-11-13 | `7f57383` | feat(patient): implement notification read state and links |
| 2026-11-13 | `a5311c9` | feat(settings): implement user profile and settings |
| 2026-11-17 | `b81d258` | feat(admin): add shared workflow store and application reviews |
| 2026-11-17 | `22846b1` | feat(admin): add authorization approval and runner assignment |
| 2026-11-17 | `9a50d89` | feat(runner): implement runner task workflows |
| 2026-11-25 | `3d834c1` | fix(workflow): scope notifications and enforce authorization standing |
| 2026-11-25 | `6f7841d` | test: add workspace and validation tests, browser page checks and CI |
| 2026-11-27 | `4aa960d` | feat(auth): add sign-in, demo accounts and role-based portal access |
| 2026-11-27 | `043efc7` | fix(auth): let the sign-in demo note wrap on narrow screens |
| 2026-11-27 | `3eaf5fd` | feat(auth): check roles in the shared service and record who did what |

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

### D7 In-App Notifications & Read/Unread Management

Committed on 13 November 2026 as `7f573838c53f0081119e6168f019b53f13257559` with message `feat(patient): implement notification read state and links` (10 application files and the progress document, which also carried the D6 hash record), then pushed: `5ecfb68..7f57383`. `git pull --rebase` beforehand reported the branch up to date.

Built in efficient mode. Decisions: read state covers **seeded and the user's own** notifications; every notification links to the related **application, patient profile or runner page**.

1. `/patient/notifications`: All / Unread filter links (`?show=unread`, counts announced as “notifications”), “Mark all as read” (shown only while something is unread), and per notification “Mark as read” / “Mark as unread”. Each title links to its item; opening it marks it read and still navigates. Every change is announced (“… marked as read.”); a failed save says so and changes nothing. An empty Unread view shows “You're all caught up”.
2. The bell count and the dashboard's latest notifications follow the read state; dashboard titles link to their item and mark it read on open.
3. Links: seeded notifications carry links; new ones (submission, correction, authorization, runner request) store theirs; notifications saved before D7 get a link worked out from their id.

**Data:** `PortalNotification` gains optional `Href`; new `NotificationReadState`. Workspace snapshot **version 5** adds the user's read/unread choices, which override each notification's default (`ApplicationWorkspace.WithReadState`, `SetReadAsync`). `FilterChips` gains a `Noun` parameter so screen-reader counts name what they count.

**Flow checks performed (browser, 13 November 2026):** initial 3 unread (bell 3); mark one read (bell 2, Unread chip 2, row moves down); mark a read one unread (bell 3); mark all read (bell cleared, “You are up to date.”); Unread filter with one item and the active chip; opening that item's link navigated to MA-2026-00431 and cleared it (“You're all caught up”); state kept across reloads; dashboard shows read state and links; a new runner-request notification arrived unread with its link. Fixed during verification: filter chips announced “applications” instead of “notifications”.

### D8 User Profile & Settings

Committed on 13 November 2026 as `a5311c965024f85cf6e85db02e426dcdd2a7c63a` with message `feat(settings): implement user profile and settings` (24 application files and the progress document, which also carried the D7 hash record), then pushed: `7f57383..a5311c9`. `git pull --rebase` beforehand reported the branch up to date.

Built in efficient mode. Decisions: in a prototype without sign-in, the patient may edit **first name, last name and an optional email** (fictional, `example.com`); staff account details are **read-only**. Settings include **display preferences** (sidebar layout, Filipino helper text) for every role; notification preferences are left out because MediAssist sends nothing outside the app.

1. `/patient/settings` replaces the placeholder:
   - **Your account:** first and last name and optional email, validated at `ApplicationWorkspace.SaveAccountAsync` (errors under each field, summary announced, nothing saved on error). Saving also renames the “You” patient profile in the same save; the header, drawer, dashboard greeting and profiles follow at once. The “You” profile editor now shows the name as fixed with a link to Settings, and the save boundary keeps the account holder's name, so the two never disagree.
   - **Display:** sidebar layout (Automatic, Always full, Always compact; tablets and computers) and Filipino helper text (Show, English only). A choice applies at once and is kept in this browser; each change is announced.
   - **Data saved in this browser:** what the browser holds (drafts, sent applications, uploads, added patients) and “Clear my saved data” behind an inline confirmation (“Keep my data” cancels). Clearing resets the workspace only; seeded sample data stays. Focus moves to the question, then back to the section heading, with a status message.
2. `/admin/settings` and `/runner/settings` replace the staff placeholders: name and role (read-only, “managed by an administrator”) plus the same display preferences.
3. The sidebar toggle and Settings share one preference: `UiPreferences` (scoped) holds the sidebar mode and Filipino choice, saved in plain `localStorage` (`mediassist.sidebar`, `mediassist.filipino`; nothing personal). The layout sets `data-fil="hide"`, and `base.css` then hides every `lang="fil"` helper line.

**Data:** new `Models/Account.cs` (`AccountDetails`, `AccountRules`, `SidebarPreference`); `PortalUser` gains optional `Email`. Workspace snapshot **version 6** adds `Account` (null until the user changes it); `GetCurrentUserAsync` merges it. `ApplicationWorkspace.SaveAccountAsync` and `ClearAsync` restore the previous state if the browser save fails. `portalLayout.js` setters now report whether the browser kept the value. The routeless `PortalPlaceholder.razor` was deleted (settings was its last route).

**Flow checks performed (browser, 13 November 2026):** empty first name and malformed email gave two field errors, `aria-invalid` and the summary; a valid save (“Mariana Santos-Cruz”, `mariana@example.com`) showed “Your details are saved.”, renamed the header and the “You” profile, and its editor showed the fixed name with the Settings link; Always compact + English only set `data-sidebar="collapsed"`, `data-fil="hide"` (no visible Filipino lines), stored both and carried over to `/runner/settings`; Clear → Keep my data changed nothing; Clear → Yes restored the seeded account and announced it, with focus handled. Fixed during verification: the clear button's red outline lost to the button's own styles (equal specificity).

### D9a Shared Workflow Store & Administrator Reviews

Committed on 17 November 2026 as `b81d258d099084cf91dc9e00395bf6b05b9cbc37` with message `feat(admin): add shared workflow store and application reviews` (application files, the new test project, `.gitignore`, `MediAssist.slnx` and the progress document, which also carried the D8 hash record), then pushed: `a5311c9..b81d258`. `git pull --rebase` beforehand reported the branch up to date.

Approved plan (17 November 2026, “Approve D9”): SQLite with EF Core for submitted work; drafts and preferences stay in the browser; every browser is the same fictional account (no sign-in); D9 split into D9a and D9b; fixed Data Protection application name; an xUnit test project.

1. **Shared store.** `Data/WorkflowDbContext.cs` (EF Core SQLite, file `src/MediAssist.Web/App_Data/mediassist.db`, git-ignored, created on first run with `EnsureCreated`; no migrations yet). Rows keep the record as JSON plus the fields staff filter on. `Services/WorkflowService.cs` (singleton) holds an immutable read model loaded at start-up; every change runs through one gate, checks its rules, writes to the database and only then swaps the read model and raises `Changed`. Seeded applications stay in `MockPortalStore` until their first change copies them in at version 1.
2. **Patient side.** Submitting moves the application, its draft uploads and the confirmation into the shared store; the **reference is chosen there under the gate**, so two browsers can't take the same one. Uploads to submitted applications go to the shared store (an accepted document can't be replaced); uploads to drafts stay in the browser. The D3 correction rule (back to review once every sent-back document has a new copy) now runs in the shared store. Patient pages, the bell and the dashboard update live when a reviewer acts. Settings now says applications you sent stay when the browser is cleared.
3. **`/admin/reviews`** replaces the placeholder: every open application (all browsers and seeded data), oldest wait first, with stage filters (Waiting to start, In review, With the family, Ready for processing), flags for a pending authorization and documents waiting for a decision. The dashboard uses the same ledger (`Features/Admin/ReviewQueueTable`) and is live.
4. **`/admin/reviews/{reference}`**: the stage's one next step — Start review (blocked while the patient's authorization is pending), Mark ready for processing / Send back for correction, Mark processing complete (“doesn't decide on assistance; the program office does”). Per document: Accept, “Needs a new copy” or, for a missing one, “Ask for it”, each with a 10–300 character reason the family sees. Every action carries the version the reviewer saw; a stale action saves nothing and shows the latest version. History lists every decision.
5. **Rules** (`Models/Workflow.cs`, `ReviewRules`, `ChecklistOverlay`): only undecided latest uploads show as “being reviewed”; send-back needs every copy decided and at least one sent back; ready needs every required document accepted. New event kinds `DocumentAccepted`, `DocumentRejected` (appended).
6. **Data Protection** uses the fixed application name `MediAssist`, so moving the app no longer loses browser data. As approved, this **reset earlier prototype browser data once**. Because of that reset, step 3 of the approved plan (copying older browser submissions into the shared store) has nothing to read and was not built; no older data can be decrypted to copy.
7. **Tests:** `tests/MediAssist.Web.Tests` (xUnit, added to `MediAssist.slnx`): review rules and overlay; against a real SQLite file — unique references and persistence across restart, a full review round with one correction, stale version refused, accepted document not replaceable and scope checked, seeded application copied on first change, authorization guard.

**Flow checks performed (browser, 17 November 2026):** queue showed the five seeded open applications; a new wizard submission with one draft upload became MA-2026-00453 with the upload carried over and the draft gone; review started (family notified); a 6-character reason was refused with the field marked and focused; Prescription sent back, Clinical abstract and Valid ID asked for; “Mark ready” and early “Send back” were refused with reasons; sent back to the family, who saw status, notes, history and the bell (6); three new copies — the third returned it to review (“Every correction is in”); a second reviewer page refreshed live when the first accepted a document; accept ×2, ready, complete; the open patient page showed Completed live; after a server restart the application was still Completed; Start review on MA-2026-00395 was refused while Lia's authorization is pending. **Fixed during verification:** a document asked for (never sent) was labelled “Sent with the application”; the reference in the review page's eyebrow rendered at 11.4 px (same fix as D1).

**Not verified:** the stale-version message in the browser (the page refreshes live, so the race is hard to trigger; the rule is covered by a test); two physically separate browsers (both portals ran in one browser profile; the shared state is server-side and covered by tests).

### D9b Authorization Approval & Runner Assignment

Committed on 17 November 2026 as `22846b16a366dd1f48998e64765b4ad9c6b871ae` with message `feat(admin): add authorization approval and runner assignment` (application files, the new tests and the progress document, which also carried the D9a hash record), then pushed: `b81d258..22846b1`. `git pull --rebase` beforehand reported the branch up to date.

Second half of the approved D9 plan.

1. **Shared requests.** Authorization requests and runner requests move from the patient's browser into the shared store (two new tables; a database created by D9a gets them on start, since `EnsureCreated` doesn't change an existing file). An authorization request carries what the reviewer needs about the patient (name, relationship, date of birth) and the family's link to the profile, because profiles stay in the family's browser. A family member's authorization on the patient side now **follows their latest request** (waiting → being reviewed, approved → authorized, not approved → can ask again). A second request while one waits, or a second open runner request for an application, is refused under the gate (two browsers can't both ask).
2. **`/admin/authorizations`** replaces the placeholder: Waiting (oldest first) and Decided filters; each request shows relationship and age, the reason given, the simulated document and when it was asked. **Approve** appears only when the basis fits (a parent's request needs a patient under 18; otherwise a warning says why); **Don't approve** needs a 10–300 character note the family sees. The family's profile, notifications, the review queue's flag and the review's start guard all follow.
3. **`/admin/runners`** replaces the placeholder: Needs a runner / Assigned / Closed filters, `?request=` highlight (the dashboard's “Assign runner” link). A waiting or declined request gets a runner from the prototype's four fictional runners (the one who declined isn't offered); an assigned one can be changed until the runner accepts. **The application's status never changes**; the family is told, the runner page shows the runner, and the application history shows “Runner assigned”.
4. **Fix to D9a found while building:** reviewer actions on another account's seeded application (MA-2026-00452, Ramon) sent a notification to this account. Notifications now go only to this account's patients.
5. **Tests** (`StaffDecisionTests`, 7): the parent-of-minor rule; assignment rules; approving Lia's request lets MA-2026-00395's review start and survives a restart; a rejection needs a note, a stale decision is refused, the family can ask again but not twice at once; assigning never changes the application and another account gets no notification; one open family runner request per application and cancelling only the family's own; a D9a database gets the new tables.

**Flow checks performed (browser, 17 November 2026):** the family added Ana Santos (11) and requested authorization as her parent; the admin page listed Lia's and Ana's requests with ages; Lia approved; a 5-character note was refused with the field marked and focused; Ana not approved with a note; the family then saw Lia **Authorized**, Ana **Not approved** with a new request offered, and both notifications; MA-2026-00395 lost its “authorization also needs review” flag. A family runner request for MA-2026-00418 opened highlighted from `?request=`; Ramil Ocampo assigned, then changed to Cora Bautista; Lia's declined request (Liza Manalo) offered only the other three runners and got Paolo Dizon; the accepted request (rr-29) offered no change; the family's runner page showed each runner with cancel hidden; MA-2026-00418 stayed **Needs correction** with “Runner assigned: Cora Bautista” in its history.

**Limits:** the history keeps only the latest assignment (a change replaces the earlier entry); runner accept/decline/progress is D10; the runner's own pages still read seeded tasks.

### D10 Medical Runner Workflows

Committed on 17 November 2026 as `9a50d89332814a8a19f797ef66f3d4fa32996273` with message `feat(runner): implement runner task workflows` (application files, the new tests and the progress document, which also carried the D9b hash record), then pushed: `22846b1..9a50d89`. `git pull --rebase` beforehand reported the branch up to date.

Approved plan (17 November 2026, “Approve D10”), with every recommended choice.

1. **One list.** A runner's task is the runner request assigned to them; the separate seeded runner tasks (`rt-…`, which didn't match what administrators assign) are gone. The two with no request became seeded requests `rr-33` (assigned) and `rr-24` (done), and the seeded requests carry their steps (Liza's decline of `rr-27` now has a reason).
2. **Runner identity without sign-in.** Runner settings has **Viewing as** (the four fictional runners, saved in this browser as `mediassist.runner`, default Paolo Dizon; `RunnerSession`). The header, greeting and every runner page follow the choice. The page says it's a prototype choice, not a sign-in.
3. **Steps** (`RunnerRules.StepProblem`, `WorkflowService.RunnerStepAsync`): only the assigned runner acts. Assigned → **Accept** or **Decline** (reason, 10–200 characters); Accepted → **I'm on my way**; On the way → **Mark done** (optional note); Accepted or on the way → **Hand back** (reason). Each step is recorded with time, runner and note (`RunnerRequestItem.Steps`), and checks the version on screen. Decline and hand-back return the request to `/admin/runners` as “Declined, needs a new runner” with the runner's reason; that runner isn't offered again. Status label “In progress” is now **“On the way”**.
4. **Pages.** `/runner` (open errands, done recently; live), `/runner/tasks` (Open, To accept, In progress, Done), `/runner/tasks/{id}` (one next step, reason/note forms, focus moved to the next step after each action, “Runners never” list, what you did), `/runner/history` (done, declined, handed back, or given to another runner). The task card is shared (`Features/Runner/RunnerTaskCard`). The runner sees only the errand, place and district, needed-by time, the patient's first name, the reference and the family's notes. `/runner/notifications` stays a placeholder.
5. **Family and office.** The family is notified when a runner accepts, finishes, or hands back (“Finding another runner”, without the runner's reason); “on the way” shows on the runner page. The application history keeps **every** assignment and step (fixing the D9b limit). The family's runner card shows the runner's done note. The office sees decline/hand-back reasons and done notes. **The application's status never changes.**
6. **Fix found by the new tests:** notification ids used a millisecond timestamp, so two changes to one request in the same millisecond collided and the save failed. Ids now add a random suffix.
7. **Tests** (`RunnerStepTests`, 12): every step transition; only the assigned runner acts and reasons are required; a decline returns the errand, the next runner finishes it, the application status is unchanged, every runner keeps it in their history and the steps survive a restart; a hand-back after accepting and a stale step refused; the runner sees only what the errand needs.

**Flow checks performed (browser, 17 November 2026):** a family runner request for MA-2026-00418; assigned to Ramil; Runner settings switched to Ramil (header and greeting followed); Ramil's dashboard showed the errand with district and “For Jose”; a 4-character decline reason was refused with the field marked and focused; declined — the office saw “Ramil Ocampo declined: …” and was offered the other three runners; assigned to Cora; as Cora: accept → on my way → done with a note, the stepper and badge following and focus moving to the next step; the family's runner page showed Cora and the note, the notifications arrived, MA-2026-00418 stayed **Needs correction** and its history listed both assignments, the decline (as “Runner can't help”), acceptance, on the way and done; Ramil's errand page said he declined it and his history listed it; as Paolo: handed back the accepted rr-29 with a reason, and his dashboard showed rr-33 open and rr-24 done. **Fixed during verification:** after declining, the errand page still treated it as the runner's (empty next-step panel) and called it “handed back”.

## Milestone 3

Approved 25 November 2026 (“Approve Milestone 3”). M3.4 (sign-in) and M3.5 (data ownership) will each get their own plan for approval before any code.

### M3.1 Architecture & Security Audit (plan only)

Verified against the code at `9a50d89`; baseline `dotnet test` 35 of 35.

- **Workflow bugs:** F1 shared notifications were shown to every browser (`ApplicationWorkspace.Notifications` unfiltered; `ForAccount` treats unknown patients as this account), leaking other families' patient names. F2 a review could start, and runners be assigned, after the patient's authorization was rejected (`StartProblem` only checked “pending”). F3 “Clear my saved data” removed added patients, so their submitted applications vanished for the family while staff still saw them.
- **No authentication or authorization:** no sign-in, no `[Authorize]`; the URL decides the role, anyone can act as administrator or any runner, and shared actions record no real actor. Household and profiles live per browser; requests copy patient details at request time.
- **Concurrency and storage:** the write gate and versions protect one server process only (no database concurrency token); schema by `EnsureCreated` plus raw `CREATE TABLE IF NOT EXISTS`, no migrations; startup trusts every stored JSON row; the read model is rebuilt per read with nested loops; database path and Data Protection keys are not configurable for hosting; a draft submitted to the shared store can reappear if the browser save then fails.
- **Security and privacy:** already present — antiforgery, HTTPS/HSTS, encoded output (only static icon markup is raw), checked return URLs, simulated files, service-boundary validation. Missing — security headers (CSP, `X-Content-Type-Options`, frame-ancestors), rate limiting, an audit trail, retention/deletion for shared records, staff correction tools; free-text errand notes could carry personal details.
- **Consistency (minor):** copied seed applications keep fixed dates while untouched seeds move daily; runner requests stay open after an application is completed or cancelled.
- **Tests missing:** `ApplicationWorkspace` behaviour, data-service merging, validation rules (account, profile, runner request, simulated files), snapshot upgrade, save-failure path, concurrent submissions, component tests; the browser suite is not committed.

**Approved roadmap:** M3.2 workflow fixes → M3.3 wider automated tests (workspace, failure and concurrency paths, validation; commit the browser suite as Playwright tests; CI) → M3.4 sign-in and roles (ASP.NET Core Identity, policies, real runner identity, actor names) → M3.5 data ownership (profiles and accounts in the database) → M3.6 database readiness (EF Core migrations, concurrency token, configurable connection and keys, tolerant loading) → M3.7 hardening (security headers, rate limiting, audit log, retention, closing runner requests with their application, read-model performance).

### M3.2 Workflow Fixes

Committed on 25 November 2026 as `3d834c12d5a8e6092e118be1a59502acdba416bd` with message `fix(workflow): scope notifications and enforce authorization standing` (application files, tests and the progress document, which also carried the D10 hash record and the M3.1 audit), then pushed: `9a50d89..3d834c1`. `git pull --rebase` beforehand reported the branch up to date.

1. **F1 — notifications reach only the family whose patient they concern.** `PortalNotification` gains optional `PatientId`; every shared notification (submission, review steps, correction, authorization requested/decided, runner requested/assigned/steps) records it, and `ApplicationWorkspace.Notifications` shows a shared one only when that patient is in this browser's household. Older notifications without a patient fall back to their application link.
2. **F2 — a rejected authorization blocks review and runner help.** `WorkflowService.Authorization(patientId)` (`AuthorizationStanding`: NotNeeded, Pending, Approved, Rejected) comes from the patient's latest request. Start review is refused while pending *or* rejected; assigning a runner and a family's runner request are refused while rejected. The review page, the review queue (“Patient authorization wasn't approved”) and the family's runner form say why. A new approved request lifts the block.
3. **F3 — clearing keeps added patients that have shared records.** `ClearAsync` keeps added (non-seeded) profiles with a shared application or authorization request (`WorkflowService.HasRecordsFor`); Settings says so. Losing browser storage in other ways still hides them until M3.5.
4. **Tests** (`FamilyScopeTests`, 3, driving two browser workspaces over one store with in-memory browser storage): F1 notifications stay with their family, including the office's decision; F2 review, runner assignment and the family's runner request refused after a rejection, and allowed again after a new approval; F3 clearing keeps the patient with a request and drops one without. `ReviewRulesTests` updated for the new start rule.

**Flow checks performed (browser, 25 November 2026):** the office didn't approve Lia's request; the review queue flagged MA-2026-00395 “Patient authorization wasn't approved”; its review page explained it and Start review was refused; assigning a runner to rr-27 was refused; the family's runner request for MA-2026-00395 was refused under the application field. F1 and F3 need two separate browsers, so they are covered by the workspace tests rather than the single browser pane.

### M3.3 Wider Automated Tests

Committed on 25 November 2026 as `6f7841d1026c83cfc190df5b90e55b570202bf01` with message `test: add workspace and validation tests, browser page checks and CI` (tests, the Playwright project, the CI workflow, `.gitignore`, `MediAssist.slnx`, `Program.cs` and the progress document, which also carried the M3.2 hash record), then pushed: `3d834c1..6f7841d`. `git pull --rebase` beforehand reported the branch up to date. The first CI run (GitHub Actions run 45627532729, ubuntu-latest, Chromium) passed every step: 57 of 57 unit and workflow tests and 4 of 4 browser page-check widths (5 min 24 s).

1. **Shared test helpers** (`tests/MediAssist.Web.Tests/TestSupport.cs`): `TestClock` (Philippine time, settable), `TestDatabase` (throwaway SQLite file; `Fail` makes every write fail), `MemoryBrowserStorage` (`FailSaves` refuses saves) and `TestHarness` (the real store, shared workflow and any number of browser workspaces). The four existing test classes now use them instead of their own copies.
2. **New tests (19):**
   - `WorkspaceTests` (8): a submitted draft and its uploads move to the shared store and leave the browser; a failed shared save keeps the draft; a failed browser save undoes a draft upload; a failed shared save changes nothing and the next try works; **12 simultaneous submissions get 12 different references** and survive a restart; the account name belongs to the account (the “You” profile can't rename itself); older (version 1) browser data loads; the patient portal never shows another account's application.
   - `ValidationRulesTests` (11 cases): account details and limits; patient profile fields; the parent-of-minor basis; runner request fields, date range and open-request rule; simulated file types and size.
3. **Browser page checks as tests** (`tests/MediAssist.Web.E2E`, Playwright 1.65.1): `AppFixture` starts the built app on a free local port with `--Workflow:DatabasePath` set to a throwaway file, then one headless browser (installed Microsoft Edge on Windows, so no download; Chromium on CI). `PageChecksTests` runs the same checks as the review-session suite — overflow, 44 px targets, 13 px text, one h1, at most one filled button, unique ids, ARIA references, heading order, AA contrast — over 51 routes at 320, 768, 1024 and 1440 px, and lists every failure.
4. **Configurable database path:** `Program.cs` reads `Workflow:DatabasePath` (relative to the content root; default `App_Data/mediassist.db`, unchanged), so tests never touch the development database.
5. **CI** (`.github/workflows/ci.yml`): on pushes to `main` and `design/premium-civic-imagery` and on pull requests — restore, Release build, unit and workflow tests, install Chromium, browser page checks, keep the test results. Read-only repository permissions.
6. **Found while preparing the commit:** the template `.gitignore` pattern `*.e2e` (Visual Studio trace files) matched the `MediAssist.Web.E2E` folder on Windows, where Git ignores letter case, so the browser tests would have been left out silently. `.gitignore` now re-includes `/tests/MediAssist.Web.E2E/` (its `obj` and `bin` stay ignored).

### M3.4 Sign-in and Roles

**Approved plan** (26 November 2026, “Approve M3.4”, all recommended choices): ASP.NET Core Identity with cookie sign-in (no registration, email confirmation or password reset yet — no email sending); accounts in a separate SQLite file with EF Core migrations; fictional demo accounts seeded in Development only with one demo password in `appsettings.Development.json`; one portal per role with “Access denied” for the others; the runner is the signed-in account (picker removed); shared actions record who did them and the service checks roles (M3.4b); the patient's name moves to the account (M3.4b); split into M3.4a and M3.4b.

#### M3.4a Sign-in and Roles

Committed on 27 November 2026 as `4aa960d26261ddadf3f24a669e8b50310b3bc6af` with message `feat(auth): add sign-in, demo accounts and role-based portal access` (application files, the Identity migration, tests, `dotnet-tools.json` and the progress document, which also carried the M3.3 hash/CI record), then pushed: `6f7841d..4aa960d`. `git pull --rebase` beforehand reported the branch up to date. The CI run (GitHub Actions run 45909596777) passed the build and 61 of 61 unit tests but **failed 1 of 17 browser tests**: the signed-out sign-in page overflowed by 2 px at 320 px on Linux Chromium (the unbreakable file name in the demo-accounts note; Linux monospace is wider than Windows). The fix — letting that file name wrap — was committed as `043efc7` (`4aa960d..043efc7`); its CI run 45910549673 passed: 61 of 61 unit tests and 17 of 17 browser tests.

1. **Accounts** (`Data/Identity.cs`, `Data/IdentitySeeder.cs`): `ApplicationUser` (first and last name), roles Patient, Administrator and Runner, `MediAssistIdentityDbContext` in `App_Data/mediassist-identity.db` (configurable as `Identity:DatabasePath`; git-ignored) with the first migration `Data/Migrations/Identity/…_InitialIdentity`. The app applies migrations at start-up. **In Development only**, it seeds the roles and six fictional accounts — `maria.santos@` (Patient), `ana.reyes@` (Administrator), `paolo.dizon@`, `liza.manalo@`, `ramil.ocampo@`, `cora.bautista@` (Runners), all `@example.com` — with the password in `appsettings.Development.json` (`Seed:DemoPassword`; a demo value, never used outside Development). The account's first and last name are added to the signed-in user (`AppClaimsFactory`). Packages: `Microsoft.AspNetCore.Identity.EntityFrameworkCore` 10.0.15, `Microsoft.EntityFrameworkCore.Design` 10.0.15 (build-time only); `dotnet-ef` 10.0.15 as a local tool (`dotnet-tools.json`).
2. **Sign-in and sign-out:** `/account/login` (a plain page so the cookie can be set): email, password, “Keep me signed in”, **lockout after 5 failed tries for 5 minutes**, a safe `returnUrl` (local, outside /account) or the account's own portal; in Development it lists the demo accounts (not the password). Sign-out is a form post to `/account/logout` with an antiforgery token, from the sidebar footer, the mobile menu and the access-denied page. Cookie: `MediAssist.Auth`, HttpOnly, SameSite=Lax, secure outside Development, 8-hour sliding expiry.
3. **Access by role:** each portal folder requires its role (`Pages/Patient`, `Pages/Admin`, `Pages/Runner` `_Imports.razor`); the shared staff placeholder was split so each portal keeps its own rule. Signed-out visitors are redirected to sign-in and brought back; another role's portal shows **“This page isn't for your account”** (`/account/access-denied`) — for direct addresses (before rendering) and in-app navigation (`AuthorizeRouteView` → `RedirectToAccount`). `/` sends a signed-in account to its portal.
4. **The account decides the portal:** `PortalLayout` takes its role from the signed-in user (`RoleNavigation.ForUser`), no longer from the address; signed-out visitors (not-found and error pages only) get the plain account frame. Staff names come from the account.
5. **Runner identity:** `RunnerSession` is now the signed-in Runner account; the D10 “Viewing as” picker, its setting and its browser storage were removed.
6. **Tests:** `IdentityTests` (4): migration and seeding create the roles and six accounts once, with names, password and lockout; each role opens its own portal. Browser: the page checks now run signed in as each portal's demo account (plus sign-in, access-denied and not-found pages signed out); new `AccessTests` (13): signed-out visitors sent to sign-in with the page kept, each account lands in its portal, four cross-role attempts denied, a runner can't open another runner's errand, signing out ends access.

**Flow checks performed (browser, 26 November 2026):** signed out, `/` and `/admin` redirected to sign-in with the address kept, and an unknown page still answered 404 in the plain frame; signed in as Ana from `/admin/reviews` and returned there, with her name in the header; `/patient/applications` (direct) and `/runner/tasks` (in-app link) both showed access denied; signing out from there returned to sign-in (“You're signed out.”) and `/admin` redirected again; Paolo landed on `/runner` with only his two errands and no picker in settings; Liza's account locked on the 5th wrong password and refused even the right one while locked.

#### M3.4b Who Did What, Service Role Checks, Account Name

Committed as `3eaf5fd69afee05dfc72ca480046b1315528da71` (`043efc7..3eaf5fd`); CI run 45913045830 passed: 64/64 unit, 17/17 browser.

- **Role checks in the shared service:** every `WorkflowService` change takes an `Actor` (id, name, role) from the signed-in account (`ICurrentActor` / `ClaimsCurrentActor`) and refuses the wrong role — family actions need Patient, reviews/authorizations/assignment need Administrator, runner steps need Runner and act only as that runner. Pages pass it (`StaffWorkflowPage.Me`; the patient workspace).
- **Who did what:** new history entries store `ActorId`/`ActorName` (families still see “Reviewer”; the staff review page shows “Ana Reyes (Reviewer)”), authorization decisions store `DecidedById/ByName` (shown on the decided list), runner steps store `ById/ByName` (assignments record the administrator).
- **Account name:** the patient's first and last name live on the Identity account (`IAccountService` / `IdentityAccountService`), edited in Settings; the sign-in email is shown read-only. The “You” profile follows the account (also in a fresh browser and after clearing saved data). The D8 browser-stored account is no longer used.
- **Tests:** `ActorTests` (3: wrong roles refused with nothing saved, who-did-what recorded and kept after restart, a runner acts only as themselves); all existing tests pass the actor; the account test now checks the “You” profile follows the account. **64 of 64** unit tests; browser suite **17 of 17**; build 0 errors, 0 warnings.
- **Checked by hand:** Maria renamed herself in Settings → header and “You” profile changed, and stayed after clearing the browser; Ana started a review → history shows “Ana Reyes (Reviewer)”.

### M3.5 Data Ownership (uncommitted)

Approved 27 November 2026 (“Approve M3.5”, recommended choices), built in prototype mode.

- **Per-account storage:** each patient account's drafts, uploads to drafts, profiles and read states are saved to its own row (`UserWorkspaces`, migration `AddUserWorkspaces` in the account database) by `ServerWorkspaceStorage`, replacing the browser's protected localStorage. They follow the account to any browser; no other account can read them. Display preferences stay in the browser. Older browser-saved patient data is not imported (prototype data).
- **Per-account household:** the seeded household (Maria, Jose, Lia) belongs to the demo account `maria.santos@example.com` (`MockPortalStore.SeedOwnerId`, set at start-up); any other patient account starts with only its own “You” profile. A second fictional patient, `jun.delacruz@example.com`, is seeded in Development.
- **Owner on shared records:** applications, authorization requests, runner requests and notifications store `OwnerId` (the submitting account; seeded ones belong to Maria's account; Ramon's sample to no one). Patient views and notifications filter by owner, and the service refuses uploads, runner requests, cancellations and continuing a seeded draft for records the account doesn't own. The household guess (`ForAccount`) was removed. Records without an owner (from earlier development databases) are visible to staff only.
- **Settings:** “Data saved in this browser” is now “Your saved work” with “Reset my drafts and added patients”.
- **Fixed during tests:** updates to an application or runner request rebuilt the record and dropped its owner, so the owner's next action was refused; updates now copy the record.
- **Verification:** build 0 errors, 0 warnings; **67 of 67** unit tests (new `OwnershipTests`: a second account starts with only itself; two families never see each other's records and can't act on them; per-account server storage; F1 test now uses two accounts); browser suite **18 of 18** (new: Jun sees none of Maria's records).

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
| `ApplicationWorkspace` | Scoped to the Blazor circuit; drafts and their uploads, profiles, read states and account changes (browser); shows the household's part of the shared store (applications, uploads, authorization and runner requests) and passes the family's actions to it (D9a, D9b) | User-only work stays in the browser |
| `WorkflowService` (D9a) | Singleton; SQLite through EF Core (`App_Data/mediassist.db`); immutable read model, one write gate, versioned applications | Submitted applications, uploads, review decisions and their notifications, shared by every portal |
| `ProtectedLocalWorkspaceStorage` | ASP.NET Core protected browser `localStorage`, key `mediassist.workspace` | Data Protection protects the serialized snapshot; fixed application name `MediAssist` since D9a |
| `MockPatientPortalDataService` | Scoped; merges local submissions/drafts over matching seed references | Patient views include local changes |
| `MockStaffPortalDataService` | Scoped; reviews, authorization and runner requests, and runner tasks from `WorkflowService` (D9a, D9b, D10); the runner portal shows the runner chosen in settings (`RunnerSession`) | No seeded staff data remains outside the shared store except staff names |
| `WorkspaceAwarePage` | Subscribes/unsubscribes to workspace changes | Re-queries page data after interactive storage load or mutation |

The layout loads browser storage after interactivity starts. Version 1 and 2 snapshots are upgraded to version 3; an unknown version or unreadable protected content resets the workspace. Storage is browser/origin-specific, not a signed-in multi-device account store. PostgreSQL and EF Core are planned in the Phase 1 concept, not implemented.

## Known issues and technical limitations

The following findings are source-verified unless explicitly described as build observations. Runtime reproduction and fixes are still required.

| Finding | Evidence and impact | Follow-up |
|---|---|---|
| ~~Mobile drawer interop mismatch~~ — **fixed in D3 (`8c0554e`)** | Both exports were removed as "unused" in `3130302`; restored and verified (open, close, Escape, navigation) | None |
| ~~Patient context from program search is dropped~~ — **fixed in D4 (`d9fd22a`)** | Programs now passes the search's patient into the wizard with the program | None |
| ~~Resumed seed draft can retain stale checklist context~~ — **fixed in D3 (`8c0554e`)** | The seed checklist is reused only while the program is unchanged | None |
| ~~Authorization and runner requests are not visible to administrators~~ — **fixed in D9b (`22846b1`)** | Both are in the shared store with their decisions. Patient profiles themselves stay in the family's browser; a request copies what the reviewer needs | Profiles move with authentication (later) |
| ~~Upload reviews are not decided~~ — **fixed in D9a (`b81d258`)** | Reviewers accept, send back or ask for documents with a reason; uploads record the decision | None |
| Management pages are placeholders | Patient profiles (D4, `d9fd22a`), hospitals (D5, `b42be6d`) and runner requests (D6, `5ecfb68`) are implemented. Settings for all roles is implemented (D8, `a5311c9`). Staff management/task routes still use placeholder components; the review queue is implemented (D9a); `/admin/authorizations` and `/admin/runners` are implemented (D9b, `22846b1`); runner task pages are implemented (D10, `9a50d89`); programs, users and staff notifications remain placeholders | Implement the pending modules below |
| ~~Notification read state cannot be changed~~ — **fixed in D7 (`7f57383`)** | Read/unread per notification and mark-all, saved in the user's browser. Staff notification pages remain placeholders | Staff notifications with D9/D10 |
| Persistence and recovery are limited | Browser storage has no account isolation, multi-device sync or cross-tab conflict handling. D3 now reports save failures to the user and rolls back uploads/submissions that were not saved | Idempotency and eventual database persistence |
| ~~Stored data is tied to the app's build location~~ — **fixed in D9a (`b81d258`)** | Data Protection now uses the fixed application name `MediAssist`; this reset earlier prototype browser data once | None |
| ~~References are local~~ — **fixed in D9a (`b81d258`)** | References are chosen in the shared store under one gate | None while one server process owns the database |
| ~~Verification is only partly automated~~ — **addressed in M3.3 (`6f7841d`)** | 57 unit and workflow tests plus the browser page checks as Playwright tests, run by CI on every push. Component (bUnit) tests and multi-step browser flows are not automated yet | Add flow tests with sign-in (M3.4) |
| Profiles cannot be removed | D4 adds and edits profiles; there is no archive/remove flow, and an added patient with applications must stay for history | Decide archive rules (D8 or later) |
| Authorization is not identity verification | Administrators approve from fictional sample files (D9b); nothing verifies a real relationship or identity | Real verification needs authentication and real documents (outside the prototype) |
| Hospital directory is fictional and static | Six seeded hospitals with invented hours; no administrator editing, geolocation or live availability | Staff-managed directory with shared persistence (later milestone) |
| ~~Runner identity is a demo choice (D10)~~ — **fixed in M3.4a (`4aa960d`)** | The runner is the signed-in Runner account | None |
| Shared store limits (D9a) | No sign-in, so every browser is the same fictional account and sees the same submissions; one server process owns the SQLite file (the write gate is in-process); schema made with `EnsureCreated` plus create-if-missing for tables added later (no migrations); “Clear my saved data” doesn't remove submitted applications | Authentication, migrations and a server database before any shared deployment |
| ~~No authentication or authorization~~ — **sign-in and roles in M3.4a (`4aa960d`); actor recording and service role checks in M3.4b (`3eaf5fd`)** | Every portal needs its role, the shared service checks it again, and shared actions record the account | None |
| ~~Shared notifications shown to every browser~~ — **fixed in M3.2 (`3d834c1`)** | Notifications now carry their patient and are shown only to that family | None |
| ~~Review could start after a rejected authorization~~ — **fixed in M3.2 (`3d834c1`)** | Review start, runner assignment and family runner requests are refused while rejected | None |
| ~~Added patients live only in the browser~~ — **fixed in M3.5 (uncommitted)** | Profiles and drafts are saved per account on the server | None |
| Demo password in the repository (M3.4a) | `Seed:DemoPassword` in `appsettings.Development.json` seeds the fictional accounts in Development only; nothing is seeded elsewhere | Set real accounts and secrets before any non-development deployment |
| No registration or password reset (M3.4a) | Accounts are seeded; there is no sign-up, email confirmation or reset because the prototype sends no email | Later milestone |

Only fictional records and simulated documents belong in this prototype. Real medical records, official submissions, payment processing, real dispatch, AI/OCR, SMS/email and production compliance readiness are outside the delivered scope.

## Remaining Phase D checklist

**Agreed roadmap:** The owner supplied the following D3–D10 scope. Existing display components are foundations, not completion of these workflows. Every unchecked item is pending; D3 is committed; D4 is committed (`d9fd22a`); D5 is committed (`b42be6d`); D6 is committed (`5ecfb68`); D7 is committed (`7f57383`); D8 is committed (`a5311c9`); D9a is committed (`b81d258`); D9b is committed (`22846b1`); D10 is committed (`9a50d89`).

- [x] **D3 — Simulated Document Uploads & Correction Workflow:** implemented and committed as `8c0554e`. Upload metadata, validation, replacement/versions, history, counts and correction return-to-review; fictional sample files only. Handoff to D9: `UploadReviewState` and `ReviewNote` are reserved for reviewer decisions.
- [x] **D4 — Patient Profile Management & Authorization Requests:** implemented, verified and committed as `d9fd22a` (pushed). Profile list/details/add/edit, reusable profiles in the wizard (add-and-return, pre-selection, authorization gate), simulated authorization requests with a supporting sample document. Handoff to D9: `AuthorizationRequest` status/decision/note fields and `IStaffPortalDataService.GetAuthorizationRequestsAsync`.
- [x] **D5 — Hospital Directory & Search:** implemented, verified and committed as `b42be6d` (pushed). Searchable directory and hospital details; the wizard uses the directory (pre-selection, “not listed at” notes) and Programs lists where each program is handled.
- [x] **D6 — Optional Medical Runner Requests (Patient Portal):** implemented, verified and committed as `5ecfb68` (pushed). Request list and form for permitted errands on eligible applications, cancel before assignment, detail-page integration and history; runner and application statuses stay independent. Assignment remains D9, execution D10.
- [x] **D7 — In-App Notifications & Read/Unread Management:** implemented, verified and committed as `7f57383` (pushed). Read/unread and mark-all for seeded and own notifications, All/Unread filters, links to the related item; bell and dashboard follow. Staff notification pages remain placeholders.
- [x] **D8 — User Profile & Settings:** implemented, verified and committed as `a5311c9` (pushed). Patient account name and optional email (synced with the “You” profile), display preferences for every role, clear saved data with confirmation; staff account details are read-only.
- [x] **D9 — Administrator Workflows: Review Queue, Document Reviews, Authorization Approval & Runner Assignment:** split into D9a and D9b (approved 17 November 2026). **D9a committed as `b81d258` (pushed):** shared SQLite store, review queue, document decisions, correction round, ready/complete. **D9b committed as `22846b1` (pushed):** authorization approval and manual runner assignment.
- [x] **D10 — Medical Runner Workflows: Assignments, Task Details, Status Updates & History:** implemented, verified and committed as `9a50d89` (pushed). One list of runner requests and tasks; runner picker; accept/decline, on my way, done, hand back; family and office follow; application status independent. Staff notifications remain placeholders.

Integration testing, mobile/accessibility checks and the known-issue fixes apply across all phases. Authentication and PostgreSQL/EF Core remain explicit architecture prerequisites for a real multi-user service; their delivery milestone is not assigned by this roadmap.

## Next development task and prerequisites

**Next step: M3.4b** — record who did what on shared actions, role checks inside the shared service, and the patient's name on the account (Settings). M3.4a and its progress update are committed as `4aa960d`; this subsequent M3.4a hash record is uncommitted.

Before M3.4: choose the sign-in approach (ASP.NET Core Identity on the same SQLite database is the recommendation) and the fictional seed accounts for each role.

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
| 2026-11-13 | D7 implemented and verified (efficient mode): notification read/unread, mark all, filters and links | `5ecfb68` + working tree | Committed as `7f57383` |
| 2026-11-13 | Committed D7 and progress documentation (exact message: `feat(patient): implement notification read state and links`), then pushed (`5ecfb68..7f57383`) | `7f573838c53f0081119e6168f019b53f13257559` | D7 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-13 | D8 implemented and verified (efficient mode): account details, display preferences, clear saved data, staff settings | `7f57383` + working tree | Committed as `a5311c9` |
| 2026-11-13 | Committed D8 and progress documentation (exact message: `feat(settings): implement user profile and settings`), then pushed (`7f57383..a5311c9`) | `a5311c965024f85cf6e85db02e426dcdd2a7c63a` | D8 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-17 | D9 plan approved (D9a/D9b split); D9a implemented and verified: shared SQLite store, review queue and page, document decisions, tests | `a5311c9` + working tree | Committed as `b81d258` |
| 2026-11-17 | Committed D9a and progress documentation (exact message: `feat(admin): add shared workflow store and application reviews`), then pushed (`a5311c9..b81d258`) | `b81d258d099084cf91dc9e00395bf6b05b9cbc37` | D9a committed and pushed; this actual-hash update is uncommitted |
| 2026-11-17 | D9b implemented and verified: shared authorization and runner requests, authorization reviews, runner assignment, tests; D9a notification scope fixed | `b81d258` + working tree | Committed as `22846b1` |
| 2026-11-17 | Committed D9b and progress documentation (exact message: `feat(admin): add authorization approval and runner assignment`), then pushed (`b81d258..22846b1`) | `22846b16a366dd1f48998e64765b4ad9c6b871ae` | D9b committed and pushed; this actual-hash update is uncommitted |
| 2026-11-17 | D10 plan approved; implemented and verified: runner steps, runner pages and picker, one list of runner requests, tests; notification id collision fixed | `22846b1` + working tree | Committed as `9a50d89` |
| 2026-11-17 | Committed D10 and progress documentation (exact message: `feat(runner): implement runner task workflows`), then pushed (`22846b1..9a50d89`) | `9a50d89332814a8a19f797ef66f3d4fa32996273` | D10 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-25 | Milestone 3.1 audit (plan only) and roadmap approved; M3.2 implemented and verified: notification scoping, authorization guard, clear keeps patients with records, two-browser tests | `9a50d89` + working tree | Committed as `3d834c1` |
| 2026-11-25 | Committed M3.2 and progress documentation (exact message: `fix(workflow): scope notifications and enforce authorization standing`), then pushed (`9a50d89..3d834c1`) | `3d834c12d5a8e6092e118be1a59502acdba416bd` | M3.2 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-25 | M3.3 implemented and verified: shared test helpers, 19 new tests, Playwright page checks, CI workflow, configurable database path | `3d834c1` + working tree | Committed as `6f7841d` |
| 2026-11-25 | Committed M3.3 and progress documentation (exact message: `test: add workspace and validation tests, browser page checks and CI`), then pushed (`3d834c1..6f7841d`). The first CI run (GitHub Actions run 45627532729, ubuntu-latest, Chromium) passed every step: 57 of 57 unit and workflow tests and 4 of 4 browser page-check widths (5 min 24 s). | `6f7841d1026c83cfc190df5b90e55b570202bf01` | M3.3 committed and pushed; this actual-hash update is uncommitted |
| 2026-11-26 | M3.4 plan approved; M3.4a implemented and verified: Identity accounts and roles, sign-in/out, Development seed accounts, route protection, layout and runner from the account, tests | `6f7841d` + working tree | Committed as `4aa960d` |
| 2026-11-27 | Committed M3.4a and progress documentation (exact message: `feat(auth): add sign-in, demo accounts and role-based portal access`), then pushed (`6f7841d..4aa960d`). The CI run (GitHub Actions run 45909596777) passed the build and 61 of 61 unit tests but **failed 1 of 17 browser tests**: the signed-out sign-in page overflowed by 2 px at 320 px on Linux Chromium (the unbreakable file name in the demo-accounts note; Linux monospace is wider than Windows). The fix — letting that file name wrap — was committed as `043efc7` (`4aa960d..043efc7`); its CI run 45910549673 passed: 61 of 61 unit tests and 17 of 17 browser tests. | `4aa960d26261ddadf3f24a669e8b50310b3bc6af` | M3.4a committed and pushed; this actual-hash update is uncommitted |
| 2026-11-27 | Committed and pushed the M3.4a CI fix (exact message: `fix(auth): let the sign-in demo note wrap on narrow screens`, `4aa960d..043efc7`); CI run 45910549673 passed (61/61 unit, 17/17 browser). Owner set **prototype development mode** (implement directly, targeted checks, brief docs) | `043efc7bdbdf9fc1586732b401d126967f9ffd07` | Committed and pushed; this record is uncommitted |
| 2026-11-27 | M3.4b implemented and verified (prototype mode): actor role checks and who-did-what in the shared service, patient name on the account | `043efc7` + working tree | Committed as `3eaf5fd` |
| 2026-11-27 | Committed and pushed M3.4b (`feat(auth): check roles in the shared service and record who did what`, `043efc7..3eaf5fd`); CI run 45913045830 passed | `3eaf5fd69afee05dfc72ca480046b1315528da71` | This record is uncommitted |
| 2026-11-27 | M3.5 implemented and verified (prototype mode): per-account server storage, per-account household, owners on shared records, second demo patient | `3eaf5fd` + working tree | Uncommitted, awaiting approval |

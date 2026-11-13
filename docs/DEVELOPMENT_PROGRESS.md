# MediAssist Development Progress

MediAssist is a C# and Blazor prototype for preparing and tracking medical assistance applications. Milestone 1 and Milestone 2 phases A–C, D1 and D2 are committed. D3 (simulated document uploads and the correction workflow) is implemented and verified but **not yet committed**. Administrator decisions, patient management and runner actions are still planned.

## Current snapshot

| Field | Verified state |
|---|---|
| Reviewed | 12 November 2026, Asia/Manila |
| Branch | `design/premium-civic-imagery` |
| Branch HEAD | `6b5791e` — docs: add MediAssist development progress tracker |
| Latest application commit | `8a98432` — D2 Application Wizard and Draft Management |
| Latest implementation | D3 — Simulated Document Uploads & Correction Workflow (**uncommitted**) |
| Application changes awaiting commit | D3 and its prerequisite fixes P1–P3 (see “D3 Simulated Document Uploads” below); 16 application files |
| Documentation awaiting commit | This document: D3 status, features, known issues, verification results and update log |
| Existing untracked paths | `.agents/`, `.claude/`, `skills-lock.json`; excluded from progress claims and preserved |
| Overall status | Milestone 2 in progress; A–C and D1–D2 committed; D3 implemented and verified, awaiting commit approval |
| Next development task | Commit D3 (with this document) once approved; then plan D4 — Patient Profile Management & Authorization Requests |

**Evidence standard:** “Completed” below means implemented in the inspected source and present in the listed commit. It does not imply production readiness or complete browser testing. D3 application changes are present in the working tree and are **not** committed. Remote synchronization was not checked; no commit, push or merge is part of this documentation update.

## Evidence and verification

- Concept and business rules: [Phase 1 specification](MediAssist_Phase1_Documentation.docx), version 1.0. It records concept planning through Step 1.23 and describes Phase 2 implementation priorities.
- Phase D roadmap: the owner's agreed D1–D10 task list supplied on 12 November 2026. This defines the numbering below; implementation status is checked independently against source and Git.
- History: `git log` and relevant commit file summaries, checked against current source. Milestone 1 is identified explicitly in commit `5e2db14`.
- Application: [project](../src/MediAssist.Web/MediAssist.Web.csproj), [startup](../src/MediAssist.Web/Program.cs), routed pages, services, models, styles and navigation.
- Design documentation: [image slots](../src/MediAssist.Web/wwwroot/images/README.md) and [fonts](../src/MediAssist.Web/wwwroot/fonts/README.md). The image README still groups application pages among pages being built; actual D1 routes take precedence for implementation status.
- Build on this review: an isolated `dotnet build` succeeded with **0 errors and 2 NU1900 warnings** because the package vulnerability service was unreachable. The ordinary output-directory build failed with MSB3027/MSB3021 because a running app locked `MediAssist.Web.exe`; the running session was left intact.
- D3 verification (12 November 2026): `dotnet build` to a separate output folder, **0 errors, 0 warnings**. A scripted browser suite passed **248 of 248** page checks (62 routes × 320, 768, 1024 and 1440 px, with stored drafts, submissions and uploads present): no horizontal overflow, 44 px targets, 13 px minimum text, one h1, at most one filled button, no duplicate IDs or broken ARIA references, heading order and AA text contrast. Flow checks are listed under D3 below.
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
| D3 — Simulated Document Uploads & Correction Workflow | Implemented and verified | **Uncommitted** | Simulated uploads, validation, replacement/versions, history, counts, correction return-to-review; prerequisite fixes P1–P3 |
| Milestone 2 as a whole | In Progress | Partially delivered | Remaining modules and integration defects below |
| D4–D10 | Pending | Agreed roadmap | Some read-only foundations exist; remaining workflows are not complete |
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

### D3 Simulated Document Uploads & Correction Workflow (uncommitted)

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
| `ApplicationWorkspace` | Scoped to the Blazor circuit; drafts, submissions, notifications and (D3) simulated uploads; snapshot version 2 | User work is separate from the singleton seed store |
| `ProtectedLocalWorkspaceStorage` | ASP.NET Core protected browser `localStorage`, key `mediassist.workspace` | Data Protection protects the serialized snapshot; no database persistence |
| `MockPatientPortalDataService` | Scoped; merges local submissions/drafts over matching seed references | Patient views include local changes |
| `MockStaffPortalDataService` | Scoped; reads the seed store only | Staff views do not receive local D2 submissions |
| `WorkspaceAwarePage` | Subscribes/unsubscribes to workspace changes | Re-queries page data after interactive storage load or mutation |

The layout loads browser storage after interactivity starts. A version 1 snapshot is upgraded to version 2; an unknown version or unreadable protected content resets the workspace. Storage is browser/origin-specific, not a signed-in multi-device account store. PostgreSQL and EF Core are planned in the Phase 1 concept, not implemented.

## Known issues and technical limitations

The following findings are source-verified unless explicitly described as build observations. Runtime reproduction and fixes are still required.

| Finding | Evidence and impact | Follow-up |
|---|---|---|
| ~~Mobile drawer interop mismatch~~ — **fixed in D3 (uncommitted)** | Both exports were removed as "unused" in `3130302`; restored and verified (open, close, Escape, navigation) | None after commit |
| Patient context from program search is dropped | Program pre-selection is **fixed in D3 (uncommitted)**. `ProgramSearch` still sends `patient`, which Programs does not consume | Decide whether a chosen patient should carry into the wizard (D4/D5) |
| ~~Resumed seed draft can retain stale checklist context~~ — **fixed in D3 (uncommitted)** | The seed checklist is reused only while the program is unchanged | None after commit |
| Local submissions and uploads are not visible to administrators | Staff service reads only `MockPortalStore`; drafts, submissions and D3 uploads live in the user's browser. The D3 correction return-to-review happens on the user's local copy without a reviewer | Choose shared workflow persistence before D9 administrator reviews |
| Upload reviews are not decided | D3 uploads stay `Pending`; nothing marks a document accepted or rejected, so “Ready” counts only change in seed data | D9: reviewer accept/reject with reason, using `UploadReviewState` and `ReviewNote` |
| Profiles and management pages are placeholders | Patient profiles/hospitals/runner/settings and staff management/task routes use placeholder components | Implement the pending modules below |
| Notification read state cannot be changed | UI displays read/unread records and D2 adds local submission notices, but exposes no mark-read operation | Add recipient-scoped read-state updates |
| Persistence and recovery are limited | Browser storage has no account isolation, multi-device sync or cross-tab conflict handling. D3 now reports save failures to the user and rolls back uploads/submissions that were not saved | Idempotency and eventual database persistence |
| Stored data is tied to the app's build location | Observed during D3 verification: ASP.NET Core Data Protection isolates keys per application location by default, so a build run from a different folder could not decrypt data saved by another (the workspace safely reset). Moving or republishing the app to a new folder would reset users' stored drafts | Set a fixed Data Protection application name and key location before any shared deployment (would reset existing prototype data once) |
| References are local | Generator considers known seed/local references only | Enforce unique references transactionally when adding shared persistence |
| Verification is not automated | No dedicated test project; D3 was verified with scripted browser checks in the review session (not committed). The owner's running app can lock the default build output | Add focused behavioral tests (validation, versioning, migration, references); repeat vulnerability audit with connectivity |

Only fictional records and simulated documents belong in this prototype. Real medical records, official submissions, payment processing, real dispatch, AI/OCR, SMS/email and production compliance readiness are outside the delivered scope.

## Remaining Phase D checklist

**Agreed roadmap:** The owner supplied the following D3–D10 scope. Existing display components are foundations, not completion of these workflows. Every unchecked item is pending; D3 is implemented but not yet committed.

- [x] **D3 — Simulated Document Uploads & Correction Workflow:** implemented and verified (**uncommitted**, awaiting approval). Upload metadata, validation, replacement/versions, history, counts and correction return-to-review; fictional sample files only. Handoff to D9: `UploadReviewState` and `ReviewNote` are reserved for reviewer decisions.
- [ ] **D4 — Patient Profile Management & Authorization Requests:** add profile create/edit/select flows, relationship records and simulated authorization requests. Seed profiles and authorization badges exist; `/patient/patients` remains a placeholder. Approval belongs to D9.
- [ ] **D5 — Hospital Directory & Search:** replace `/patient/hospitals` with a searchable directory and hospital details. The wizard currently offers names derived from seed applications; the existing Programs search is not a hospital directory.
- [ ] **D6 — Optional Medical Runner Requests (Patient Portal):** replace `/patient/runner` with request creation and tracking, scoped to the patient's applications and permitted errands. Runner help cards and seeded request displays exist; assignment belongs to D9 and runner execution to D10.
- [ ] **D7 — In-App Notifications & Read/Unread Management:** extend the existing patient notification list, bell counts and D2 submission notices with persisted read/unread actions and event links. Staff notification pages remain placeholders.
- [ ] **D8 — User Profile & Settings:** replace settings placeholders with the agreed profile/settings flows. Seed account names and sidebar preferences exist; account/profile editing does not.
- [ ] **D9 — Administrator Workflows: Review Queue, Document Reviews, Authorization Approval & Runner Assignment:** extend the seeded dashboard into actionable reviews, correction reasons, authorization approval, readiness transitions and manual runner assignment/reassignment. Management routes remain placeholders; choose shared workflow persistence first.
- [ ] **D10 — Medical Runner Workflows: Assignments, Task Details, Status Updates & History:** replace runner task/history placeholders with assigned-task details, accept/decline, progress, completion and history. The dashboard currently displays seeded tasks; runner and application statuses must remain independent.

Integration testing, mobile/accessibility checks and the known-issue fixes apply across all phases. Authentication and PostgreSQL/EF Core remain explicit architecture prerequisites for a real multi-user service; their delivery milestone is not assigned by this roadmap.

## Next development task and prerequisites

**Next step: commit D3** (application changes and this document) once the owner approves. Then plan **D4 — Patient Profile Management & Authorization Requests**.

D3 decisions recorded:

- [x] Storage: browser-only mock metadata in the user's workspace (snapshot version 2); staff cannot see it until D9 chooses shared persistence.
- [x] Document identity: one `DocumentUpload` per version, linked by `ReplacesId`; validation rules in `SimulatedFiles`; history derived from uploads.
- [x] Flexible submission kept: missing documents never block submission; “uploaded / being reviewed” is separate from reviewer-accepted “ready”.
- [x] Save failures are reported and nothing is recorded unless saved.
- [x] Acceptance checks run (see D3 flow checks above).

Before D4: decide whether the program-search patient choice should carry into the wizard, and whether D4 profile/authorization data also stays browser-only.

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

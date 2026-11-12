# MediAssist image slots

Every photograph or illustration in the portal sits in a named **slot**. Until a file is supplied,
the slot shows a finished-looking, wordless stand-in (cream panel, woven edge, a faint bayanihan house).

## How to add an image

1. Export the file at the size below, as **WebP** (JPG or PNG also work).
2. Name it exactly `<slot key>.webp` and save it in this folder (`wwwroot/images/`).
3. Rebuild and run the app. The slot picks the file up automatically; no code change.
4. Check the alt text in `Models/ImageSlots.cs` still describes what the photo shows.

Open any page with `?slots=1` added to the address (for example `/patient?slots=1`) to see
every slot's key and size on top of it.

**Fallbacks:** any `hero-…` slot without its own file uses `hero-default.webp`;
`program-feature-<program id>` falls back to `program-feature.webp`.

## Slots

| Slot key | Where | Ratio (phone) | Deliver at | Subject and notes |
|---|---|---|---|---|
| `hero-patient-home` | Patient dashboard hero (cropped to 16:9 when stacked: keep the subject in the middle band) | 3:2 (4:3) | 2400×1600 | A family with a health worker; subject left of centre |
| `hero-admin` | Administrator dashboard hero | 3:2 (4:3) | 1800×1200 | Review work; no readable patient data |
| `hero-runner` | Runner dashboard hero | 3:2 (4:3) | 1800×1200 | A runner arriving at a hospital entrance |
| `hero-programs` | Programs page | 3:2 (4:3) | 2400×1600 | Family with a hospital social worker |
| `hero-documents` | Documents page | 3:2 (4:3) | 2400×1600 | Hands sorting forms; no readable personal details |
| `hero-notifications` | Notifications page | 3:2 (4:3) | 2400×1600 | Three generations looking at a phone |
| `hero-default` | Any page without its own hero file | 3:2 (4:3) | 2400×1600 | Family and health worker at a community health centre |
| `hero-patient-hospitals`, `hero-patient-patients`, `hero-patient-runner`, `hero-patient-settings`, `hero-patient-applications`, `hero-admin-reviews`, `hero-admin-runners`, … | Pages still being built: the compact hero beside the status message, wide screens only (key = `hero-` + first two URL segments) | 3:2 (4:3) | 2400×1600 | Optional; otherwise `hero-default` |
| `program-feature` / `program-feature-<id>` | Featured program on the dashboard (cropped to 16:9 when stacked) | 4:3 | 1600×1200 | Ids: `hospital-bill`, `medicine-cost`, `dialysis`, `diagnostics` |
| `banner-notice` | Dashboard "Latest notifications" | 16:9 | 1920×1080 | Health worker explaining a form to an older patient |
| `aside-runner` | Runner help card | 1:1 | 800×800 | Runner handing an envelope to a clerk |
| `og-share` | Link previews (future public pages) | 1.91:1 | 1200×630 | Brand image |

## Rules for every image

- **Weight:** hero images at most 250 KB, others at most 150 KB (WebP, quality about 75).
- **Rights:** only photographs MediAssist owns, commissioned or licensed. Model releases for
  recognisable people; no patients' real medical information in frame.
- **No text in the image** and no captions under it (headings sit beside the image instead).
- **No official marks:** no government seals, agency logos, flags or uniforms that imply MediAssist
  is a government service.
- **Alt text:** describe what is shown in one sentence (update `Models/ImageSlots.cs` if the photo differs).

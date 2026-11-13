using MediAssist.Web.Models;

namespace MediAssist.Web.Services;

/// <summary>
/// The one in-memory source of fictional data shared by the patient and staff mock services, so
/// every portal tells the same story (same references, statuses, dates and history).
/// Every person, hospital, program and reference number here is invented.
/// <para>
/// Times are written as offsets from the start of the current day and materialised on each call
/// (<see cref="Today"/>), so the prerendered and interactive renders agree and nothing drifts
/// after midnight. Read-only in this milestone: lists are rebuilt per call and never shared
/// mutable state, so the singleton is thread-safe. Write features (upload, submit) would add
/// methods here without changing the UI-facing services.
/// </para>
/// </summary>
public sealed class MockPortalStore(TimeProvider timeProvider)
{
    public static readonly Guid UserId = Guid.Parse("6f1d2c1e-0000-4000-8000-000000000001");
    private static readonly Guid MariaId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a1");
    private static readonly Guid JoseId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a2");
    private static readonly Guid LiaId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000a3");
    // Another account's patient: visible to staff only.
    private static readonly Guid RamonId = Guid.Parse("6f1d2c1e-0000-4000-8000-0000000000b1");

    public DateTimeOffset Today
    {
        get
        {
            var now = timeProvider.GetLocalNow();
            return new DateTimeOffset(now.Date, now.Offset);
        }
    }

    // ---- People ----

    public PortalUser CurrentUser { get; } = new(UserId, "Maria", "Santos", "maria.santos@example.com");

    /// <summary>The account holder's household: the patients this account applies for.</summary>
    public IReadOnlyList<PatientProfile> Household { get; } =
    [
        new(MariaId, "Maria Santos", ProfileRules.SelfRelationship, AuthorizationStatus.Self, new DateOnly(1984, 6, 9), "Quezon City"),
        new(JoseId, "Jose Santos", "Father", AuthorizationStatus.Authorized, new DateOnly(1951, 3, 14), "Quezon City"),
        new(LiaId, "Lia Santos", "Daughter", AuthorizationStatus.PendingReview, new DateOnly(2013, 8, 2), "Quezon City"),
    ];

    public bool IsHousehold(Guid patientId) => Household.Any(p => p.Id == patientId);

    /// <summary>
    /// Seeded authorization requests (Phase 1: an administrator reviews them). Jose's was approved;
    /// Lia's is waiting, which is why MA-2026-00395 is flagged in the review queue. D9 reviews these.
    /// </summary>
    public IReadOnlyList<AuthorizationRequest> AuthorizationRequests()
    {
        var t = Today;
        return
        [
            new("auth-jose", JoseId, AuthorizationBasis.SignedLetter, "signed-authorization-letter-scan.pdf", "application/pdf", 842_752,
                t.AddDays(-58).AddHours(10), AuthorizationRequestStatus.Approved, t.AddDays(-55).AddHours(15),
                "The letter is signed and dated by the patient."),
            new("auth-lia", LiaId, AuthorizationBasis.ParentOfMinor, "birth-certificate-scan.pdf", "application/pdf", 1_153_434,
                t.AddDays(-4).AddHours(8).AddMinutes(30)),
        ];
    }

    // ---- Programs (fictional names; "run by" is a generic office type, never a real agency) ----

    public IReadOnlyList<AssistanceProgram> Programs { get; } =
    [
        new("hospital-bill", "Hospital Bill Support", "Tulong sa bayarin sa ospital",
            "Hospital social service office",
            "Helps pay part of a hospital bill for patients who are admitted or about to be discharged.",
            "Room, procedures and medicines billed by the hospital",
            ["Clinical abstract", "Hospital bill or quotation", "Certificate of indigency", "Valid ID"]),
        new("medicine-cost", "Medicine Cost Assistance", "Tulong sa gamot",
            "City social welfare office",
            "Covers prescribed medicines that a patient cannot afford, in part or in full.",
            "Prescribed medicines from a partner pharmacy",
            ["Prescription", "Clinical abstract", "Valid ID"]),
        new("dialysis", "Dialysis Session Support", "Tulong sa dialysis",
            "Provincial health office",
            "Supports regular dialysis sessions for patients with kidney disease.",
            "Dialysis sessions at an accredited centre",
            ["Treatment plan", "Clinical abstract", "Certificate of indigency", "Valid ID"]),
        new("diagnostics", "Diagnostic Procedure Aid", "Tulong sa pagsusuri",
            "Hospital social service office",
            "Helps with the cost of imaging and laboratory procedures a doctor has requested.",
            "X-ray, CT, MRI and laboratory tests",
            ["Doctor's request", "Quotation", "Valid ID"]),
    ];

    // ---- Hospital directory (D5; fictional names, districts and hours) ----

    /// <summary>
    /// Hospitals and the programs their social service office handles. The first three are the
    /// hospitals named in the seeded applications, so every existing application has a directory entry.
    /// </summary>
    public IReadOnlyList<Hospital> Hospitals { get; } =
    [
        new("san-isidro-general", "San Isidro General Hospital", "Public general hospital", "San Isidro", "Quezon City",
            ["Emergency care", "Surgery", "Internal medicine", "Dialysis unit", "Pharmacy"],
            ["hospital-bill", "medicine-cost", "dialysis"],
            "Monday to Friday, 8:00 AM to 5:00 PM", "Ground floor, beside the billing office"),
        new("bayview-medical", "Bayview Medical Center", "Private general hospital", "Bayview district", "Pasig City",
            ["Emergency care", "Laboratory", "Imaging", "Pharmacy", "Outpatient clinics"],
            ["hospital-bill", "medicine-cost", "diagnostics"],
            "Monday to Saturday, 8:00 AM to 4:00 PM", "Second floor, patient relations"),
        new("northridge-childrens", "Northridge Children's Hospital", "Specialty hospital for children", "Northridge", "Quezon City",
            ["Pediatrics", "Laboratory", "Imaging", "Neonatal care"],
            ["hospital-bill", "diagnostics"],
            "Monday to Friday, 7:30 AM to 4:30 PM", "Main lobby, information desk"),
        new("malinis-district", "Malinis District Hospital", "Public district hospital", "Malinis", "Marikina City",
            ["Emergency care", "Maternity", "Internal medicine", "Pharmacy"],
            ["hospital-bill", "medicine-cost"],
            "Monday to Friday, 8:00 AM to 5:00 PM", "Ground floor, room 104"),
        new("riverbend-kidney", "Riverbend Kidney and Dialysis Center", "Specialty center", "Riverbend", "Pasig City",
            ["Dialysis", "Nephrology clinic", "Laboratory"],
            ["dialysis", "diagnostics"],
            "Monday to Saturday, 6:00 AM to 6:00 PM", "Reception, ground floor"),
        new("eastgate-diagnostics", "Eastgate Diagnostic Center", "Outpatient diagnostic center", "Eastgate", "Mandaluyong City",
            ["Laboratory", "X-ray", "CT scan", "MRI"],
            ["diagnostics"],
            "Monday to Friday, 8:00 AM to 5:00 PM", "Cashier and patient services counter"),
    ];

    // ---- Applications (all accounts; services filter to the household) ----

    public IReadOnlyList<AssistanceApplication> Applications()
    {
        var t = Today;
        return
        [
            new("MA-2026-00418", JoseId, "Jose Santos", "San Isidro General Hospital", "Hospital Bill Support",
                ApplicationStatus.NeedsCorrection, 3, 5, t.AddHours(-3).AddMinutes(-20),
                "Re-upload a clearer copy of the medical abstract",
                "hospital-bill", t.AddDays(-12).AddHours(9)),
            new("MA-2026-00431", MariaId, "Maria Santos", "Bayview Medical Center", "Medicine Cost Assistance",
                ApplicationStatus.UnderReview, 4, 4, t.AddDays(-1).AddHours(10),
                "Wait for the document review to finish",
                "medicine-cost", t.AddDays(-6).AddHours(8)),
            new("MA-2026-00437", JoseId, "Jose Santos", "San Isidro General Hospital", "Dialysis Session Support",
                ApplicationStatus.Draft, 1, 4, t.AddDays(-2).AddHours(16),
                "Finish step 4: Prepare documents",
                "dialysis", t.AddDays(-3).AddHours(10)),
            new("MA-2026-00395", LiaId, "Lia Santos", "Northridge Children's Hospital", "Diagnostic Procedure Aid",
                ApplicationStatus.Submitted, 2, 3, t.AddDays(-4).AddHours(9),
                "Patient authorization is being reviewed",
                "diagnostics", t.AddDays(-5).AddHours(13)),
            new("MA-2026-00362", MariaId, "Maria Santos", "Bayview Medical Center", "Diagnostic Procedure Aid",
                ApplicationStatus.ReadyForProcessing, 3, 3, t.AddDays(-9).AddHours(14),
                "No action needed from you",
                "diagnostics", t.AddDays(-20).AddHours(10)),
            new("MA-2026-00317", JoseId, "Jose Santos", "San Isidro General Hospital", "Medicine Cost Assistance",
                ApplicationStatus.Completed, 4, 4, t.AddDays(-21).AddHours(11),
                "Internal processing finished",
                "medicine-cost", t.AddDays(-40).AddHours(9)),
            new("MA-2026-00288", MariaId, "Maria Santos", "Bayview Medical Center", "Hospital Bill Support",
                ApplicationStatus.Cancelled, 0, 4, t.AddDays(-30).AddHours(10),
                "No action needed. This application was cancelled",
                "hospital-bill", t.AddDays(-33).AddHours(15)),
            new("MA-2026-00452", RamonId, "Ramon Villanueva", "San Isidro General Hospital", "Hospital Bill Support",
                ApplicationStatus.Submitted, 3, 4, t.AddDays(-2).AddHours(15),
                "Wait for the review to start",
                "hospital-bill", t.AddDays(-4).AddHours(11)),
        ];
    }

    // ---- History: what happened to each application, oldest first ----

    public IReadOnlyList<ApplicationEvent> Events()
    {
        var t = Today;
        const string You = "You";
        const string Reviewer = "Reviewer";
        const string MediAssist = "MediAssist";
        return
        [
            // MA-2026-00418: submitted with gaps, reviewed, sent back for a clearer abstract.
            new("MA-2026-00418", t.AddDays(-12).AddHours(9), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00418", t.AddDays(-10).AddHours(15), ApplicationEventKind.Submitted, "Application submitted", You,
                "Sent with 2 of 5 documents. The rest can follow.", ApplicationStatus.Submitted),
            new("MA-2026-00418", t.AddDays(-8).AddHours(11), ApplicationEventKind.DocumentUploaded, "Certificate of indigency added", You),
            new("MA-2026-00418", t.AddDays(-6).AddHours(10), ApplicationEventKind.ReviewStarted, "Review started", Reviewer,
                Status: ApplicationStatus.UnderReview),
            new("MA-2026-00418", t.AddHours(-3).AddMinutes(-20), ApplicationEventKind.CorrectionRequested, "Correction requested", Reviewer,
                "The attending physician's signature on page 2 of the clinical abstract cannot be read. Please upload a clearer copy.",
                ApplicationStatus.NeedsCorrection),

            // MA-2026-00431: complete on submission, now in review, with a runner errand.
            new("MA-2026-00431", t.AddDays(-6).AddHours(8), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00431", t.AddDays(-5).AddHours(14), ApplicationEventKind.Submitted, "Application submitted", You,
                "Sent with all 4 documents.", ApplicationStatus.Submitted),
            new("MA-2026-00431", t.AddDays(-2).AddHours(9), ApplicationEventKind.RunnerRequested, "Runner help requested", You,
                "Pick up the medicine voucher at Bayview Medical Center."),
            new("MA-2026-00431", t.AddDays(-1).AddHours(10), ApplicationEventKind.ReviewStarted, "Review started", Reviewer,
                Status: ApplicationStatus.UnderReview),

            // MA-2026-00437: a draft waiting for documents.
            new("MA-2026-00437", t.AddDays(-3).AddHours(10), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00437", t.AddDays(-2).AddHours(16), ApplicationEventKind.DocumentUploaded, "Valid ID added", You),

            // MA-2026-00395: submitted; waiting on the patient's authorization.
            new("MA-2026-00395", t.AddDays(-5).AddHours(13), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00395", t.AddDays(-4).AddHours(8).AddMinutes(45), ApplicationEventKind.RunnerRequested, "Runner help requested", You,
                "Submit the laboratory quotation at Northridge Children's Hospital."),
            new("MA-2026-00395", t.AddDays(-4).AddHours(9), ApplicationEventKind.Submitted, "Application submitted", You,
                "Review starts once Lia Santos's authorization is approved.", ApplicationStatus.Submitted),

            // MA-2026-00362: reviewed and passed to the program office.
            new("MA-2026-00362", t.AddDays(-20).AddHours(10), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00362", t.AddDays(-18).AddHours(9), ApplicationEventKind.Submitted, "Application submitted", You,
                "Sent with all 3 documents.", ApplicationStatus.Submitted),
            new("MA-2026-00362", t.AddDays(-15).AddHours(11), ApplicationEventKind.ReviewStarted, "Review started", Reviewer,
                Status: ApplicationStatus.UnderReview),
            new("MA-2026-00362", t.AddDays(-9).AddHours(14), ApplicationEventKind.ReadyForProcessing, "Review finished", Reviewer,
                "All documents were accepted. The program office will process the application.", ApplicationStatus.ReadyForProcessing),

            // MA-2026-00317: the whole journey.
            new("MA-2026-00317", t.AddDays(-40).AddHours(9), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00317", t.AddDays(-38).AddHours(10), ApplicationEventKind.Submitted, "Application submitted", You,
                "Sent with all 4 documents.", ApplicationStatus.Submitted),
            new("MA-2026-00317", t.AddDays(-35).AddHours(13), ApplicationEventKind.ReviewStarted, "Review started", Reviewer,
                Status: ApplicationStatus.UnderReview),
            new("MA-2026-00317", t.AddDays(-28).AddHours(15), ApplicationEventKind.ReadyForProcessing, "Review finished", Reviewer,
                "All documents were accepted. The program office will process the application.", ApplicationStatus.ReadyForProcessing),
            new("MA-2026-00317", t.AddDays(-21).AddHours(11), ApplicationEventKind.Completed, "Application completed", MediAssist,
                "The program office finished its internal processing.", ApplicationStatus.Completed),

            // MA-2026-00288: started by mistake and cancelled.
            new("MA-2026-00288", t.AddDays(-33).AddHours(15), ApplicationEventKind.Created, "Application started", You,
                Status: ApplicationStatus.Draft),
            new("MA-2026-00288", t.AddDays(-30).AddHours(10), ApplicationEventKind.Cancelled, "Application cancelled", You,
                "Started for the wrong hospital.", ApplicationStatus.Cancelled),

            // MA-2026-00452 (another account): submitted.
            new("MA-2026-00452", t.AddDays(-4).AddHours(11), ApplicationEventKind.Created, "Application started", "Applicant",
                Status: ApplicationStatus.Draft),
            new("MA-2026-00452", t.AddDays(-2).AddHours(15), ApplicationEventKind.Submitted, "Application submitted", "Applicant",
                Status: ApplicationStatus.Submitted),
        ];
    }

    // ---- Document checklists ----

    public IReadOnlyList<ApplicationChecklist> Checklists { get; } =
    [
        new("MA-2026-00418", "Hospital Bill Support", "Jose Santos",
        [
            new("Clinical abstract", DocumentStatus.NeedsCorrection, true, "Hospital medical records section",
                "The attending physician's signature on page 2 cannot be read."),
            new("Hospital bill or quotation", DocumentStatus.Missing, true, "Hospital billing office"),
            new("Certificate of indigency", DocumentStatus.Complete, true, "Your barangay hall"),
            new("Barangay certificate of residency", DocumentStatus.Complete, true, "Your barangay hall"),
            new("Valid ID", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
        new("MA-2026-00437", "Dialysis Session Support", "Jose Santos",
        [
            new("Treatment plan", DocumentStatus.Missing, true, "Your dialysis centre"),
            new("Clinical abstract", DocumentStatus.Missing, true, "Hospital medical records section"),
            new("Certificate of indigency", DocumentStatus.Missing, true, "Your barangay hall"),
            new("Valid ID", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
        new("MA-2026-00395", "Diagnostic Procedure Aid", "Lia Santos",
        [
            new("Doctor's request", DocumentStatus.Complete, true, "The requesting doctor"),
            new("Quotation", DocumentStatus.PendingReview, true, "Hospital laboratory or imaging cashier"),
            new("Valid ID of parent or guardian", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
        new("MA-2026-00431", "Medicine Cost Assistance", "Maria Santos",
        [
            new("Prescription", DocumentStatus.Complete, true, "Your attending doctor"),
            new("Clinical abstract", DocumentStatus.Complete, true, "Hospital medical records section"),
            new("Barangay certificate of residency", DocumentStatus.Complete, true, "Your barangay hall"),
            new("Valid ID", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
        new("MA-2026-00362", "Diagnostic Procedure Aid", "Maria Santos",
        [
            new("Doctor's request", DocumentStatus.Complete, true, "The requesting doctor"),
            new("Quotation", DocumentStatus.Complete, true, "Hospital laboratory or imaging cashier"),
            new("Valid ID", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
        new("MA-2026-00317", "Medicine Cost Assistance", "Jose Santos",
        [
            new("Prescription", DocumentStatus.Complete, true, "Your attending doctor"),
            new("Clinical abstract", DocumentStatus.Complete, true, "Hospital medical records section"),
            new("Barangay certificate of residency", DocumentStatus.Complete, true, "Your barangay hall"),
            new("Valid ID", DocumentStatus.Complete, true, "Any government-issued ID"),
        ]),
    ];

    // ---- Actions and notifications (the account holder's) ----

    public IReadOnlyList<ActionItem> ActionItems()
    {
        var t = Today;
        return
        [
            new("act-1", ActionPriority.High,
                "Medical abstract needs a clearer copy",
                "The reviewer could not read the attending physician's signature on page 2.",
                "MA-2026-00418", "Jose Santos",
                "Upload corrected document", "/patient/applications/MA-2026-00418",
                t.AddDays(3)),
            new("act-2", ActionPriority.Medium,
                "Draft application is waiting for documents",
                "3 of 4 required documents are still missing. You can submit now and add them later.",
                "MA-2026-00437", "Jose Santos",
                "Continue application", "/patient/applications/new?draft=MA-2026-00437"),
        ];
    }

    public IReadOnlyList<PortalNotification> Notifications()
    {
        var t = Today;
        return
        [
            new("n-1", "Correction requested", "MA-2026-00418 needs a clearer medical abstract.", t.AddHours(-3), false,
                "/patient/applications/MA-2026-00418"),
            new("n-2", "Review started", "MA-2026-00431 is now under review.", t.AddDays(-1).AddHours(10), false,
                "/patient/applications/MA-2026-00431"),
            new("n-3", "Authorization received", "Lia Santos's authorization request is being reviewed.", t.AddDays(-4), false,
                "/patient/patients/lia"),
            new("n-4", "Application completed", "Internal processing for MA-2026-00317 is finished.", t.AddDays(-21), true,
                "/patient/applications/MA-2026-00317"),
        ];
    }

    // ---- Runner errands ----

    public IReadOnlyList<RunnerRequestItem> RunnerRequests()
    {
        var t = Today;
        return
        [
            new("rr-31", "MA-2026-00452", "Deliver original hospital bill to the social service office",
                "San Isidro General Hospital", t.AddDays(1).AddHours(14), RunnerTaskStatus.PendingAssignment, null),
            new("rr-29", "MA-2026-00431", "Pick up approved medicine voucher",
                "Bayview Medical Center", t.AddHours(16), RunnerTaskStatus.Accepted, "Paolo Dizon"),
            new("rr-27", "MA-2026-00395", "Submit laboratory quotation",
                "Northridge Children's Hospital", t.AddDays(2).AddHours(10), RunnerTaskStatus.Declined, "Liza Manalo"),
        ];
    }

    public IReadOnlyList<RunnerTask> RunnerTasks()
    {
        var t = Today;
        return
        [
            new("rt-29", "MA-2026-00431", "Pick up approved medicine voucher",
                "Bayview Medical Center, social service office", "Bayview district", t.AddHours(16),
                RunnerTaskStatus.Accepted, "Maria"),
            new("rt-33", "MA-2026-00317", "Return signed acknowledgement form",
                "San Isidro General Hospital, billing office", "San Isidro", t.AddDays(1).AddHours(11),
                RunnerTaskStatus.Assigned, "Jose"),
            new("rt-24", "MA-2026-00362", "Deliver laboratory results to the program office",
                "Bayview Medical Center, laboratory", "Bayview district", t.AddDays(-1).AddHours(15),
                RunnerTaskStatus.Completed, "Maria"),
        ];
    }
}

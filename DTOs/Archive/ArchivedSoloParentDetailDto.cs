using System;
using System.Collections.Generic;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.DTOs.Archive
{
    public class ArchivedSoloParentDetailDto
    {
        public Guid Id { get; set; }
        public string ArchiveReason { get; set; } = string.Empty;
        public DateTime DateArchived { get; set; }
        public string ArchivedBy { get; set; } = string.Empty;
        public string FullName => $"{PersonalInfo?.LastName}, {PersonalInfo?.FirstName}".Trim(',', ' ');

        // Decrypted Demographic Details
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public EmploymentInfo Employment { get; set; } = new EmploymentInfo();
        public ContactInfo ContactDetails { get; set; } = new ContactInfo();
        public AddressInfo AddressDetails { get; set; } = new AddressInfo();
        public EmergencyContactInfo EmergencyContact { get; set; } = new EmergencyContactInfo();
        public ProblemPresentedDetails ProblemPresented { get; set; } = new ProblemPresentedDetails();

        public DateTime? ApplicationDate { get; set; }
        public DateTime? LastTermExpiresAt { get; set; }
        public DateTime OriginalDateCreated { get; set; }
        public string OriginalCreatedBy { get; set; } = string.Empty;

        // Historical Snapshots
        public List<FamilyMemberDto> FamilyMembers { get; set; } = new List<FamilyMemberDto>();
        public List<RecordTermDto> RecordTerms { get; set; } = new List<RecordTermDto>();

        // Special Demographics
        public bool IsLGBT { get; set; }
        public bool IsPantawidBeneficiary { get; set; }
        public bool IsIndigenous { get; set; }
        public string SubsidyEligibility { get; set; } = string.Empty;
        public string NeedsAndProblems { get; set; }
        public string OtherSourcesOfIncome { get; set; }

        public bool IsShredded { get; set; }
        public DateTime? DateShredded { get; set; }
        public bool IsReactivated { get; set; }
        public DateTime? DateReactivated { get; set; }
        public DateTime ScheduledShredDate => DateArchived.AddYears(10);
    }
}

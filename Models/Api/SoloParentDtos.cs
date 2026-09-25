using System;
using System.Collections.Generic;

namespace SOLUM_UI.Models.Api
{
    // ========================
    // Value Objects
    // ========================

    public class AddressInfo
    {
        public string Address { get; set; } = string.Empty;
        public string Barangay { get; set; } = string.Empty;
    }

    public class ContactInfo
    {
        public string ApplicantContactNumber { get; set; } = string.Empty;
    }

    public class EmergencyContactInfo
    {
        public string EmergencyPersonName { get; set; } = string.Empty;
        public string RelationshipToEmergencyPerson { get; set; } = string.Empty;
        public string EmergencyPersonContactAddress { get; set; } = string.Empty;
        public string EmergencyPersonContactNumber { get; set; } = string.Empty;
    }

    public class EmploymentInfo
    {
        public string Occupation { get; set; } = string.Empty;
        public decimal MonthlyIncome { get; set; }
        public string EmploymentStatus { get; set; } = string.Empty; // "employed", "self_employed", "not_employed"
    }

    public class PersonalInfo
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullNameHash { get; set; } = string.Empty;
        public string MiddleName { get; set; }
        public string ExtensionName { get; set; }
        public string CivilStatus { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public DateTime Birthdate { get; set; }
        public string Birthplace { get; set; } = string.Empty;
        public int Age { get; set; }
        public string EducationalAttainment { get; set; } = string.Empty;
        public string PhilsysCardNumber { get; set; }
        public string Religion { get; set; } = string.Empty;
        public bool IsPantawidBeneficiary { get; set; }
        public bool IsIndigenousPerson { get; set; }
        public bool IsLGBTQ { get; set; }

        public void SetFullNameHash(string hash)
        {
            FullNameHash = hash;
        }
    }

    public class ProblemPresentedDetails
    {
        public string ProblemPresentedNumber { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string CauseOfDeath { get; set; }
        public string date { get; set; }
        public string TypeOfDisability { get; set; }
        public string PeriodOfSeparation { get; set; }
        public decimal? LengthOfAbroad { get; set; }
        public bool? isNullity { get; set; }
        public bool? isAnnulmentOfMarraige { get; set; }
    }

    // ========================
    // Family Members
    // ========================

    public interface IFamilyMemberRequest
    {
        string Name { get; }
        string Sex { get; }
        int Age { get; }
        DateTime Birthdate { get; }
        string CivilStatus { get; }
        string Relationship { get; }
        string EducationalLevel { get; }
        decimal Income { get; }
    }

    public class CreateFamilyMemberRequest : IFamilyMemberRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Birthdate { get; set; }
        public string CivilStatus { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string EducationalLevel { get; set; } = string.Empty;
        public decimal Income { get; set; }
    }

    public class UpdateFamilyMemberRequest : IFamilyMemberRequest
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Birthdate { get; set; }
        public string CivilStatus { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string EducationalLevel { get; set; } = string.Empty;
        public decimal Income { get; set; }
    }

    public class FamilyMemberDto
    {
        public Guid Id { get; set; }
        public Guid SoloParentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public int Age { get; set; }
        public DateTime Birthdate { get; set; }
        public string CivilStatus { get; set; } = string.Empty;
        public string Relationship { get; set; } = string.Empty;
        public string EducationalLevel { get; set; } = string.Empty;
        public decimal Income { get; set; }
        public DateTime DateCreated { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class RecordTermDto
    {
        public Guid Id { get; set; }
        public Guid SoloParentId { get; set; }
        public string StartsAt { get; set; }
        public string ExpiresAt { get; set; }
    }

    // ========================
    // Solo Parent CRUD DTOs
    // ========================

    public interface ISoloParentRequest
    {
        PersonalInfo PersonalInfo { get; set; }
        EmploymentInfo Employment { get; set; }
        ContactInfo ContactDetails { get; set; }
        AddressInfo AddressDetails { get; set; }
        EmergencyContactInfo EmergencyContact { get; set; }
        ProblemPresentedDetails ProblemPresented { get; set; }
    }

    public class CreateSoloParentRequest : ISoloParentRequest
    {
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public EmploymentInfo Employment { get; set; } = new EmploymentInfo();
        public ContactInfo ContactDetails { get; set; } = new ContactInfo();
        public AddressInfo AddressDetails { get; set; } = new AddressInfo();
        public EmergencyContactInfo EmergencyContact { get; set; } = new EmergencyContactInfo();
        public ProblemPresentedDetails ProblemPresented { get; set; } = new ProblemPresentedDetails();
        public List<CreateFamilyMemberRequest> FamilyMembers { get; set; } = new List<CreateFamilyMemberRequest>();
    }

    public class UpdateSoloParentRequest : ISoloParentRequest
    {
        public Guid Id { get; set; }
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public EmploymentInfo Employment { get; set; } = new EmploymentInfo();
        public ContactInfo ContactDetails { get; set; } = new ContactInfo();
        public AddressInfo AddressDetails { get; set; } = new AddressInfo();
        public EmergencyContactInfo EmergencyContact { get; set; } = new EmergencyContactInfo();
        public ProblemPresentedDetails ProblemPresented { get; set; } = new ProblemPresentedDetails();
        public List<UpdateFamilyMemberRequest> ExistingFamilyMembers { get; set; } = new List<UpdateFamilyMemberRequest>();
        public List<CreateFamilyMemberRequest> NewFamilyMembers { get; set; } = new List<CreateFamilyMemberRequest>();
        public List<Guid> RemovedFamilyMemberIds { get; set; } = new List<Guid>();
    }

    public class GetSoloParentRequest
    {
        public Guid? Id { get; set; }
        public string Fullname { get; set; }
        public string Sex { get; set; }
        public string Barangay { get; set; }
        public bool? IsActive { get; set; }
        public string SortBy { get; set; } = "LastName";
        public string SortOrder { get; set; } = "asc";
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }

    public class SoloParentSummaryDto
    {
        public Guid Id { get; set; }
        public string Barangay { get; set; } = string.Empty;
        public string Sex { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class SoloParentListItemDto
    {
        public Guid Id { get; set; }
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public EmploymentInfo Employment { get; set; } = new EmploymentInfo();
        public ContactInfo ContactDetails { get; set; } = new ContactInfo();
        public AddressInfo AddressDetails { get; set; } = new AddressInfo();
        public EmergencyContactInfo EmergencyContact { get; set; } = new EmergencyContactInfo();
        public ProblemPresentedDetails ProblemPresented { get; set; } = new ProblemPresentedDetails();
        public DateTime DateCreated { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Matches the SoloParent aggregate entity returned by GET /api/soloparent/{id}
    /// </summary>
    public class SoloParentDto
    {
        public Guid Id { get; set; }
        public PersonalInfo PersonalInfo { get; set; } = new PersonalInfo();
        public EmploymentInfo Employment { get; set; } = new EmploymentInfo();
        public ContactInfo ContactDetails { get; set; } = new ContactInfo();
        public AddressInfo AddressDetails { get; set; } = new AddressInfo();
        public EmergencyContactInfo EmergencyContact { get; set; } = new EmergencyContactInfo();
        public ProblemPresentedDetails ProblemPresented { get; set; } = new ProblemPresentedDetails();
        public DateTime DateCreated { get; set; }
        public bool IsDeleted { get; set; }
        public List<FamilyMemberDto> FamilyMembers { get; set; } = new List<FamilyMemberDto>();
        public List<RecordTermDto> RecordTerms { get; set; } = new List<RecordTermDto>();
    }
}

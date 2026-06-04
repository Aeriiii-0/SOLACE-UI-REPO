using System;

namespace SOLUM_UI.Models
{
    public class SoloParentRecord
    {
        public string Id { get; set; }
        public string Name { get; set; }       
        public string Surname { get; set; }
        public string FirstName { get; set; }
        public string MiddleName { get; set; }
        public string ExtensionName { get; set; }      
        public DateTime DateOfBirth { get; set; }
        public string PlaceOfBirth { get; set; }
        public string Sex { get; set; }       
        public string CivilStatus { get; set; }      
        public string Citizenship { get; set; }
        public string BloodType { get; set; }
        public string Height { get; set; }
        public string Weight { get; set; }
        public string Address { get; set; }
        public string ContactNumber { get; set; }
        public string SourceOfReferral { get; set; }
        public DateTime DateAdmitted { get; set; }
        public string CaseNo { get; set; }
        public string OffenseCommitted { get; set; }
        public string NatureOfReferral { get; set; }      
        public string Barangay { get; set; }
        public int Children { get; set; }
        public string Status { get; set; }       
        public DateTime LastUpdated { get; set; }

        
        public string LastUpdatedFormatted => LastUpdated.ToString("yyyy-MM-dd");
        public string DateOfBirthFormatted => DateOfBirth.ToString("yyyy-MM-dd");
        public string DateAdmittedFormatted => DateAdmitted.ToString("yyyy-MM-dd");
        public string FullName => string.Format("{0}, {1} {2}", Surname, FirstName, MiddleName).Trim();
    }
}

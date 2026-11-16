using System.Collections.Generic;
using SOLUM_UI.Models.Api;

namespace SOLUM_UI.DTOs.Archive
{
    public class ReactivateArchivedRecordRequest
    {
        public EmploymentInfo UpdatedEmployment { get; set; }
        public ContactInfo UpdatedContactDetails { get; set; }
        public AddressInfo UpdatedAddressDetails { get; set; }
        public List<CreateFamilyMemberRequest> UpdatedFamilyMembers { get; set; }
        public string NeedsAndProblems { get; set; }
        public string OtherSourcesOfIncome { get; set; }
        public string Notes { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOLUM_UI.Models.Api
{
    public class MonthlyAnalyticsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string Barangay { get; set; } = "ALL";
        public int TotalSoloParents { get; set; }

        public Dictionary<string, int> Sex { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> CivilStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> EmploymentStatus { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> AgeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> MonthlyIncomeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> DependentsAgeBrackets { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> Categories { get; set; } = new Dictionary<string, int>();
    }
}

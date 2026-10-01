using System.Collections.Generic;

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

    // Sector breakdowns
    public Dictionary<string, int> Sectors { get; set; } = new Dictionary<string, int>();        
    public Dictionary<string, int> Lgbt { get; set; } = new Dictionary<string, int>();         
    public Dictionary<string, int> PantawidBeneficiary { get; set; } = new Dictionary<string, int>(); 
    public Dictionary<string, int> Indigenous { get; set; } = new Dictionary<string, int>();    
}
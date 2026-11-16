namespace SOLUM_UI.DTOs.Archive
{
    public class ArchiveSearchRequest
    {
        public string SearchTerm { get; set; }
        public string Barangay { get; set; }
        public string ArchiveReason { get; set; }
        public int? YearArchived { get; set; }
        public bool? IncludeShredded { get; set; } = false;

        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}

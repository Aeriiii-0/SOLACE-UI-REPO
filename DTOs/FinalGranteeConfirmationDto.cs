using System;
using System.Collections.Generic;

namespace SOLUM_UI.DTOs
{
    public class FinalGranteeConfirmationDto
    {
        public int FiscalYear { get; set; } = 2026;
        public List<string> ConfirmedSpIds { get; set; } = new List<string>();
        public decimal TotalDisbursedAmount { get; set; }
        public DateTime ConfirmationDate { get; set; } = DateTime.Now;
    }
}

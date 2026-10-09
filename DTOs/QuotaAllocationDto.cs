namespace SOLUM_UI.DTOs
{
    public class QuotaAllocationDto
    {
        public int BudgetAmount { get; set; } = 10000;
        public int PerGranteeAmount { get; set; } = 1000;
        public int AllocatedSlots => PerGranteeAmount > 0 ? BudgetAmount / PerGranteeAmount : 0;
    }
}

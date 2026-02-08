namespace capstone_backend.Business.DTOs.DatePlanItem
{
    public class ReorderDatePlanItemsRequest
    {
        public List<int> OrderedItemIds { get; set; } = new();
    }
}

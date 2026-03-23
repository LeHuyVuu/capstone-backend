namespace capstone_backend.Business.Jobs.VenueSettlement
{
    public interface IVenueSettlementWorker
    {
        Task ProcessPendingSettlementsAsync();
    }
}

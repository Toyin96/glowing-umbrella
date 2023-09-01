namespace LegalSearch.Domain.Enums.LegalRequest
{
    public enum CsoRequestStatusType
    {
        AllRequest = 1,
        Completed,
        PendingWithCso, 
        PendingWithSolicitor, //assignedToLawyer
        CancelledRequest
    }
}

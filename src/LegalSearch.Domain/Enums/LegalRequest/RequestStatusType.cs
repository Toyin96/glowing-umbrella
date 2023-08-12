namespace LegalSearch.Domain.Enums.LegalRequest
{
    public enum RequestStatusType
    {
        Initiated = 1,
        AssignedToLawyer,
        LawyerAccepted,
        LawyerRejected,
        BackToCso,
        UnAssigned,
        Completed
    }
}

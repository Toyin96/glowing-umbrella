namespace LegalSearch.Domain.ApplicationMessages
{
    public static class ConstantMessage
    {
        public const string NewRequestAssignmentMessage = "You have been assigned a new request. Please find attached the details of the request below";
        public const string UnAssignedRequestMessage = "The system is unable to route this request to a solicitor. Please find attached the details of the request below";
        public const string RequestPendingWithSolicitorMessage = "You are yet to revert any comments on this request in the last 24 hours. Please find attached the details of the request below and treat as urgent";
    }
}

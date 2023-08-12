namespace LegalSearch.Domain.ApplicationMessages
{
    public static class ConstantMessage
    {
        public const string NewRequestAssignmentMessage = "You have been assigned a new request. Please find attached the details of the request below";
        public const string UnAssignedRequestMessage = "The system is unable to route this request to a solicitor. Please find attached the details of the request below";
        public const string RequestPendingWithSolicitorMessage = "You are yet to revert any comments on this request in the last 24 hours. Please find attached the details of the request below and treat as urgent";
        public const string RequestRoutedBackToCSOMessage = "The solicitor assigned to this request needs additional information. Please find attached the details of the request below and treat as urgent";
    }
}

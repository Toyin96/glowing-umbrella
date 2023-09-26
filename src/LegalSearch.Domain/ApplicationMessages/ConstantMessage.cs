namespace LegalSearch.Domain.ApplicationMessages
{
    public static class ConstantMessage
    {
        public const string ReminderNotificationMessageOnPendingAssignedRequestForCso = "The solicitor assigned to this request just got a reminder nudging him/her to complete the request on time.";
        public const string NewRequestAssignmentMessage = "You have been assigned a new request. Please find attached the details of the request below";
        public const string AssignedRequestMessageForSolicitor = "You have been assigned a new request. Please find attached the details of the request below";
        public const string AssignedRequestMessageForCso = "Your request has been assigned to a solicitor. You will receive updates from them regarding the progress of your request.";
        public const string UnAssignedRequestMessage = "The system is unable to route this request to a solicitor. Please find attached the details of the request below";
        public const string RequestPendingWithSolicitorMessage = "You are yet to revert any comments on this request in the last 24 hours. Please find attached the details of the request below and treat as urgent";
        public const string RequestRoutedBackToCSOMessage = "The solicitor assigned to this request needs additional information. Please find attached the details of the request below and treat as urgent";
        public const string CompletedRequestMessage = "The solicitor assigned to this request has successfully completed the request. Please find attached the details of the request below";
    }

    public static class ConstantTitle
    {
        public const string NewRequestAssignmentTitle = "New Request";
        public const string NewRequestAssignmentTitleForSolicitor = "Assigned Request";
        public const string CompletedRequestTitleForCso = "Completed Request";
        public const string UnAssignedRequestTitleForCso = "UnAssigned Request";
        public const string ReminderNotificationTitleOnPendingAssignedRequestForSolicitor = "Reminder Notification on Pending Request";
        public const string ReminderNotificationTitleOnPendingAssignedRequestForCso = "Solicitor Just Got a Reminder on Pending Request";
        public const string AdditionalInformationNeededOnAssignedRequestTitle = "Request Needs Additional Information";
    }
}

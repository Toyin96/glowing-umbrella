namespace LegalSearch.Infrastructure.Utilities
{
    public static class NotificationTemplates
    {
        public static string GenerateNewRequestNotificationForSolicitor()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>New Request Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>You have a new request waiting for your attention. Please log in to the system to view and process the request.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string NotifySolicitorOnRequestAssignment()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Request Assignment Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has been assigned to a solicitor. You will receive updates from them regarding the progress of your request.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string OutstandingRequestNotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Outstanding Request Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has been pending for more than 24 hours. Please review and provide any necessary updates.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string RequestwithElapsedSLANotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Request with Elapsed SLA Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has exceeded the expected response time. Please check the status and take necessary actions.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string RequestReturnedNotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Request Returned Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has been returned to the Customer Service Officer for further review. Please stay tuned for updates.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }


        public static string ManualSolicitorAssignmentNotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Manual Solicitor Assignment Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has been manually assigned to a solicitor. They will handle your request from here.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string RequestCompletedNotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Request Completed Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request has been successfully completed. You can review the final details in the system.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string UnassignedRequestNotification()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Unassigned Request Notification</title>
            </head>
            <body>
                <p>Hello,</p>
                <p>Your request is currently unassigned. We will assign it to the appropriate team member shortly.</p>
                <p>Thank you,<br>Legal Search Team</p>
            </body>
            </html>

            ";
            return text;
        }
    }
}

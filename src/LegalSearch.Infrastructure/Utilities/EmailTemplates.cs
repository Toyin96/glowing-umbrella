namespace LegalSearch.Infrastructure.Utilities
{
    public static class EmailTemplates
    {
        public static string GetEmailTemplateForNewlyOnboardedUser()
        {
            var text = @"
            <html>
            <body>
              <p>Hello {{Username}},</p>
              <p>You've been onboarded by an admin to LegalSearch as a {{role}}. Please log in to your account using your AD credentials.</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetEmailTemplateForNewlyOnboardedSolicitor()
        {
            var text = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Update Your Default Password</title>
            </head>
            <body>
                <p>Dear {{username}},</p>

                <p>You're receiving this email because you have recently been onboarded on LegalSearch as a {{role}}. Before you can start using the application, we require you to change your default password.</p>

                <p>Please follow the steps below to update your password:</p>
                <ol>
                    <li><a href=""{{frontendBaseUrl}}/reset-password?token={{token}}&email={{email}}"">Click text</a> to proceed to the application.</li>
                    <li>Next, enter your new password to change your password.</li>
                </ol>

                <p>If you have any questions or need assistance, please feel free to contact our support team at <a href=""mailto:[Support Email]"">[Support Email]</a>.</p>

                <p>Thank you for using our application!</p>

                <p>Best regards,<br>
                The LegalSearch Team</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetEmailTemplateForAuthenticating2FaCode()
        {
            var text = @"
            <html>
            <body>
              <p>Hello {{Username}},</p>
              <p>To complete your login process, please enter this 2FA code: {{token}} to complete authentication.</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetEmailTemplateForUnlockingAccount()
        {
            var text = @"
            <html>
            <body>
              <p>Hi {{Username}},</p>
              <p>Your account has been temporarily locked for security purposes. Please use this 6-digits code: {{token}} to your account.</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetEmailTemplateForUnlockingAccountAwareness()
        {
            var text = @"
            <html>
            <body>
              <p>Hi {{Username}},</p>
              <p>Your account has been temporarily locked for security purposes. Please click on 'Unlock account' in the application to regain access to your account.</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetEmailTemplateForPasswordReset()
        {
            var text = @"
            <html>
            <body>
              <p>Hi {{Username}},</p>
              <p>It's time to enhance your security. Please log in and change your password with this token: {{token}}</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }

        public static string GetDailyReportEmailTemplateForZsm()
        {
            var text = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Daily Summary Report</title>
    <style>
        body {
            font-family: Arial, sans-serif;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }
        .header {
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 20px;
        }
        .content {
            font-size: 16px;
            margin-bottom: 20px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""content"">
            <p>Dear {{ZonalServiceManagerName}},</p>
            <p>Here is a recap of today, {{date}},</p>
            <ul>
                <li>Completed requests: {{CompletedRequestCount}}</li>
                <li>Total pending requests with Solicitors: {{RequestsPendingWithSolicitorCount}}</li>
                <li>Total pending requests with Customer Service Officers: {{RequestsPendingWithCsoCount}}</li>
                <li>Total pending requests that have exceeded SLA (3 days): {{RequestsWithElapsedSlaCount}}</li>
            </ul>
            <p>Best regards,<br>LegalSearch Team</p>
        </div>
    </div>
</body>
</html>

";
            return text;
        }

        public static string GetDailyReportEmailTemplateForCsm()
        {
            var text = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Daily Summary Report</title>
    <style>
        body {
            font-family: Arial, sans-serif;
        }
        .container {
            max-width: 600px;
            margin: 0 auto;
            padding: 20px;
            background-color: #f9f9f9;
        }
        .header {
            font-size: 24px;
            font-weight: bold;
            margin-bottom: 20px;
        }
        .content {
            font-size: 16px;
            margin-bottom: 20px;
        }
    </style>
</head>
<body>
    <div class=""container"">
        <div class=""content"">
            <p>Dear {{CustomerServiceManagerName}},</p>
            <p>Here is a recap of today, {{date}},</p>
            <ul>
                <li>Completed requests: {{CompletedRequestCount}}</li>
                <li>Total pending requests with Solicitors: {{RequestsPendingWithSolicitorCount}}</li>
                <li>Total pending requests with Customer Service Officers: {{RequestsPendingWithCsoCount}}</li>
                <li>Total pending requests that have exceeded SLA (3 days): {{RequestsWithElapsedSlaCount}}</li>
            </ul>
            <p>Best regards,<br>LegalSearch Team</p>
        </div>
    </div>
</body>
</html>

";
            return text;
        }

    }
}

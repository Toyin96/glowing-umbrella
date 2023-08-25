namespace LegalSearch.Infrastructure.Utilities
{
    public static class EmailTemplates
    {
        public static string GetEmailTemplateForNewlyOnboardedUser()
        {
            var text = @"
            <html>
            <body>
              <h1>Welcome to LegalSearch</h1>
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
                    <li>Login to the application using your default password: {{password}} and email: {{email}}.</li>
                    <li>Navigate to the ""Change Password"" section in your account settings.</li>
                    <li>Enter your current password and choose a new password of your choice.</li>
                    <li>Save the changes to update your password.</li>
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
              <h1>Complete Your Login with 2FA</h1>
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
              <h1>Unlock Your Account</h1>
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
              <h1>Unlock Your Account</h1>
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
              <h1>Change Your Password</h1>
              <p>Hi {{Username}},</p>
              <p>It's time to enhance your security. Please log in and change your password with this token: {{token}}</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
            </body>
            </html>

            ";
            return text;
        }
    }
}

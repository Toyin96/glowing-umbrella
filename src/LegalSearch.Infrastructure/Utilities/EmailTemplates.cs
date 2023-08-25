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
            <html>
            <body>
              <h1>Welcome to LegalSearch</h1>
              <p>Hello {{Username}},</p>
              <p>You've been onboarded by an admin to LegalSearch as a {{role}} with email: {{email}} and default password: {{password}}. Please log in and change your default password using your email and password generated.</p>
              <p>Best regards,</p>
              <p>LegalSearch</p>
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

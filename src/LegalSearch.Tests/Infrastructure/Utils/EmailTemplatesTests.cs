using LegalSearch.Infrastructure.Utilities;

namespace LegalSearch.Test.Infrastructure.Utils
{
    public class EmailTemplatesTests
    {
        [Fact]
        public void GetEmailTemplateForNewlyOnboardedUser_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForNewlyOnboardedUser();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetEmailTemplateForNewlyOnboardedSolicitor_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForNewlyOnboardedSolicitor();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetEmailTemplateForAuthenticating2FaCode_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForAuthenticating2FaCode();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetEmailTemplateForUnlockingAccount_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForUnlockingAccount();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetEmailTemplateForUnlockingAccountAwareness_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForUnlockingAccountAwareness();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetEmailTemplateForPasswordReset_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetEmailTemplateForPasswordReset();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetDailyReportEmailTemplateForZsm_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetDailyReportEmailTemplateForZsm();

            // Assert
            Assert.NotEmpty(template);
        }

        [Fact]
        public void GetDailyReportEmailTemplateForCsm_ReturnsTemplate()
        {
            // Act
            var template = EmailTemplates.GetDailyReportEmailTemplateForCsm();

            // Assert
            Assert.NotEmpty(template);
        }
    }
}

using LegalSearch.Domain.Entities.LegalRequest;
using LegalSearch.Domain.Enums.LegalRequest;
using LegalSearch.Infrastructure.Managers;
using LegalSearch.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace LegalSearch.Test.Managers
{
    public class LegalSearchRequestPaymentLogManagerTests
    {
        [Fact]
        public async Task AddLegalSearchRequestPaymentLog_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestPaymentLogManager(dbContext);

                var paymentLog = new LegalSearchRequestPaymentLog
                {
                    SourceAccountName = "SourceAccountNameValue",
                    SourceAccountNumber = "SourceAccountNumberValue",
                    DestinationAccountName = "DestinationAccountNameValue",
                    DestinationAccountNumber = "DestinationAccountNumberValue",
                    LienId = "LienIdValue",
                    CurrencyCode = "CurrencyCodeValue",
                    PaymentStatus = PaymentStatusType.MakePayment,
                    TransferRequestId = "TransferRequestIdValue",
                    TransferNarration = "TransferNarrationValue",
                    TranId = "TranIdValue",
                    TransactionStan = "TransactionStanValue",
                    TransferAmount = 100.0M,
                    PaymentResponseMetadata = "PaymentResponseMetadataValue",
                    LegalSearchRequestId = Guid.NewGuid()
                };

                // Act
                var result = await manager.AddLegalSearchRequestPaymentLog(paymentLog);

                // Assert
                Assert.True(result);
                var addedPaymentLog = dbContext.LegalSearchRequestPaymentLogs.Single();
                // Verify that the payment log was added to the database
                Assert.Equal(paymentLog.SourceAccountName, addedPaymentLog.SourceAccountName);
            }
        }


        [Fact]
        public async Task GetAllLegalSearchRequestPaymentLogNotYetCompleted_ReturnsFilteredPaymentLogs()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestPaymentLogManager(dbContext);

                // Create a list of sample payment logs
                var paymentLogs = new List<LegalSearchRequestPaymentLog>
                {
                    new LegalSearchRequestPaymentLog
                    {
                        SourceAccountName = "SourceAccountNameValue",
                        SourceAccountNumber = "SourceAccountNumberValue",
                        DestinationAccountName = "DestinationAccountNameValue",
                        DestinationAccountNumber = "DestinationAccountNumberValue",
                        LienId = "LienIdValue",
                        CurrencyCode = "CurrencyCodeValue",
                        PaymentStatus = PaymentStatusType.MakePayment,
                        TransferRequestId = "TransferRequestIdValue",
                        TransferNarration = "TransferNarrationValue",
                        TranId = "TranIdValue",
                        TransactionStan = "TransactionStanValue",
                        TransferAmount = 100.0M,
                        PaymentResponseMetadata = "PaymentResponseMetadataValue",
                        LegalSearchRequestId = Guid.NewGuid()
                    }
                };

                dbContext.LegalSearchRequestPaymentLogs.AddRange(paymentLogs);
                dbContext.SaveChanges();

                // Act
                var result = await manager.GetAllLegalSearchRequestPaymentLogNotYetCompleted();

                // Assert
                Assert.NotEmpty(result);
                Assert.All(result, log => Assert.NotEqual(PaymentStatusType.PaymentMade, log.PaymentStatus));
            }
        }



        [Fact]
        public async Task UpdateLegalSearchRequestPaymentLog_ReturnsTrueOnSuccess()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDatabase")
                .Options;

            using (var dbContext = new AppDbContext(options))
            {
                var manager = new LegalSearchRequestPaymentLogManager(dbContext);

                var paymentLog = new LegalSearchRequestPaymentLog
                {
                    SourceAccountName = "SourceAccountNameValue",
                    SourceAccountNumber = "SourceAccountNumberValue",
                    DestinationAccountName = "DestinationAccountNameValue",
                    DestinationAccountNumber = "DestinationAccountNumberValue",
                    LienId = "LienIdValue",
                    CurrencyCode = "CurrencyCodeValue",
                    PaymentStatus = PaymentStatusType.MakePayment,
                    TransferRequestId = "TransferRequestIdValue",
                    TransferNarration = "TransferNarrationValue",
                    TranId = "TranIdValue",
                    TransactionStan = "TransactionStanValue",
                    TransferAmount = 100.0M, // Set the desired decimal value
                    PaymentResponseMetadata = "PaymentResponseMetadataValue",
                    LegalSearchRequestId = Guid.NewGuid()
                };

                dbContext.LegalSearchRequestPaymentLogs.Add(paymentLog);
                dbContext.SaveChanges();

                // Act
                var result = await manager.UpdateLegalSearchRequestPaymentLog(paymentLog);

                // Assert
                Assert.True(result);
            }
        }

    }
}

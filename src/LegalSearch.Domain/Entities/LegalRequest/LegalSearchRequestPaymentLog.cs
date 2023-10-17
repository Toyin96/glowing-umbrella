using LegalSearch.Domain.Common;
using LegalSearch.Domain.Enums.LegalRequest;
using System.ComponentModel.DataAnnotations.Schema;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class LegalSearchRequestPaymentLog : BaseEntity
    {
        public required string SourceAccountName { get; set; }
        public required string SourceAccountNumber { get; set; }
        public required string DestinationAccountName { get; set; }
        public required string DestinationAccountNumber { get; set; }
        public required string LienId { get; set; }
        public required string CurrencyCode { get; set; }
        public PaymentStatusType PaymentStatus { get; set; }
        public string? TransferRequestId { get; set; }
        public string? TransferNarration { get; set; }
        public string? TranId { get; set; }
        public string? TransactionStan { get; set; }
        public decimal TransferAmount { get; set; }
        public string? PaymentResponseMetadata { get; set; }

        // configure relationship with legal search request table
        [ForeignKey("LegalRequest")]
        public Guid LegalSearchRequestId { get; set; }
    }
}

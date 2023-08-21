using LegalSearch.Domain.Entities.LegalRequest;

namespace LegalSearch.Application.Interfaces.LegalSearchRequest
{
    public interface ILegalSearchRequestPaymentLogManager
    {
        Task<bool> AddLegalSearchRequestPaymentLog(LegalSearchRequestPaymentLog legalSearchRequestPaymentLog);
        Task<List<LegalSearchRequestPaymentLog>> GetAllLegalSearchRequestPaymentLogPendingOnLienStage();
    }
}

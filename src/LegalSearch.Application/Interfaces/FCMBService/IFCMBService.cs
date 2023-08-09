using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.FCMBService
{
    public interface IFCMBService
    {
        Task<GetAccountInquiryResponse> GetAccountInquiry(string accountNumber);
        Task<AccountNameInquiryResponse> GetAccountNameInquiry(NameInquiryRequest nameInquiryRequest);
    }
}

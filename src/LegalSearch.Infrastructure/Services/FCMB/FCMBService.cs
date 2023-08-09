using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Infrastructure.Services.FCMB
{
    internal class FCMBService : IFCMBService
    {
        public Task<GetAccountInquiryResponse> GetAccountInquiry(string accountNumber)
        {
            throw new NotImplementedException();
        }

        public Task<AccountNameInquiryResponse> GetAccountNameInquiry(NameInquiryRequest nameInquiryRequest)
        {
            throw new NotImplementedException();
        }
    }
}

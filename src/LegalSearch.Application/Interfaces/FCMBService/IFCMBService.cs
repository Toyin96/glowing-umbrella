using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.FCMBService
{
    public interface IFcmbService
    {
        Task<GetAccountInquiryResponse?> MakeAccountInquiry(string accountNumber);
        Task<AddLienToAccountResponse?> AddLien(AddLienToAccountRequest addLienToAccountRequest);
        Task<RemoveLienFromAccountResponse?> RemoveLien(RemoveLienFromAccountRequest removeLienFromAccountRequest);
        Task<IntrabankTransferResponse?> InitiateTransfer(IntrabankTransferRequest intrabankTransferRequest);
    }
}

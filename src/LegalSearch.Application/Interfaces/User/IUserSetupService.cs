using System.Threading.Tasks;
using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;

namespace LegalSearch.Application.Interfaces.User
{
    public interface IUserSetupService
    {
        Task<ObjectResponse<SolicitorOnboardResponse>> OnboardSolicitorAsync(SolicitorOnboardRequest request);
    }
}

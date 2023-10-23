using LegalSearch.Application.Models.Requests.User;

namespace LegalSearch.Application.Interfaces.User
{
    public interface ICustomerServiceManager
    {
        Task<IEnumerable<CustomerServiceManagerMiniDto>> GetCustomerServiceManagers();
    }
}

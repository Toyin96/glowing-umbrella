using Fcmb.Shared.Models.Responses;
using LegalSearch.Application.Interfaces.User;
using LegalSearch.Application.Models.Constants;
using LegalSearch.Application.Models.Requests.User;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.User
{
    internal class CustomerManagerService : ICustomerManagerService
    {
        private readonly ICustomerServiceManager _customerServiceManager;
        private readonly ILogger<CustomerManagerService> _logger;

        public CustomerManagerService(ICustomerServiceManager customerServiceManager, ILogger<CustomerManagerService> logger)
        {
            _customerServiceManager = customerServiceManager;
            _logger = logger;
        }
        public async Task<ListResponse<CustomerServiceManagerMiniDto>> GetCustomerServiceManagers()
        {
            try
            {
                var customerServiceManagers = await _customerServiceManager.GetCustomerServiceManagers();

                return new ListResponse<CustomerServiceManagerMiniDto>("Operation was success", ResponseCodes.Success)
                {
                    Data = customerServiceManagers,
                    Total = customerServiceManagers.Count()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"An exception occurred inside GetCustomerServiceManagers. See reason: {JsonSerializer.Serialize(ex)}");
                throw;
            }
        }
    }
}

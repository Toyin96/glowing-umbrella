using Fcmb.Shared.Auth.Models.Requests;
using Fcmb.Shared.Auth.Models.Responses;
using Fcmb.Shared.Models.Responses;
using Fcmb.Shared.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace LegalSearch.Infrastructure.Services.User
{
    public class AuthService : Application.Interfaces.Auth.IAuthService
    {
        private readonly IHttpClientFactory httpClientFactory;

        private readonly ILogger<AuthService> logger;

        private readonly IConfiguration configuration;

        public AuthService(IHttpClientFactory httpClientFactory, ILogger<AuthService> logger, IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;
            this.configuration = configuration;
        }

        public async Task<ObjectResponse<AdLoginResponse>> LoginAsync(LoginRequest request)
        {
            HttpClient client = httpClientFactory.CreateClient("Auth");
            string clientId = configuration["FCMBConfig:AuthClientId"];
            string secretKey = configuration["FCMBConfig:AuthSecretKey"];
            StringContent body = new StringContent($"<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:tem=\"http://tempuri.org/\">\r\n                                                    <soapenv:Header/>\r\n                                                    <soapenv:Body>\r\n                                                        <tem:GetUserAdFullDetails>\r\n                                                            <tem:LoginName>{request.Email}</tem:LoginName>\r\n                                                            <tem:Password>{request.Password}</tem:Password>\r\n                                                            <tem:AppID>{clientId}</tem:AppID>\r\n                                                            <tem:AppKey>{secretKey}</tem:AppKey>\r\n                                                        </tem:GetUserAdFullDetails>\r\n                                                    </soapenv:Body>\r\n                                                </soapenv:Envelope>", Encoding.UTF8, "text/xml");
            try
            {
                HttpResponseMessage httpResponseMessage = await client.PostAsync("AuthenticationUtilityServiceSIT/AuthenticationService.asmx", body);
                string response = await httpResponseMessage.Content.ReadAsStringAsync();
                logger.LogInformation("AD Auth Response: {Response}", response);
                httpResponseMessage.EnsureSuccessStatusCode();
                (string, string, AdLoginResponse) loginResponse2 = GetLoginResponse(response, request.Email);
                string code = loginResponse2.Item1;
                string message = loginResponse2.Item2;
                AdLoginResponse loginResponse = loginResponse2.Item3;

                ObjectResponse<AdLoginResponse> obj = new(message, code)
                {
                    Data = loginResponse,
                };

                return obj;
            }
            catch (HttpRequestException ex)
            {
                HttpRequestException e = ex;
                logger.LogError(e, "Error Signing In {Username} To AD", request.Email);
                logger.LogError("Exception", e);
                return new ObjectResponse<AdLoginResponse>("Error Occurred", "999");
            }
        }

        private (string code, string message, AdLoginResponse? loginResponse) GetLoginResponse(string loginResult, string email)
        {
            try
            {
                XDocument xDocument = XDocument.Parse(loginResult);
                XNamespace xNamespace = (XNamespace?)"https://tempuri.org/";
                string? text = xDocument.Descendants(xNamespace + "Response")?.FirstOrDefault()?.Value;
                if (string.IsNullOrEmpty(text) || text != "00")
                {
                    logger.LogError("Failure response from Auth Service {Code}", text);
                    return ("999", "Failure Response From Auth Service", null);
                }

                string staffId = xDocument.Descendants(xNamespace + "StaffID")?.FirstOrDefault()?.Value ?? string.Empty;
                string staffName = xDocument.Descendants(xNamespace + "StaffName")?.FirstOrDefault()?.Value ?? string.Empty;
                string displayName = xDocument.Descendants(xNamespace + "DisplayName")?.FirstOrDefault()?.Value ?? string.Empty;
                string department = xDocument.Descendants(xNamespace + "Department")?.FirstOrDefault()?.Value ?? string.Empty;
                string groups = xDocument.Descendants(xNamespace + "Groups")?.FirstOrDefault()?.Value ?? string.Empty;
                string managerName = xDocument.Descendants(xNamespace + "ManagerName")?.FirstOrDefault()?.Value ?? string.Empty;
                string managerDepartment = xDocument.Descendants(xNamespace + "ManagerDepartment")?.FirstOrDefault()?.Value ?? string.Empty;
                AdLoginResponse adLoginResponse = new AdLoginResponse
                {
                    Department = department,
                    DisplayName = displayName,
                    Email = email,
                    Groups = groups,
                    ManagerDepartment = managerDepartment,
                    ManagerName = managerName,
                    StaffId = staffId,
                    StaffName = staffName
                };
                logger.LogInformation("Login response object {Response}", adLoginResponse.Serialize<AdLoginResponse>());
                return ("00", "Successfully Signed In To Auth Service", adLoginResponse);
            }
            catch (XmlException ex)
            {
                logger.LogError("Exception in parsing XML response", ex);
                return ("999", "Failure Response From Auth Service", null);
            }
        }
    }
}

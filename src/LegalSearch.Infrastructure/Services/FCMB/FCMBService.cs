using LegalSearch.Application.Interfaces.FCMBService;
using LegalSearch.Application.Models.Requests;
using LegalSearch.Application.Models.Responses;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Text.Json;

namespace LegalSearch.Infrastructure.Services.FCMB
{
    public class FCMBService : IFcmbService
    {
        private readonly HttpClient _client;
        private readonly FCMBServiceAppConfig _fCMBServiceAppConfig;
        private readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public FCMBService(HttpClient client, IOptions<FCMBServiceAppConfig> fCMBServiceAppConfig)
        {
            _client = client;
            _fCMBServiceAppConfig = fCMBServiceAppConfig.Value;
        }
        public async Task<GetAccountInquiryResponse?> MakeAccountInquiry(string accountNumber)
        {
            // clear all existing headers
            _client.DefaultRequestHeaders.Clear();

            // Set up the request headers
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _fCMBServiceAppConfig.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Client_ID", _fCMBServiceAppConfig.ClientId);
            _client.DefaultRequestHeaders.Add("x-token", GetToken().token);
            _client.DefaultRequestHeaders.Add("UTCTimestamp", GetToken().currentime);

            // Build the URL for the API call
            string baseUrl = _fCMBServiceAppConfig.BaseUrl;
            string endpoint = "/accountinquiry-clone/api/AccountInquiry/customerAccountInfoByAccountNo";
            string encodedAccountNumber = Uri.EscapeDataString(accountNumber);

            Uri actionUri = new Uri($"{baseUrl}/{endpoint}?accountNo={encodedAccountNumber}");

            // Send the GET request
            HttpResponseMessage httpResponse = await _client.GetAsync(actionUri);

            // Read the response content as a string
            string response = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a C# object
            return JsonSerializer.Deserialize<GetAccountInquiryResponse>(response, _jsonSerializerOptions);
        }

        public async Task<AddLienToAccountResponse?> AddLien(AddLienToAccountRequest addLienToAccountRequest)
        {
            // clear all existing headers
            _client.DefaultRequestHeaders.Clear();

            // Set up the request headers
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _fCMBServiceAppConfig.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Client_ID", _fCMBServiceAppConfig.ClientId);
            _client.DefaultRequestHeaders.Add("x-token", GetToken().token);
            _client.DefaultRequestHeaders.Add("UTCTimestamp", GetToken().currentime);

            var actionUrl = $"{_fCMBServiceAppConfig.BaseUrl}/lien/api/Accounts/v1/AddLien";

            // Send the GET request
            var httpResponse = await _client.PostAsync($"{actionUrl}", new StringContent(JObject.FromObject(addLienToAccountRequest).ToString(),
                Encoding.UTF8, "application/json"));

            // Read the response content as a string
            var response = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a C# object
            return JsonSerializer.Deserialize<AddLienToAccountResponse>(response, _jsonSerializerOptions);
        }

        public async Task<RemoveLienFromAccountResponse?> RemoveLien(RemoveLienFromAccountRequest removeLienFromAccountRequest)
        {
            // clear all existing headers
            _client.DefaultRequestHeaders.Clear();

            // Set up the request headers
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _fCMBServiceAppConfig.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Client_ID", _fCMBServiceAppConfig.ClientId);
            _client.DefaultRequestHeaders.Add("x-token", GetToken().token);
            _client.DefaultRequestHeaders.Add("UTCTimestamp", GetToken().currentime);

            var actionUrl = $"{_fCMBServiceAppConfig.BaseUrl}/lien/api/Accounts/v1/RemoveLien";

            // Send the GET request
            var httpResponse = await _client.PostAsync($"{actionUrl}", new StringContent(JObject.FromObject(removeLienFromAccountRequest).ToString(),
                Encoding.UTF8, "application/json"));

            // Read the response content as a string
            var response = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a C# object
            return JsonSerializer.Deserialize<RemoveLienFromAccountResponse>(response, _jsonSerializerOptions);
        }

        public async Task<IntrabankTransferResponse?> InitiateTransfer(IntrabankTransferRequest intrabankTransferRequest)
        {
            // clear all existing headers
            _client.DefaultRequestHeaders.Clear();

            // Set up the request headers
            _client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _fCMBServiceAppConfig.SubscriptionKey);
            _client.DefaultRequestHeaders.Add("Client_ID", _fCMBServiceAppConfig.ClientId);
            _client.DefaultRequestHeaders.Add("x-token", GetToken().token);
            _client.DefaultRequestHeaders.Add("UTCTimestamp", GetToken().currentime);

            var actionUrl = $"{_fCMBServiceAppConfig.BaseUrl}/cpmtransfer-api/api/cpm/doTransfer";

            // Send the GET request
            var httpResponse = await _client.PostAsync($"{actionUrl}", new StringContent(JObject.FromObject(intrabankTransferRequest).ToString(),
                Encoding.UTF8, "application/json"));

            // Read the response content as a string
            var response = await httpResponse.Content.ReadAsStringAsync();

            // Deserialize the JSON response into a C# object
            return JsonSerializer.Deserialize<IntrabankTransferResponse>(response, _jsonSerializerOptions);
        }

        private (string currentime, string token) GetToken()
        {
            var currentTime = DateTime.UtcNow;
            var _currentDate = currentTime.ToString("yyyy-MM-ddTHH:mm:ss.fff");

            var date = currentTime.ToString("yyyy-MM-ddHHmmss");
            var data = date + _fCMBServiceAppConfig.ClientId + _fCMBServiceAppConfig.Password;
            return (_currentDate, SHA512(data));
        }

        private string SHA512(string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            using (var hash = System.Security.Cryptography.SHA512.Create())
            {
                var hashedInputBytes = hash.ComputeHash(bytes);

                // Convert to text
                // StringBuilder Capacity is 128, because 512 bits / 8 bits in byte * 2 symbols for byte
                var hashedInputStringBuilder = new StringBuilder(128);
                foreach (var b in hashedInputBytes)
                    hashedInputStringBuilder.Append(b.ToString("x2"));
                return hashedInputStringBuilder.ToString();

            }
        }
    }
}

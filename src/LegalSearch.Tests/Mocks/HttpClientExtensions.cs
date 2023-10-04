using Moq;

namespace LegalSearch.Tests.Mocks
{
    public static class HttpClientExtensions
    {
        public static void SetupPostAsync(this Mock<HttpClient> httpClient, HttpResponseMessage response)
        {
            httpClient
                .Setup(client => client.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>()))
                .ReturnsAsync(response);
        }
    }
}

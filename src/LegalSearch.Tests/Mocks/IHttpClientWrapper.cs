namespace LegalSearch.Tests.Mocks
{
    public interface IHttpClientWrapper
    {
        Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content);
    }
}

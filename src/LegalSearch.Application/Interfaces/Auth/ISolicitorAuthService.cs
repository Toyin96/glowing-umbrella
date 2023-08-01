namespace LegalSearch.Application.Interfaces.Auth
{
    public interface ISolicitorAuthService<Solicitor> : IUserAuthService<Domain.Entities.User.User>
    {
        Task<bool> CreateUserAsync(string email, string password);
    }
}

using System;
using LegalSearch.Application.Models.Auth;

namespace LegalSearch.Application.Interfaces.Auth
{
    public interface IAuthTokenGenerator
    {
        string Generate(UserSession session, TimeSpan validity);
    }
}

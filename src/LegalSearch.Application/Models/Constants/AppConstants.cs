using System;

namespace LegalSearch.Application.Models.Constants
{
    public static class AppConstants
    {
        public static readonly string DbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") ?? "Host=localhost;Database=LegalSearchDb;Username=postgres;Password=postgres";
    }
}

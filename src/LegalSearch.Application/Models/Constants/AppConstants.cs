﻿namespace LegalSearch.Application.Models.Constants
{
    public static class AppConstants
    {
        public static readonly string DbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
            ?? "Data Source=WEMA-WDB-L9396;Initial Catalog=LegalSearch;Trusted_Connection=True;TrustServerCertificate=True;Integrated Security=True;Encrypt=False;";
    }
}

using System;

namespace LegalSearch.Application.Models.Constants
{
    public static class AppConstants
    {
        public static readonly string DbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING") 
            ?? "Data Source=WEMA-WDB-L9396;Initial Catalog=LegalSearch;Integrated Security=True";
    }
}

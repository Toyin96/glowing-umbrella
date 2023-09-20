namespace LegalSearch.Application.Models.Constants
{
    public static class AppConstants
    {
        public static readonly string DbConnectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING")
            ?? "Data source=52.247.216.167,1433; Initial Catalog=legal-search-be; integrated security=false;MultipleActiveResultSets=true;Trusted_Connection=false;User Id=sa;Password=microsoft_;";
    }

    public static class ReportConstants
    {
        public const string LegalSearchReport = "Legal Search Report";
    }
}

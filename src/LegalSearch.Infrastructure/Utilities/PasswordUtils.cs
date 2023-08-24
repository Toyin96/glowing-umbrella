namespace LegalSearch.Infrastructure.Utilities
{
    public static class PasswordUtils
    {
        public static string GenerateNumericToken()
        {
            Random random = new Random();
            int numericToken = random.Next(1000, 9999); // Generate a random number between 1000 and 9999
            return numericToken.ToString();
        }

    }
}

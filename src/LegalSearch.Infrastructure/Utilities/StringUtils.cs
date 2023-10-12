namespace LegalSearch.Infrastructure.Utilities
{
    public static class StringUtils
    {
        public static string First10Characters(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            return input.Length > 10 ? input.Substring(0, 10) : input;
        }
    }
}

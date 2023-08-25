namespace LegalSearch.Infrastructure.Utilities
{
    public static class EmailUtils
    {
        public static async Task<string> UpdatePlaceHolders(this string text, List<KeyValuePair<string, string>> keyValuePairs)
        {
            if (!string.IsNullOrWhiteSpace(text) && keyValuePairs != null)
            {
                foreach (KeyValuePair<string, string> item in keyValuePairs)
                {
                    if (text.Contains(item.Key))
                    {
                        text = text.Replace(item.Key, item.Value);
                    }
                }
            }
            return text;
        }
    }
}

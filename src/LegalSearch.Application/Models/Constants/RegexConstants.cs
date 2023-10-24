namespace LegalSearch.Application.Models.Constants
{
    public static class RegexConstants
    {
        public const string NameRegex = "^[a-zA-ZÀ-ÖØ-öø-ÿŁ-łŃ-ńĄ-ąÇ-çĆ-ćĈ-ĉĜ-ĝĤ-ĥĴ-ĵŜ-ŝŴ-ŵŶ-ŷ]+$|^([a-zA-Z]+[-'\\s]?[a-zA-Z]+)+$\r\n";
        public const string PhoneNumberRegex = "^(\\+?234)?[789][01]\\d{8}$";
        public const string EmailRegex = "\\A(?:[a-zA-Z0-9!#$%&'*+\\/=?^_`{|}~-]+(?:\\.[a-zA-Z0-9!#$%&'*+\\/=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?)\\Z";
        public const string PasswordRegex = "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^a-zA-Z\\d\\s:]).{8,}$";
    }
}

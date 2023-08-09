namespace LegalSearch.Application.Models.Responses
{
    public class StaffLoginResponse
    {
        public string Token { get; set; }
        public string DisplayName { get; set; }
        public string Branch { get; set; }
        public string Role { get; set; }
        public List<string> Permissions { get; set; }
        public DateTime? LastLoginDate { get; set; }
        public string SolId { get; set; }
    }
}

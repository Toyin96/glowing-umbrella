using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Solicitor : User
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public Firm Firm { get; set; }
        public Address Address { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string BankAccount { get; set; }
    }
}

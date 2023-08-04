using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Solicitor : User
    {
        public Firm Firm { get; set; }
        public Address Address { get; set; }
        [Required]
        public string BankAccount { get; set; }
    }
}

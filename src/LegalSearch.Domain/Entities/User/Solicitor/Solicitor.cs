using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Solicitor : User
    {
        [Required]
        public string BankAccount { get; set; }
    }
}

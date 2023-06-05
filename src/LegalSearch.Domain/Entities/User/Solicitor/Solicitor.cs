using System;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class Solicitor : User
    {
        public string Address { get; set; }
        public Guid FirmId { get; set; }
        public Firm Firm { get; set; }
        public Guid StateId { get; set; }
        public State State { get; set; }
        public Guid LgaId { get; set; }
        public Lga Lga { get; set; }
        public Guid BankAccountId { get; set; }
        public Guid BankAccount { get; set; }
    }
}

using System;
using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class BankAccount : BaseEntity
    {
        public Guid BankId { get; set; }
        public Guid Bank { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; }
    }
    
    public class Bank : BaseEntity
    {
        public string Name { get; set; }
        public string Code { get; set; }
    }
}

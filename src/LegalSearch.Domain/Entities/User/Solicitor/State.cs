using System;
using System.Collections.Generic;
using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.User.Solicitor
{
    public class State : BaseEntity
    {
        public string Name { get; set; }
        public ICollection<Lga> Lgas { get; set; }
    }
    
    public class Lga : BaseEntity
    {
        public string Name { get; set; }
        public Guid StateId { get; set; }
        public State State { get; set; }
    }
}

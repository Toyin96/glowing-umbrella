﻿using LegalSearch.Domain.Common;

namespace LegalSearch.Domain.Entities.LegalRequest
{
    public class LegalRequest : BaseEntity
    {
        public string StaffId { get; set; }
        public User.User? Solicitor { get; set; }
        public string RequestType { get; set; }
        public Guid BusinessLocation { get; set; }
        public Guid RegistrationLocation { get; set; }
        public string Status { get; set; }
        public string CustomerAccount { get; set; }
        public string RegistrationNumber { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string? AdditionalInformation { get; set; }
        public ICollection<SupportingDocument> SupportingDocuments { get; set; } = new List<SupportingDocument>();
    }
}

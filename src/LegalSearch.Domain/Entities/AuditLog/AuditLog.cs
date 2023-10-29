using LegalSearch.Domain.Common;
using LegalSearch.Domain.Enums.AuditLog;
using System.Diagnostics.CodeAnalysis;

namespace LegalSearch.Domain.Entities.AuditLog
{
    [ExcludeFromCodeCoverage]
    public class AuditLog : BaseEntity
    {
        public Guid? ActorId { get; set; }

        public Guid? EntityId { get; set; }

        public string? EntityTable { get; set; }

        public AuditAction AuditAction { get; set; }

        public string? Log { get; set; }

        public bool IsSuccessful { get; set; } = true;
    }
}

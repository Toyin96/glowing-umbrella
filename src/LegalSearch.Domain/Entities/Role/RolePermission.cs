using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Domain.Entities.Role
{
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }
        public string Permission { get; set; }
    }
}

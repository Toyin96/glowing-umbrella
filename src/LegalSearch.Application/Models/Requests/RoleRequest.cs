using Fcmb.Shared.Models.Constants;
using Fcmb.Shared.Models.Requests;
using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests
{
    public record RoleRequest
    {
        [Required(ErrorMessage = "Please Provide Role Name"), RegularExpression(RegexConstants.TextRegex, ErrorMessage = "Please Provide A Valid Role Name.")]
        [StringLength(100, ErrorMessage = "Role Name Cannot Be More Than Length Of 100")]
        public string RoleName { get; init; }

        public List<string> Permissions { get; init; } = new();
    }

    public record FilterRoleRequest : PaginatedRequest
    {
        public string RoleName { get; init; }

        public List<string> Permissions { get; init; } = new();
    }
}

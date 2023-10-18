using LegalSearch.Application.Models.Constants;
using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests
{
    public abstract record BaseUserRequest
    {
        [Required(ErrorMessage = "Please Provide First Name"), RegularExpression(RegexConstants.NameRegex, ErrorMessage = "Please Provide A Valid First Name.")]
        [StringLength(100, ErrorMessage = "First Name Cannot Be More Than Length Of 100")]
        public required string FirstName { get; init; }

        [Required(ErrorMessage = "Please Provide Last Name"), RegularExpression(RegexConstants.NameRegex, ErrorMessage = "Please Provide A Valid Last Name.")]
        [StringLength(100, ErrorMessage = "Last Name Cannot Be More Than Length Of 100")]
        public string? LastName { get; init; }

        [Required(ErrorMessage = "Please Provide Phone Number"), RegularExpression(RegexConstants.PhoneNumberRegex, ErrorMessage = "Please Provide A Valid Phone Number.")]
        [StringLength(14, ErrorMessage = "Phone Number Cannot Be More Than Length Of 14")]
        public string? PhoneNumber { get; init; }

        [Required(ErrorMessage = "Please Provide Email"), RegularExpression(RegexConstants.EmailRegex, ErrorMessage = "Please Provide A Valid Email Address.")]
        [StringLength(100, ErrorMessage = "Email Cannot Be More Than Length Of 100")]
        public required string Email { get; init; }
    }

    public record SolicitorOnboardRequest : BaseUserRequest
    {
        [Required]
        public required FirmRequest Firm { get; set; }
        [Required]
        public required string BankAccount { get; set; }
    }

    public class FirmRequest
    {
        public required string Name { get; set; }
        public required string Address { get; set; }
        public Guid StateId { get; set; }
        public Guid StateOfCoverageId { get; set; }
    }
}

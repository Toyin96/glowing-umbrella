using System.ComponentModel.DataAnnotations;
using Fcmb.Shared.Models.Constants;

namespace LegalSearch.Application.Models.Requests
{
    public abstract record BaseUserRequest
    {
        [Required(ErrorMessage = "Please Provide First Name"), RegularExpression(RegexConstants.FullNameRegex, ErrorMessage = "Please Provide A Valid First Name.")]
        [StringLength(100, ErrorMessage = "First Name Cannot Be More Than Length Of 100")]
        public string FirstName { get; init; }
        
        [Required(ErrorMessage = "Please Provide Last Name"), RegularExpression(RegexConstants.FullNameRegex, ErrorMessage = "Please Provide A Valid Last Name.")]
        [StringLength(100, ErrorMessage = "Last Name Cannot Be More Than Length Of 100")]
        public string LastName { get; init; }
        
        [Required(ErrorMessage = "Please Provide Phone Number"), RegularExpression(RegexConstants.PhoneNumberRegex, ErrorMessage = "Please Provide A Valid Phone Number.")]
        [StringLength(14, ErrorMessage = "Phone Number Cannot Be More Than Length Of 14")]
        public string PhoneNumber { get; init; }
        
        [Required(ErrorMessage = "Please Provide Email"), RegularExpression(RegexConstants.EmailRegex, ErrorMessage = "Please Provide A Valid Email Address.")]
        [StringLength(100, ErrorMessage = "Email Cannot Be More Than Length Of 100")]
        public string Email { get; init; }
    }
    
    public record SolicitorOnboardRequest : BaseUserRequest
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public FirmRequest Firm { get; set; }
        public Guid StateId { get; set; }
        public AddressRequest Address { get; set; }
        [Required]
        public string PhoneNumber { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        public string BankAccount { get; set; }
    }

    public class FirmRequest
    {
        public string Name { get; set; }
        public AddressRequest Address { get; set; }
    }

    public class AddressRequest
    {
        public string Street { get; set; }
        public Guid StateId { get; set; }
    }
}

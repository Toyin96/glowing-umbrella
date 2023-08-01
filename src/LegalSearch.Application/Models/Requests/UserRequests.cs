using System;
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
        [Required(ErrorMessage = "Please Provide Firm")]
        public Guid FirmId { get; init; }
        
        [Required(ErrorMessage = "Please Provide State")]
        public Guid StateId { get; init; }
        
        [Required(ErrorMessage = "Please Provide LGA")]
        public Guid RegionId { get; init; }
        
        [Required(ErrorMessage = "Please Provide Bank Account")]
        public Guid BankAccountId { get; init; }
        
        [Required(ErrorMessage = "Please Provide Address"), RegularExpression(RegexConstants.TextRegex, ErrorMessage = "Please Provide A Valid Address.")]
        [StringLength(100, ErrorMessage = "Address Cannot Be More Than Length Of 100")]
        public string Address { get; init; }
    }
}

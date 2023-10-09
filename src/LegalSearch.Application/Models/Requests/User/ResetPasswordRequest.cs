using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests.User
{
    public class ResetPasswordRequest
    {
        [Required]
        [DataType(DataType.Password)]
        public required string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public required string ConfirmNewPassword { get; set; }
        public required string Email { get; set; }
        public required string Token { get; set; }
    }
}

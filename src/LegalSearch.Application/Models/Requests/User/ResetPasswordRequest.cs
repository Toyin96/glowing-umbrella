using System.ComponentModel.DataAnnotations;

namespace LegalSearch.Application.Models.Requests.User
{
    public class ResetPasswordRequest
    {
        [Required]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmNewPassword { get; set; }
        public string Email { get; set; }
        public string Token { get; set; }
    }
}

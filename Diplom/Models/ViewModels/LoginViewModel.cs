using System.ComponentModel.DataAnnotations;

namespace Diplom.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Email or username")]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }
}

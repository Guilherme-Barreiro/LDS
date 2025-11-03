using System.ComponentModel.DataAnnotations;

namespace ConsultaPlus.API.DTOs
{
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs
{
    public class CreateUserDto
    {
        [Required]
        [MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        [MaxLength(100)]
        public string Password { get; set; } = string.Empty;

        [StringLength(50)]
        public string Role { get; set; } = "Viewer";
    }
}

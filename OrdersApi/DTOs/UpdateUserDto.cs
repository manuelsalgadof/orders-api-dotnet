using System.ComponentModel.DataAnnotations;

namespace OrdersApi.DTOs
{
    public class UpdateUserDto
    {
        [MaxLength(150)]
        public string? Name { get; set; }

        [EmailAddress]
        [MaxLength(256)]
        public string? Email { get; set; }

        public string? Status { get; set; }
    }
}

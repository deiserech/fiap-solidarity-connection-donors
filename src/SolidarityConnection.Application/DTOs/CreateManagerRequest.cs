using System.ComponentModel.DataAnnotations;

namespace SolidarityConnection.Application.DTOs
{
    public class CreateManagerRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public required string Name { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email must have at most 255 characters.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [StringLength(128, MinimumLength = 8, ErrorMessage = "Password must be between 8 and 128 characters.")]
        public required string Password { get; set; }
    }
}


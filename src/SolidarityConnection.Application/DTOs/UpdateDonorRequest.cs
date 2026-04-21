using System.ComponentModel.DataAnnotations;

namespace SolidarityConnection.Application.DTOs
{
    public class UpdateDonorRequest
    {
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 200 characters.")]
        public string? Name { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format.")]
        [StringLength(255, ErrorMessage = "Email must have at most 255 characters.")]
        public string? Email { get; set; }
    }
}


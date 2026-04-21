namespace SolidarityConnection.Application.DTOs
{
    using System.ComponentModel.DataAnnotations;

    public class LoginDto
    {
        [Required(ErrorMessage = "O e-mail Ã© obrigatÃ³rio.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [StringLength(255, ErrorMessage = "O e-mail deve ter no máximo 255 caracteres.")]
        public required string Email { get; set; }

        [Required(ErrorMessage = "A senha Ã© obrigatÃ³ria.")]
        [StringLength(128, MinimumLength = 1, ErrorMessage = "A senha Ã© obrigatÃ³ria.")]
        public required string Password { get; set; }
    }
}


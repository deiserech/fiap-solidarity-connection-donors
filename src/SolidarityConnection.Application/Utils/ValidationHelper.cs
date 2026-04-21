using System.Text.RegularExpressions;

namespace SolidarityConnection.Application.Utils
{
    public static class ValidationHelper
    {
        private static readonly Regex EmailRegex = new Regex(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex CpfRegex = new Regex(
            @"^\d{11}$",
            RegexOptions.Compiled);

        public static List<string> ValidateRegisterEntries(string password, string email)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(email))
            {
                errors.Add("O email é obrigatório.");
            }
            else if (!IsValidEmail(email))
            {
                errors.Add("O email fornecido não é válido.");
            }
            var passwordErrors = ValidatePassword(password);
            if (passwordErrors.Count > 0)
            {
                errors.AddRange(passwordErrors);
            }
            return errors;
        }

        public static List<string> ValidateRegisterEntries(string password, string email, string cpf)
        {
            var errors = ValidateRegisterEntries(password, email);

            if (string.IsNullOrWhiteSpace(cpf))
            {
                errors.Add("O CPF Ã© obrigatÃ³rio.");
            }
            else if (!CpfRegex.IsMatch(cpf))
            {
                errors.Add("O CPF deve conter 11 dÃ­gitos numÃ©ricos.");
            }

            return errors;
        }

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            return EmailRegex.IsMatch(email);
        }

        public static List<string> ValidatePassword(string password)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(password))
            {
                errors.Add("A senha Ã© obrigatÃ³ria.");
                return errors;
            }

            if (password.Length < 8)
                errors.Add("A senha deve ter no mÃ­nimo 8 caracteres.");

            if (!password.Any(char.IsLower))
                errors.Add("A senha deve conter pelo menos uma letra minÃºscula.");

            if (!password.Any(char.IsUpper))
                errors.Add("A senha deve conter pelo menos uma letra maiÃºscula.");

            if (!password.Any(char.IsDigit))
                errors.Add("A senha deve conter pelo menos um nÃºmero.");

            if (!password.Any(c => "@$!%*?&#+\\-_.=".Contains(c)))
                errors.Add("A senha deve conter pelo menos um caractere especial (@$!%*?&#+\\-_.=).");

            return errors;
        }
    }
}

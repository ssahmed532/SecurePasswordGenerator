namespace SecurePasswordGenerator.Models
{
    public class PasswordPolicy
    {
        public int MinimumLength { get; set; } = 12;
        public int MaximumLength { get; set; } = 16;
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNumber { get; set; } = true;
        public bool RequireSpecialCharacter { get; set; } = true;
        public bool AvoidCommonPatterns { get; set; } = true;
    }
}

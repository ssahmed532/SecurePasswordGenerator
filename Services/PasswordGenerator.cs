using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using SecurePasswordGenerator.Models;
using SecurePasswordGenerator.Utilities;

namespace SecurePasswordGenerator.Services
{
    public class PasswordGenerator
    {
        private readonly PasswordPolicy _policy;

        public PasswordGenerator(PasswordPolicy policy)
        {
            _policy = policy;
        }

        // Modify this method so that it supports the following rules:
        // 1) The password must start with an uppercase or lowercase character
        // 2) The generated password must not include the same character consecutively
        // 3) Special characters cannot occur consecutively
        // 4) The same numeric character cannot occur more than once in the same password
        // 5) A special character cannot occur more than once in the same password
        public string GeneratePassword()
        {
            if (_policy.MinimumLength < 12)
                throw new ArgumentException("Minimum password length must be at least 12 characters for PCI-DSS compliance.");

            var passwordBuilder = new StringBuilder();
            passwordBuilder.Append(RandomCharacterGenerator.GetRandomUppercase());
            passwordBuilder.Append(RandomCharacterGenerator.GetRandomLowercase());
            passwordBuilder.Append(RandomCharacterGenerator.GetRandomDigit());
            passwordBuilder.Append(RandomCharacterGenerator.GetRandomSpecialCharacter());

            while (passwordBuilder.Length < _policy.MinimumLength)
            {
                char nextChar = GetRandomCharacter();
                passwordBuilder.Append(nextChar);
            }

            return new string(passwordBuilder.ToString().ToCharArray().OrderBy(_ => RandomNumberGenerator.GetInt32(0, 100)).ToArray());
        }

        private char GetRandomCharacter()
        {
            int choice = RandomNumberGenerator.GetInt32(0, 4);
            return choice switch
            {
                0 => RandomCharacterGenerator.GetRandomUppercase(),
                1 => RandomCharacterGenerator.GetRandomLowercase(),
                2 => RandomCharacterGenerator.GetRandomDigit(),
                _ => RandomCharacterGenerator.GetRandomSpecialCharacter()
            };
        }
    }
}

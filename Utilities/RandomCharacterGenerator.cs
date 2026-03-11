using System;
using System.Security.Cryptography;


namespace SecurePasswordGenerator.Utilities
{
    public static class RandomCharacterGenerator
    {
        private static readonly char[] SpecialCharacters = "!@#$%^&*()-_=+".ToCharArray();

        public static char GetRandomUppercase() =>
            (char)RandomNumberGenerator.GetInt32(65, 91); // A-Z

        public static char GetRandomLowercase() =>
            (char)RandomNumberGenerator.GetInt32(97, 123); // a-z

        public static char GetRandomDigit() =>
            (char)RandomNumberGenerator.GetInt32(48, 58); // 0-9

        public static char GetRandomSpecialCharacter() =>
            SpecialCharacters[RandomNumberGenerator.GetInt32(0, SpecialCharacters.Length)];
    }
}

using System;
using SecurePasswordGenerator.Models;
using SecurePasswordGenerator.Services;

class Program
{
    static void Main()
    {
        var policy = new PasswordPolicy();
        var generator = new PasswordGenerator(policy);
        
        Console.WriteLine("Generated Secure Password:");
        Console.WriteLine(generator.GeneratePassword());
    }
}

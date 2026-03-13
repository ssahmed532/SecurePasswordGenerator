using System;
using System.Reflection;
using SecurePasswordGenerator.Models;
using SecurePasswordGenerator.Services;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v"))
        {
            Console.WriteLine($"SecurePasswordGenerator v{GetVersion()}");
            return;
        }

        if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h"))
        {
            Console.WriteLine($"SecurePasswordGenerator v{GetVersion()}");
            Console.WriteLine();
            Console.WriteLine("Usage: SecurePasswordGenerator [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  -h, --help       Show this help message");
            Console.WriteLine("  -v, --version    Show the version number");
            Console.WriteLine();
            Console.WriteLine("When run without options, generates a secure password.");
            return;
        }

        var policy = new PasswordPolicy();
        var generator = new PasswordGenerator(policy);

        Console.WriteLine("Generated Secure Password:");
        Console.WriteLine(generator.GeneratePassword());
    }

    static string GetVersion()
    {
        var version = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "unknown";

        // Strip the source revision hash suffix (e.g. "+abc123def")
        var plusIndex = version.IndexOf('+');
        return plusIndex >= 0 ? version[..plusIndex] : version;
    }
}

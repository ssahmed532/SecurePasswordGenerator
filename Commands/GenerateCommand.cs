using Spectre.Console;
using Spectre.Console.Cli;
using SecurePasswordGenerator.Models;
using SecurePasswordGenerator.Services;

namespace SecurePasswordGenerator.Commands;

public class GenerateCommand : Command<GenerateCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellation)
    {
        var policy = new PasswordPolicy();
        var generator = new PasswordGenerator(policy);
        var password = generator.GeneratePassword();

        AnsiConsole.MarkupLine("[bold green]Generated Secure Password:[/]");
        AnsiConsole.WriteLine(password);

        return 0;
    }
}

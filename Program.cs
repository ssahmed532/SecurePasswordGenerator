using Spectre.Console.Cli;
using SecurePasswordGenerator.Commands;

var app = new CommandApp<GenerateCommand>();

app.Configure(config =>
{
    config.SetApplicationName("SecurePasswordGenerator");
    config.SetApplicationVersion("0.2.0");
});

return app.Run(args);

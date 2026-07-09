using Pillaro.Dataverse.PluginFramework.Cli.PluginCommands;

var exitCode = await PluginCommandRouter.RunAsync(args);
return exitCode;

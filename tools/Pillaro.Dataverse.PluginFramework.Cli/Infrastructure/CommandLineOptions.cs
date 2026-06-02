namespace Pillaro.Dataverse.PluginFramework.Cli.Infrastructure;

internal sealed class CommandLineOptions
{
    private readonly Dictionary<string, string?> _values = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _flags = new(StringComparer.OrdinalIgnoreCase);

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (string.Equals(arg, "-h", StringComparison.Ordinal))
            {
                options._flags.Add("help");
                options._values["help"] = null;
                continue;
            }

            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{arg}'. Options must start with --.");
            }

            var name = arg[2..];
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Option name is required.");
            }

            if (i + 1 >= args.Length || args[i + 1].StartsWith("-", StringComparison.Ordinal))
            {
                options._flags.Add(name);
                options._values[name] = null;
                continue;
            }

            options._values[name] = args[++i];
        }

        return options;
    }

    public bool HasFlag(string name) => _flags.Contains(name);

    public IEnumerable<string> Names => _values.Keys;

    public string? Get(string name)
    {
        return _values.TryGetValue(name, out var value) ? value : null;
    }

    public string Require(string name)
    {
        var value = Get(name);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"Missing required option --{name}.");
        }

        return value;
    }
}

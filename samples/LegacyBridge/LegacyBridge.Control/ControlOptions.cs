namespace LegacyBridge.Control;

/// <summary>CLI options for the sample: data directory and reset flag.</summary>
public sealed record ControlOptions(string DataDirectory, bool Reset)
{
    public string LegacyDbPath => Path.Combine(DataDirectory, "legacy.db");

    public string WarehouseDbPath => Path.Combine(DataDirectory, "warehouse.db");

    public static ControlOptions Parse(string[] args)
    {
        var data = Path.Combine(AppContext.BaseDirectory, "data");
        var reset = false;

        for (var index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--reset":
                    reset = true;
                    break;
                case "--data" when index + 1 < args.Length:
                    data = args[++index];
                    break;
            }
        }

        return new ControlOptions(Path.GetFullPath(data), reset);
    }
}

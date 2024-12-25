using NoitaMap.Logging;

namespace NoitaMap;

public static class PathService
{
    private static string? _savePath;
    private static string? _worldPath;
    private static string? _dataPath;

    /// <summary>
    /// Returns the directory that the application is in. 
    /// </summary>
    public static string ApplicationPath { get; }

    private static string SavePath => _savePath ?? string.Empty;

    public static string WorldPath => _worldPath ?? string.Empty;

    public static string DataPath => _dataPath ?? string.Empty;

    static PathService()
    {
        ApplicationPath = AppContext.BaseDirectory;
    }

    public static void SetPaths(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];

            if (arg == "-s" || arg == "--save")
            {
                if (i + 1 >= args.Length)
                {
                    Logger.LogCritical("Invalid Command Line Argument");

                    break;
                }

                i++;
                _savePath = args[i];
            }
            else if (arg == "-w" || arg == "--world")
            {
                if (i + 1 >= args.Length)
                {
                    Logger.LogCritical("Invalid Command Line Argument");

                    break;
                }

                i++;
                _worldPath = args[i];
            }
            else if (arg == "-d" || arg == "--data")
            {
                if (i + 1 >= args.Length)
                {
                    Logger.LogCritical("Invalid Command Line Argument");

                    break;
                }

                i++;
                _dataPath = args[i];
            }
            else if (arg == "-h" || arg == "-?" || arg == "--help")
            {
                PrintUsage();
                Environment.Exit(0);
            }
            else
            {
                Logger.LogCritical($"Unknown argument: {arg}");
                PrintUsage();
                Environment.Exit(-1);
            }
        }

        if (OperatingSystem.IsWindows())
        {
            string localLowPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low";

            _savePath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "save00");

            _worldPath ??= Path.Combine(_savePath, "world");

            _dataPath ??= Path.Combine(localLowPath, "Nolla_Games_Noita", "data");
        }

        if (_savePath is null)
        {
            Logger.LogCritical("Please specify a path for your save: --save \"/path/to/your/save\"");
            throw new Exception("No Save Path Specified");
        }
        else
        {
            _worldPath ??= Path.Combine(_savePath, "world");

            _dataPath ??= Path.Combine(Path.Combine(_savePath, "../data"));
        }

        if (WorldPath is null)
        {
            Logger.LogCritical("Please specify a path for your world: --world \"/path/to/your/save/world\"");
            throw new Exception("No Save Path Specified");
        }

        if (!Directory.Exists(DataPath))
        {
            _dataPath = null;
        }

        Logger.LogInformation($"SavePath: \"{SavePath}\"");
        Logger.LogInformation($"WorldPath: \"{WorldPath}\"");
        Logger.LogInformation($"DataPath: \"{DataPath}\"");
    }

    private static void PrintUsage()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("-w or --world  \"/path/to/your/save/world/\"");
        Console.WriteLine("-s or --save   \"/path/to/your/save/\"");
        Console.WriteLine("Optional:");
        Console.WriteLine("-d or --data   \"/path/to/exported/game/data/folder/\"");
    }
}

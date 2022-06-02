public class DotNet 
{
    public static void Build(string file, BuildSettings settings = null)
    {
        settings ??= new BuildSettings();
        string parameters = $"publish \"{file}\" --verbosity {settings.Verbosity.ToString().ToLower()} ";
        if (settings.NoLogo)
            parameters += "--nologo ";
        if (settings.NoRestore)
            parameters += "--no-restore ";
        if (settings.SelfContained)
            parameters += "--self-contained ";
        if (string.IsNullOrEmpty(settings.Runtime) == false)
            parameters += "-r " + settings.Runtime + " ";
        if (string.IsNullOrEmpty(settings.Configuration) == false)
            parameters += "-c " + settings.Configuration + " ";
        if (string.IsNullOrEmpty(settings.OutputDirectory) == false)
            parameters += "-o \"" + settings.OutputDirectory + "\" ";

        var result = Utils.Exec(BuildOptions.IsWindows ? "dotnet.exe" : "dotnet", parameters);
        if(result.exitCode != 0)
            throw new Exception("Build Failed!");
    }
    
    public enum LoggerVerbosity
    {
        Quiet = 0,
        Minimal = 1,
        Normal = 2,
        Detailed = 3,
        Diagnostic = 4
    }

    public class BuildSettings
    {
        public bool NoLogo { get; set; }
        public bool NoRestore { get; set; }
        public string Configuration { get; set; }
        public string OutputDirectory { get; set; }
        public string Runtime { get; set; }
        public bool SelfContained { get; set; }
        public bool SingleFile { get; set; }
        public LoggerVerbosity Verbosity { get; set; }

        public BuildSettings()
        {
            NoLogo = true;
            Verbosity = LoggerVerbosity.Quiet;
        }
    }
}

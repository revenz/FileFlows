namespace FileFlowsScriptRepo.Generators;

public class Generator
{
    
    /// <summary>
    /// Gets the project root directory by checking if the current working directory is inside a "bin" directory.
    /// If it is, moves up one directory level to find the project root.
    /// </summary>
    /// <returns>The project root directory.</returns>
    protected static string GetProjectRootDirectory()
    {
        string currentDirectory = Environment.CurrentDirectory;

        string binDirectoryMarker = "/bin/";
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            binDirectoryMarker = "\\bin\\";
        }

        int index = currentDirectory.IndexOf(binDirectoryMarker, StringComparison.OrdinalIgnoreCase);
        if (index != -1)
        {
            return currentDirectory.Substring(0, index);
        }

        return currentDirectory;
    }
}
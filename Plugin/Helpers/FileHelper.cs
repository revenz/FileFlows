using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileFlows.Plugin.Helpers
{
    public class FileHelper
    {
        public static bool CreateDirectoryIfNotExists(ILogger logger, string directory)
        {
            if (string.IsNullOrEmpty(directory))
                return false;
            var di = new DirectoryInfo(directory);
            if (di.Exists)
                return true;

            if (IsWindows)
                di.Create();
            else
                CreateLinuxDir(logger, di);

            return di.Exists;
        }

        private static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static bool CreateLinuxDir(ILogger logger, DirectoryInfo di)
        {
            if (di.Exists)
                return true;
            if (di.Parent != null && di.Parent.Exists == false)
            {
                if (CreateLinuxDir(logger, di.Parent) == false)
                    return false;
            }
            logger?.ILog("Creating folder: " + di.FullName);

            string cmd = $"mkdir {EscapePathForLinux(di.FullName)}";

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return ChangeOwner(logger, di.FullName);
                    }
                    logger?.ELog("Failed creating directory:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed creating directory: " + di.FullName + " -> " + ex.Message);
                return false;
            }
        }



        public static bool ChangeOwner(ILogger logger, string filePath, bool recursive = true, bool file = false, bool execute = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // its windows, lets just pretend we did this

            bool log = filePath.Contains("Runner-") == false;

            if (file == false)
            {
                if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                    filePath += Path.DirectorySeparatorChar;

                if(log)
                    logger?.ILog("Changing owner on folder: " + filePath);
            }
            else
            {
                if (log)
                    logger?.ILog("Changing owner on file: " + filePath);
                recursive = false;
            }

            string puid = Environment.GetEnvironmentVariable("PUID")?.EmptyAsNull() ?? "nobody";
            string pgid = Environment.GetEnvironmentVariable("PGID")?.EmptyAsNull() ?? "users";

            string cmd = $"chown{(recursive ? " -R" : "")} {puid}:{pgid} {EscapePathForLinux(filePath)}";
            if (log)
                logger.ILog("Change owner command: " + cmd);

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return SetPermissions(logger, filePath, file: file, execute: execute);
                    logger?.ELog("Failed changing owner:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed changing owner: " + filePath + " => " + ex.Message);
                return false;
            }
        }


        public static bool SetPermissions(ILogger logger, string filePath, bool recursive = true, bool file = false, bool execute = false)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return true; // its windows, lets just pretend we did this

            bool log = filePath.Contains("Runner-") == false;

            if (file == false)
            {
                if (filePath.EndsWith(Path.DirectorySeparatorChar) == false)
                    filePath += Path.DirectorySeparatorChar;
                if(log)
                    logger?.ILog("Setting permissions on folder: " + filePath);
            }
            else
            {
                if (log)
                    logger?.ILog("Setting permissions on file: " + filePath);
                recursive = false;
            }



            string cmd = $"chmod{(recursive ? " -R" : "")} {(execute ? 777 : 666)} {EscapePathForLinux(filePath)}";

            try
            {
                using (var process = new System.Diagnostics.Process())
                {
                    process.StartInfo = new System.Diagnostics.ProcessStartInfo("/bin/bash", $"-c \"{cmd}\"");
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.Start();
                    string output = process.StandardError.ReadToEnd();
                    Console.WriteLine(output);
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                        return true;
                    logger?.ELog("Failed setting permissions:" + process.StartInfo.FileName, process.StartInfo.Arguments + Environment.NewLine + output);
                    if (string.IsNullOrWhiteSpace(error) == false)
                        logger?.ELog("Error output:" + output);
                    return false;
                }
            }
            catch (Exception ex)
            {
                logger?.ELog("Failed setting permissions: " + filePath + " => " + ex.Message);
                return false;
            }
        }

        private static string EscapePathForLinux(string path)
        {
            path = Regex.Replace(path, "([\\'\"\\$\\?\\*()\\s&])", "\\$1");
            return path;
        }

        public static void SaveFile(ILogger logger, string file, byte[] data)
        {
            File.WriteAllBytes(file, data);
            if (IsWindows)
                return;
            ChangeOwner(logger, file, file:true);
        }
        public static void ExtractFile(ILogger logger, string file, string destination)
        {
            System.IO.Compression.ZipFile.ExtractToDirectory(file, destination);
            if (IsWindows)
                return;
            ChangeOwner(logger,destination, execute: true);
        }
    }
}

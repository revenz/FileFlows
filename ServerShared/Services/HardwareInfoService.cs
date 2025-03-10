using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using FileFlows.Plugin;
using FileFlows.Shared.Models;

namespace FileFlows.ServerShared.Services;

/// <summary>
/// Provides methods to retrieve hardware information across different operating systems.
/// </summary>
public class HardwareInfoService
{
    private HardwareInfo? _info;
    
    /// <summary>
    /// Gets the hardware information of the system.
    /// </summary>
    /// <returns>A <see cref="HardwareInfo"/> instance containing the system's hardware information.</returns>
    public HardwareInfo GetHardwareInfo()
    {
        if (_info == null)
        {
            var (processorVendor, processorModel) = GetProcessor();
            _info = new HardwareInfo
            {
                OperatingSystem = GetOperatingSystem(),
                OperatingSystemType = PlatformHelper.GetOperatingSystemType(),
                OperatingSystemVersion = GetOperatingSystemVersion(),
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                Gpus = GetGPUs(),
                ProcessorVendor = processorVendor,
                Processor = processorModel,
                Memory = GetTotalMemory(),
                CoreCount = Environment.ProcessorCount
            };
        }
        return _info;
    }

    /// <summary>
    /// Gets the operating system version.
    /// </summary>
    /// <returns>the opreating system version</returns>
    private string GetOperatingSystemVersion()
    {
        if (OperatingSystem.IsWindows())
            return GetWindowsVersion();
        if (OperatingSystem.IsMacOS())
            return GetMacOSVersion();
        if (OperatingSystem.IsLinux())
            return GetLinuxDistroVersion();
        return Environment.OSVersion.VersionString;
    }

    /// <summary>
    /// Retrieves the operating system name.
    /// </summary>
    /// <returns>The name of the operating system.</returns>
    private string GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return "Windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxDistribution();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return "MacOS";
        return "Unknown OS";
    }

    /// <summary>
    /// Retrieves the names of the GPUs installed on the system along with their details.
    /// </summary>
    /// <returns>A list of <see cref="GpuInfo"/> instances representing the GPUs.</returns>
    private List<GpuInfo> GetGPUs()
    {
        var gpus = new List<GpuInfo>();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Using wmic command to get GPU info
            try
            {
                // Try using the PowerShell approach first
                gpus.AddRange(GetGpuInfoUsingPowerShell());
                if (gpus.Count > 0) return gpus;

                // Try using the WMI approach if PowerShell fails
                gpus.AddRange(GetGpuInfoUsingWmi());
                if (gpus.Count > 0) return gpus;

                // Finally, try using the dxdiag approach
                gpus.AddRange(GetGpuInfoUsingDxdiag());

            }
            catch (Exception)
            {
                // Ignored
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Check for both NVIDIA and AMD GPUs
            // You may need additional permissions to access some of the files
            try
            {
                // NVIDIA GPU detection
                var result = ExecuteCommand("lspci | grep -i nvidia");
                if (result.IsFailed == false)
                {
                    var nvidiaInfo = result.Value.Trim();
                    foreach (var line in nvidiaInfo.Split(Environment.NewLine))
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("VGA"))
                        {
                            var gpu = ParseNvidiaGpu(line);
                            if (gpu != null)
                                gpus.Add(gpu);
                        }
                    }
                }

                // AMD GPU detection
                var amdInfoResult = ExecuteCommand("lspci | grep -i amd");
                if (amdInfoResult.IsFailed == false)
                {
                    var amdInfo = amdInfoResult.Value.Trim();
                    foreach (var line in amdInfo.Split(Environment.NewLine))
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("VGA"))
                        {
                            var gpu = ParseAmdGpu(line);
                            if (gpu != null)
                                gpus.Add(gpu);
                        }
                    }
                }

                // Intel GPU detection
                var intelInfoResult = ExecuteCommand("lspci | grep -i Intel");
                if (intelInfoResult.IsFailed == false)
                {
                    var info = intelInfoResult.Value.Trim();
                    foreach (var line in info.Split(Environment.NewLine))
                    {
                        if (!string.IsNullOrWhiteSpace(line) && line.Contains("VGA"))
                        {
                            var gpu = ParseIntelGpu(line);
                            if (gpu != null)
                                gpus.Add(gpu);
                        }
                    }
                }
            }
            catch
            {
                // Handle if the command fails
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Using system_profiler to get GPU info on macOS
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "system_profiler";
                process.StartInfo.Arguments = "SPDisplaysDataType";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                foreach (var line in output.Split(Environment.NewLine))
                {
                    if (line.Contains("Chipset Model"))
                    {
                        var model = line.Split(':')[1].Trim();
                        gpus.Add(new GpuInfo
                        {
                            Vendor = model.Contains("Intel", StringComparison.InvariantCultureIgnoreCase) ? "Intel" :
                                model.Contains("Apple", StringComparison.InvariantCultureIgnoreCase) ? "Apple" :
                                model,
                            Model = CleanUpModel(model)!,
                            Memory = GetMacGpuMemory(),
                            DriverVersion = GetMacDriverVersion()
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Ignored
            }
        }

        return gpus;
    }


    /// <summary>
    /// Parses the GPU model information string.
    /// </summary>
    /// <param name="line">The model information string from the GPU.</param>
    /// <returns>A tuple containing the vendor and model of the GPU.</returns>
    private GpuInfo ParseAmdGpu(string line)
    {
        // Example input:
        // "Model: 0b:00.0 VGA compatible controller: Advanced Micro Devices, Inc. [AMD/ATI] Navi 23 [Radeon RX 6600/6600 XT/6600M] (rev c1)"

        string vendor = "AMD"; // Set vendor to AMD
        string model = "Unknown";

        // Split the model info to isolate the relevant sections
        var parts = line.Split(new[] { " VGA compatible controller: " }, StringSplitOptions.None);

        if (parts.Length > 1)
        {
            // Extract model information
            model = parts[1][parts[1].IndexOf('[')..]
                .Trim(); // Extracts "[AMD/ATI] Navi 23 [Radeon RX 6600/6600 XT/6600M]"
            model = model.Trim().Replace("(", "").Replace(")", ""); // Clean up parentheses
        }

        if (model.StartsWith("[AMD/ATI]"))
            model = model[9..].Trim(); // Remove "[AMD/ATI]" prefix

        var match = Regex.Match(model, @"\[(Radeon [^]]+)\]");
        if (match is { Success: true, Groups.Count: > 1 })
            model = match.Groups[1].Value; // Extract the Radeon model

        return new()
        {
            Vendor = vendor,
            Model = model,
            Memory = GetAmdMemory(line), // Call your existing method to get memory
            DriverVersion = GetAmdDriverVersion() // Call your existing method to get driver version
        };
    }

    /// <summary>
    /// Parses the GPU model information string for Intel GPUs.
    /// </summary>
    /// <param name="line">The model information string from the GPU.</param>
    /// <returns>A tuple containing the vendor, model, memory (in bytes), and driver version of the GPU.</returns>
    private GpuInfo ParseIntelGpu(string line)
    {
        // Example input:
        // 55:00.0 VGA compatible controller: Intel Corporation DG2 [Arc A380] (rev 05)

        string vendor = "Intel"; // Set vendor to Intel
        string model = "Unknown";
        long memory = 0; // Initialize memory in bytes (long type to handle larger values)

        // Split the model info to isolate the relevant sections
        var parts = line.Split(new[] { " VGA compatible controller: " }, StringSplitOptions.None);

        if (parts.Length > 1)
        {
            // Extract model information
            model = parts[1].Substring(parts[1].IndexOf("Intel") + 6).Trim(); // Extracts Intel model part
            model = model.Substring(0, model.IndexOf(" (")).Trim(); // Remove the revision info

            // Check if it's an Arc model (e.g., Arc A380, Arc A750, Arc B580)
            var match = Regex.Match(model, @"\[(Arc [^]]+)\]");
            if (match.Success)
            {
                model = match.Groups[1].Value; // Extract the Intel Arc model (e.g., "Arc A380")
            }
        }

        // Check for memory size in the line (e.g., "Memory: 8 GB")
        var memoryMatch = Regex.Match(line, @"Memory:\s*(\d+)\s*(GB|MB|KB)?");
        if (memoryMatch.Success)
        {
            // Extract the number and unit
            long memorySize = long.Parse(memoryMatch.Groups[1].Value);
            string memoryUnit = memoryMatch.Groups[2].Value.ToUpper();

            // Convert to bytes based on the unit
            memory = memoryUnit switch
            {
                "GB" => memorySize * 1024 * 1024 * 1024, // GB to bytes
                "MB" => memorySize * 1024 * 1024, // MB to bytes
                "KB" => memorySize * 1024, // KB to bytes
                _ => memorySize, // Default is bytes (no conversion needed)
            };
        }

        // If memory is 0, set a default based on GPU model using a switch
        if (memory == 0)
        {
            memory = model switch
            {
                "Arc A380" => 4L * 1024 * 1024 * 1024, // 4 GB in bytes
                "Arc A750" => 8L * 1024 * 1024 * 1024, // 8 GB in bytes
                "Arc A770" => 8L * 1024 * 1024 * 1024, // 8 GB in bytes
                "Arc B570" => 10L * 1024 * 1024 * 1024, // 8 GB in bytes
                "Arc B580" => 12L * 1024 * 1024 * 1024, // 8 GB in bytes
                _ => 0 // Default for other models
            };
        }

        return new GpuInfo()
        {
            Vendor = vendor,
            Model = model,
            Memory = memory, // Memory in bytes
        };
    }

    /// <summary>
    /// Parses the NVIDIA GPU info
    /// </summary>
    /// <param name="gpuInfo">the line to parse</param>
    /// <returns>The GpuInfo or null if invalid</returns>
    private GpuInfo? ParseNvidiaGpu(string gpuInfo)
    {
        // Example input: "01:00.0 VGA compatible controller: NVIDIA Corporation AD107M [GeForce RTX 4060 Max-Q / Mobile] (rev a1)"

        var parts = gpuInfo.Split(new[] { ':' }, 2); // Split at the first colon
        if (parts.Length < 2)
        {
            return null; // Not a valid format
        }

        var description = parts[1].Trim(); // Get the description part

        // Extract model
        var modelStartIndex = description.IndexOf('[');
        var modelEndIndex = description.IndexOf(']');
        var model = modelStartIndex >= 0 && modelEndIndex > modelStartIndex
            ? description.Substring(modelStartIndex + 1, modelEndIndex - modelStartIndex - 1).Trim()
            : "Unknown Model";

        return new GpuInfo
        {
            Vendor = "NVIDIA",
            Model = model,
            Memory = GetNvidiaMemory(gpuInfo), // Call your existing method to get memory
            DriverVersion = GetNvidiaDriverVersion() // Call your existing method to get driver version
        };
    }


    /// <summary>
    /// Executes a command
    /// </summary>
    /// <param name="command">the command to execute</param>
    /// <returns>the result of the command</returns>
    private Result<string> ExecuteCommand(string command)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            return process.StandardOutput.ReadToEnd();
        }
        catch (Exception ex)
        {
            return Result<string>.Fail(ex.Message);
        }
    }

    /// <summary>
    /// Retrieves the memory of NVIDIA GPUs in bytes.
    /// </summary>
    /// <param name="gpuInfo">Information about the GPU.</param>
    /// <returns>The memory size of the GPU in bytes.</returns>
    private long GetNvidiaMemory(string gpuInfo)
    {
        // Use the name of the GPU to query its memory size
        // Command to get memory size for a specific GPU based on its name
        string command =
            $"nvidia-smi --query-gpu=memory.total --format=csv,noheader,nounits -i {gpuInfo.Split(' ')[0]}";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of NVIDIA GPUs.
    /// </summary>
    /// <returns>The driver version of the NVIDIA GPU.</returns>
    private string GetNvidiaDriverVersion()
    {
        var result = ExecuteCommand("nvidia-smi --query-gpu=driver_version --format=csv,noheader");
        return result.IsFailed ? string.Empty : result.Value.Trim();
    }

    /// <summary>
    /// Retrieves the memory of AMD GPUs in bytes.
    /// </summary>
    /// <param name="gpuInfo">Information about the GPU.</param>
    /// <returns>The memory size of the AMD GPU in bytes.</returns>
    private long GetAmdMemory(string gpuInfo)
    {
        // Command to get memory size for a specific AMD GPU based on its name
        string command = $"lspci -v -s {gpuInfo.Split(' ')[0]} | grep 'Memory size' | awk '{{print $3}}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value (assumes output is in MB)
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of AMD GPUs.
    /// </summary>
    /// <returns>The driver version of the AMD GPU.</returns>
    private string GetAmdDriverVersion()
    {
        // Command to get the driver version for AMD GPUs
        var result = ExecuteCommand("glxinfo | grep 'OpenGL version'");
        return result.IsFailed ? string.Empty : result.Value.Trim();
    }


    /// <summary>
    /// Retrieves the memory size of the GPU on macOS in bytes.
    /// </summary>
    /// <returns>The memory size of the macOS GPU in bytes.</returns>
    private long GetMacGpuMemory()
    {
        // Command to get GPU memory size on macOS
        string command = "system_profiler SPDisplaysDataType | grep 'VRAM' | awk '{print $2}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return 0;
        string output = result.Value.Trim();

        // Try to parse the output as a long value (assumes output is in MB)
        if (long.TryParse(output, out long memorySize))
        {
            return memorySize * 1024 * 1024; // Convert MB to Bytes
        }

        return 0; // Default to 0 if parsing fails
    }

    /// <summary>
    /// Retrieves the driver version of the GPU on macOS.
    /// </summary>
    /// <returns>The driver version of the macOS GPU.</returns>
    private string GetMacDriverVersion()
    {
        // Command to get GPU driver version on macOS
        string command = "system_profiler SPDisplaysDataType | grep 'Driver Version' | awk '{print $3}'";

        // Execute the command and get the output
        var result = ExecuteCommand(command);
        if (result.IsFailed)
            return string.Empty;
        string output = result.Value.Trim();

        return output?.EmptyAsNull() ?? string.Empty;
    }


    /// <summary>
    /// Retrieves the name of the processor.
    /// </summary>
    /// <returns>The name of the processor.</returns>
    private (string vendor, string model) GetProcessor()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Using wmic command to get Processor info
            try
            {
                using var process = new Process();
                process.StartInfo.FileName = "wmic";
                process.StartInfo.Arguments = "cpu get name";
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.Start();
                return CleanUpCpu(process.StandardOutput.ReadToEnd().Split(Environment.NewLine)[1].Trim());
            }
            catch (Exception)
            {
                // Ignored
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Using /proc/cpuinfo to get Processor info
            var result = ExecuteCommand("cat /proc/cpuinfo | grep 'model name' | uniq | awk -F: '{print $2}'");
            if (result.IsFailed == false)
                return CleanUpCpu(result.Value.Trim());
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            // Using sysctl to get Processor info
            var result = ExecuteCommand("sysctl -n machdep.cpu.brand_string");
            if (result.IsFailed == false)
                return CleanUpCpu(result.Value.Trim());
        }

        return (string.Empty, "Unknown Processor");
    }

    /// <summary>
    /// Retrieves GPU information using PowerShell commands.
    /// </summary>
    /// <returns>A list of <see cref="GpuInfo"/> objects with GPU details.</returns>
    private List<GpuInfo> GetGpuInfoUsingPowerShell()
    {
        var gpus = new List<GpuInfo>();

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "powershell";
            process.StartInfo.Arguments =
                "Get-WmiObject win32_VideoController | Select-Object Name, AdapterRAM, DriverVersion | ConvertTo-Csv -NoTypeInformation";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < lines.Length; i++) // Skip header
            {
                var parts = lines[i].Split(',');

                if (parts.Length >= 3)
                {
                    var model = CleanUpModel(parts[0]);
                    var vendor = CleanUpVendor(parts[0]);
                    if (model == null || vendor == null)
                        continue;
                    gpus.Add(new GpuInfo
                    {
                        Vendor = vendor!, // May need to adjust based on output
                        Model = model, // Same as Vendor in this output
                        Memory = long.TryParse(parts[1].Trim(), out long memory) ? memory : 0,
                        DriverVersion = parts[2].Trim().TrimStart('"').TrimEnd('"')
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving GPU info using PowerShell: {ex.Message}");
        }

        return gpus;
    }
    /// <summary>
    /// Cleans up a CPU string.
    /// </summary>
    /// <param name="cpu">the CPU to clean up</param>
    /// <returns>the cleaned up CPU, or null if a bad model</returns>
    private (string vendor, string model) CleanUpCpu(string? cpu)
    {
        if (cpu == null)
            return (string.Empty, string.Empty);
        string vendor = cpu.Contains("Intel", StringComparison.InvariantCultureIgnoreCase) ? "Intel" :
            cpu.Contains("Apple", StringComparison.InvariantCultureIgnoreCase) ? "Apple" :
            cpu.Contains("AMD", StringComparison.InvariantCultureIgnoreCase) ? "AMD" : cpu;
        
        cpu = cpu.Replace("Intel(R) ", ""); // Remove Intel(R) prefix
        cpu = cpu.Replace("Core(TM) ", ""); // Remove Intel(R) prefix
        cpu = Regex.Replace(cpu, @"\b\d+(?:st|nd|rd|th) Gen\b", ""); // Remove generation suffix
        cpu = Regex.Replace(cpu, @"\b\d+[\s\-]Core\b", ""); // Remove core suffix
        cpu = cpu.Replace("Processor", "");
        cpu = cpu.Replace("NVIDIA ", "", StringComparison.InvariantCultureIgnoreCase); // Remove NVIDIA prefix
        cpu = cpu.Replace("Apple ", "", StringComparison.InvariantCultureIgnoreCase); // Remove Apple prefix
        cpu = cpu.Replace("AMD ", "", StringComparison.InvariantCultureIgnoreCase); // Remove AMD prefix
        cpu = cpu.Replace("(TM)", ""); // Remove (TM) suffix
        cpu = cpu.Replace("(R)", ""); // Remove (R) suffix
        cpu = cpu.Replace(" CPU", ""); // Remove CPU suffix
        cpu = cpu.Replace(" Genuine 0000", ""); // Remove Genuine 0000 suffix
        // remove @ 3.60GHz etc
        cpu = Regex.Replace(cpu, @"@\s*\d+\.\d+GHz", "");
        while (cpu.Contains("  "))
            cpu = cpu.Replace("  ", " ");
        return (vendor, cpu.Trim());
    }

    /// <summary>
    /// Cleans up a model string.
    /// </summary>
    /// <param name="model">the model to clean up</param>
    /// <returns>the cleaned up model name, or null if a bad model</returns>
    private string? CleanUpModel(string? model)
    {
        if (model == null)
            return null;
        model = model.Trim().TrimStart('"').TrimEnd('"');
        if (model.Contains("Virtual Desktop Monitor", StringComparison.InvariantCultureIgnoreCase))
            return null;
        if (model.Contains("Microsoft Remote", StringComparison.InvariantCultureIgnoreCase))
            return null;
        if (model.Contains("Virtual Display", StringComparison.InvariantCultureIgnoreCase))
            return null;
        if (model.Contains("USB Device", StringComparison.InvariantCultureIgnoreCase))
            return null;
        model = model.Replace("Intel(R) ", ""); // Remove Intel(R) prefix
        model = model.Replace("NVIDIA ", "", StringComparison.InvariantCultureIgnoreCase); // Remove NVIDIA prefix
        model = model.Replace("Apple ", "", StringComparison.InvariantCultureIgnoreCase); // Remove Apple prefix
        model = model.Replace("AMD ", "", StringComparison.InvariantCultureIgnoreCase); // Remove AMD prefix
        model = model.Replace(" Lite Hash Rate", "", StringComparison.InvariantCultureIgnoreCase); 
        model = model.Replace("(TM)", ""); // Remove (TM) suffix
        model = model.Replace("(R)", ""); // Remove (R) suffix
        model = model.Replace("Graphics", ""); // Remove Graphics suffix
        while (model.Contains("  "))
            model = model.Replace("  ", " ");
        return model;
    }

    /// <summary>
    /// Cleans up a vendor string.
    /// </summary>
    /// <param name="vendor">the vendor to clean up</param>
    /// <returns>the cleaned up vendor name, or null if a bad model</returns>
    private string? CleanUpVendor(string? vendor)
    {
        if(string.IsNullOrWhiteSpace(vendor))
            return null;
        if (vendor.Contains("NVIDIA", StringComparison.InvariantCultureIgnoreCase) == true)
            return "NVIDIA";
        if (vendor.Contains("AMD", StringComparison.InvariantCultureIgnoreCase) == true)
            return "AMD";
        if (vendor.Contains("Radeon", StringComparison.InvariantCultureIgnoreCase) == true)
            return "AMD";
        if (vendor.Contains("VMware", StringComparison.InvariantCultureIgnoreCase) == true)
            return "VMware";
        if (vendor.Contains("Intel", StringComparison.InvariantCultureIgnoreCase) == true)
            return "Intel";
        if (vendor.Contains("Microsoft Remote", StringComparison.InvariantCultureIgnoreCase))
            return null;
        if (vendor.Contains("Virtual Display", StringComparison.InvariantCultureIgnoreCase))
            return null;
        if (vendor.Contains("USB Device", StringComparison.InvariantCultureIgnoreCase))
            return null;
        vendor = vendor.Trim().TrimStart('"').TrimEnd('"');
        return vendor;
    }

    /// <summary>
    /// Retrieves GPU information using WMI.
    /// </summary>
    /// <returns>A list of <see cref="GpuInfo"/> objects with GPU details.</returns>
    private List<GpuInfo> GetGpuInfoUsingWmi()
    {
        var gpus = new List<GpuInfo>();
        if (OperatingSystem.IsWindows() == false)
            return gpus;

#pragma warning disable CA1416
        try
        {
            using var searcher =
                new ManagementObjectSearcher("select Name, AdapterRAM, DriverVersion from Win32_VideoController");
            foreach (var obj in searcher.Get())
            {
                var model = CleanUpModel(obj["Name"]?.ToString());
                var vendor = CleanUpVendor(obj["Name"]?.ToString());
                if (model == null || vendor == null)
                    continue;
                
                gpus.Add(new GpuInfo
                {
                    Vendor = vendor,
                    Model = model,
                    Memory = Convert.ToInt64(obj["AdapterRAM"] ?? 0),
                    DriverVersion = obj["DriverVersion"]?.ToString() ?? string.Empty
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog($"Error retrieving GPU info using WMI: {ex.Message}");
        }
#pragma warning restore CA1416

        return gpus;
    }

    /// <summary>
    /// Retrieves GPU information using the DirectX Diagnostic Tool (dxdiag).
    /// </summary>
    /// <returns>A list of <see cref="GpuInfo"/> objects with GPU details.</returns>
    private List<GpuInfo> GetGpuInfoUsingDxdiag()
    {
        var gpus = new List<GpuInfo>();

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "dxdiag";
            process.StartInfo.Arguments = "/t dxdiag_output.txt";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            // Read the generated output file
            string output = System.IO.File.ReadAllText("dxdiag_output.txt");

            // Parsing the output for GPU information
            // Split the output into lines
            var lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            GpuInfo? currentGpu = null;

            foreach (var line in lines)
            {
                // Check for GPU vendor information
                if (line.StartsWith("Card name: "))
                {
                    if (currentGpu != null)
                    {
                        gpus.Add(currentGpu); // Add the previous GPU info if it exists
                    }

                    currentGpu = new GpuInfo
                    {
                        Model = line.Substring("Card name: ".Length).Trim() // Extract the model
                    };
                    continue;
                }

                // Check for GPU memory information
                if (line.StartsWith("Display Memory: "))
                {
                    if (currentGpu != null)
                    {
                        string memoryString = line["Display Memory: ".Length..].Trim();
                        // Example: "8192 MB"
                        if (long.TryParse(memoryString.Split(' ')[0], out long memoryInMb))
                        {
                            currentGpu.Memory = memoryInMb * 1024 * 1024; // Convert MB to bytes
                        }
                    }

                    continue;
                }

                // Check for driver version information
                if (line.StartsWith("Driver Version: "))
                {
                    if (currentGpu != null)
                    {
                        currentGpu.DriverVersion =
                            line.Substring("Driver Version: ".Length).Trim(); // Extract the driver version
                    }

                    continue;
                }
            }

            // Add the last GPU info if exists
            if (currentGpu != null)
            {
                gpus.Add(currentGpu);
            }

            // Clean up output file
            System.IO.File.Delete("dxdiag_output.txt");
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog($"Error retrieving GPU info using dxdiag: {ex.Message}");
        }

        return gpus;
    }

    /// <summary>
    /// Gets the total physical memory in bytes.
    /// </summary>
    /// <returns>The total physical memory in bytes.</returns>
    public long GetTotalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetWindowsMemory();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetLinuxMemory();
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return GetMacMemory();
        return 0;
    }

    /// <summary>
    /// Gets the total physical memory for Windows.
    /// </summary>
    /// <returns>The total physical memory in bytes.</returns>
    private long GetWindowsMemory()
    {
      
        if (OperatingSystem.IsWindows() == false)
            return 0;
#pragma warning disable CA1416
        try
        {
            // Using ManagementObjectSearcher to get total physical memory
            using var searcher =
                new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            foreach (var obj in searcher.Get())
            {
                // Convert TotalVisibleMemorySize from kilobytes to bytes
                return Convert.ToInt64(obj["TotalVisibleMemorySize"]) * 1024;
            }
        }
        catch (Exception ex)
        {
            // Log or handle exceptions as needed
            Logger.Instance?.WLog("Failed getting memory size on Windows: " + ex.Message);
        }
#pragma warning restore CA1416
        return 0; // Fallback if unable to retrieve memory
    }

    /// <summary>
    /// Gets the total physical memory for Linux.
    /// </summary>
    /// <returns>The total physical memory in bytes.</returns>
    private long GetLinuxMemory()
    {
        try
        {
            string memInfo = System.IO.File.ReadAllText("/proc/meminfo");
            var match = Regex.Match(memInfo, @"MemTotal:\s+(\d+)\s+kB");
            if (match.Success)
            {
                return ConvertBytesToRoundedGBInBytes(long.Parse(match.Groups[1].Value) * 1024); // Convert kB to bytes
            }
        }
        catch (Exception ex)
        {
            // Log or handle exceptions as needed
            Logger.Instance?.WLog("failed getting memory size on Linux: " + ex.Message);
        }

        return 0; // Fallback if unable to retrieve memory
    }

    /// <summary>
    /// Converts memory size in bytes to gigabytes (GB), rounding to the nearest standard size
    /// (4GB, 8GB, 16GB, 32GB, etc.) if the value is within a 5% tolerance of those sizes,
    /// and returns the value in bytes.
    /// </summary>
    /// <param name="bytes">The memory size in bytes.</param>
    /// <returns>The memory size in bytes, rounded to the nearest standard GB size if close enough, or the exact size otherwise.</returns>
    private long ConvertBytesToRoundedGBInBytes(long bytes)
    {
        // Convert bytes to gigabytes
        double totalGB = bytes / (1024.0 * 1024.0 * 1024.0);

        // Predefined list of standard GB sizes
        long[] standardSizesGB = { 4, 8, 16, 32, 64, 128, 256 };

        // Define tolerance as 5% (you can adjust this)
        double tolerancePercentage = 0.05;

        foreach (var size in standardSizesGB)
        {
            // Calculate the tolerance range
            double lowerBound = size * (1 - tolerancePercentage);
            double upperBound = size * (1 + tolerancePercentage);

            // If totalGB is within the tolerance range, round to the standard size
            if (totalGB >= lowerBound && totalGB <= upperBound)
            {
                // Return the rounded value in bytes
                return size * 1024L * 1024L * 1024L;
            }
        }

        // If not close to any standard size, return the actual memory size in bytes
        return bytes;
    }

    /// <summary>
    /// Gets the total physical memory for macOS.
    /// </summary>
    /// <returns>The total physical memory in bytes.</returns>
    private long GetMacMemory()
    {
        try
        {
            // Use sysctl to get physical memory
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "hw.memsize",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Parse output to extract memory
            var match = Regex.Match(output, @"hw\.memsize:\s+(\d+)");
            if (match.Success)
            {
                return long.Parse(match.Groups[1].Value);
            }
        }
        catch (Exception ex)
        {
            Logger.Instance?.WLog("Failed getting memory size on macOS: " + ex.Message);
            // Log or handle exceptions as needed
        }

        return 0; // Fallback if unable to retrieve memory
    }
    
    /// <summary>
    /// Gets the Windows version string and returns the human-readable version name.
    /// </summary>
    /// <returns>The human-readable version name, e.g., "Windows 7", "Windows 8", "Windows 8.1", "Windows 10", "Windows 11", or "Unknown Windows Version".</returns>
    public static string GetWindowsVersion()
    {
        // The string containing the Windows version information, such as "Windows(Microsoft Windows NT 10.0.22631.0)".
        var input =Environment.OSVersion.VersionString;
        // Regular expression to capture the version number (e.g., 10.0)
        var regex = new Regex(@"Windows NT (?<version>\d+\.\d+)");
        var match = regex.Match(input);

        if (match.Success)
        {
            // Extract the version number (e.g., "10.0")
            var version = match.Groups["version"].Value;

            // Handle Windows 10 and 11 based on build numbers
            if (version == "10.0")
            {
                // Extract the build number to distinguish between Windows 10 and Windows 11
                var buildRegex = new Regex(@"(\d+\.\d+\.(?<build>\d+)\.\d+)");
                var buildMatch = buildRegex.Match(input);

                if (buildMatch.Success && int.TryParse(buildMatch.Groups["build"].Value, out int build))
                {
                    // Windows 10: Build numbers lower than 22000
                    // Windows 11: Build numbers 22000 and above
                    return build >= 22000 ? "11" : "10";
                }

                return "10"; // Default to Windows 10 if build number is unknown
            }

            // Map other versions based on the version number
            return version switch
            {
                "6.1" => "7",
                "6.2" => "8",
                "6.3" => "8.1",
                _ => input
            };
        }

        // Return a default message if no version is matched
        return input;
    }
    
    /// <summary>
    /// Gets the macOS version using the `sw_vers` command.
    /// </summary>
    /// <returns>A string representing the macOS version.</returns>
    private static string GetMacOSVersion()
    {
        try
        {
            using var process = new Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "sw_vers",
                    Arguments = "-productVersion",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            string version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return version;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to parse MacOS version: " + ex.Message);
            return Environment.OSVersion.VersionString;
        }
    }
    
    /// <summary>
    /// Gets the Linux distribution name and version from /etc/os-release.
    /// </summary>
    /// <returns>A string representing the Linux distribution and version.</returns>
    private static string GetLinuxDistroVersion()
    {
        try
        {
            string[] osRelease = File.ReadAllLines("/etc/os-release");
            foreach (string line in osRelease)
            {
                if (line.StartsWith("VERSION_ID="))
                {
                    return line.Split('=')[1].Trim('\"');
                }
            }

            return Environment.OSVersion.VersionString;
        }
        catch (Exception ex)
        {
            Logger.Instance.WLog("Failed to parse Linux version: " + ex.Message);
            return Environment.OSVersion.VersionString;
        }
    }

    /// <summary>
    /// Gets the Linux distribution name from /etc/os-release.
    /// </summary>
    /// <returns>A string representing the Linux distribution.</returns>
    private static string GetLinuxDistribution()
    {
        try
        {
            string[] osRelease = File.ReadAllLines("/etc/os-release");
            foreach (string line in osRelease)
            {
                if (line.StartsWith("NAME="))
                    return line.Split('=')[1].Trim('\"');
            }

            return "Linux";
        }
        catch (Exception)
        {
            return "Linux";
        }
    }
}


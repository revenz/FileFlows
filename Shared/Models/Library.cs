using FileFlows.Plugin;

namespace FileFlows.Shared.Models
{
    public class Library : FileFlowObject
    {
        public bool Enabled { get; set; }
        public string Path { get; set; }
        public string Filter { get; set; }
        public string Template { get; set; }
        public string Description { get; set; }
        public ObjectReference Flow { get; set; }

        public bool Scan { get; set; }

        /// <summary>
        /// If this library monitors for folders or files
        /// </summary>
        public bool Folders { get; set; }

        /// <summary>
        /// Gets or sets if this library will use fingerprinting to determine if a file already is known
        /// </summary>
        public bool UseFingerprinting { get; set; }

        /// <summary>
        /// Gets or sets if hidden files and folders should be excluded from the library
        /// </summary>
        public bool ExcludeHidden { get; set; }

        public string Schedule { get; set; }

        /// <summary>
        /// When the library was last scanned
        /// </summary>
        public DateTime LastScanned { get; set; }


        /// <summary>
        /// The timespan of when this was last scanned
        /// </summary>
        public TimeSpan LastScannedAgo => DateTime.Now - LastScanned;

        /// <summary>
        /// Gets or sets the number of seconds to scan files
        /// </summary>
        public int ScanInterval { get; set; }

        /// <summary>
        /// Gets or sets the number of seconds to wait before checking for file size changes when scanning the library
        /// </summary>
        public int FileSizeDetectionInterval { get; set; }

        /// <summary>
        /// Gets or sets the processing priority of this library
        /// </summary>
        public ProcessingPriority Priority { get; set; }
    }

    public enum ProcessingPriority
    {
        Lowest = -10,
        Low = -5,
        Normal = 0,
        High = 5,
        Highest = 10

    }
}
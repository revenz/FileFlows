namespace FileFlows.Client
{
    /// <summary>
    /// GLobals
    /// </summary>
    public class Globals
    {
        /// <summary>
        /// Gets the version of FileFlows
        /// </summary>
#if(DEBUG)
        public static readonly string Version = DateTime.UtcNow.ToString("yy.MM") + ".9.9999";
#else
        public const string Version = "23.10.2.2469";
#endif

        public static string LIST_OPTION_GROUP = "###GROUP###";


        private static string _lblName;
        public static string lblName
        {
            get
            {
                if (_lblName == null)
                    _lblName = Translater.Instant("Labels.Name");
                return _lblName;
            }
        }
        private static string _lblEnabled;
        public static string lblEnabled
        {
            get
            {
                if (_lblEnabled == null)
                    _lblEnabled = Translater.Instant("Labels.Enabled");
                return _lblEnabled;
            }
        }
    }
}

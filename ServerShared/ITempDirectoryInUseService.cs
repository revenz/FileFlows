namespace FileFlows.ServerShared
{
    public interface ITempDirectoryInUseService
    {
        public bool IsTempDirectoryInUse(string directory);
    }
}

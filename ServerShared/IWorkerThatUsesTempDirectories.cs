namespace FileFlows.ServerShared
{
    public interface IWorkerThatUsesTempDirectories
    {
        public bool IsTempDirectoryInUse(string directory);
    }
}

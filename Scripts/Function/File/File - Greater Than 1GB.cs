/**
 * Checks if a file is larger than 1 GB
 * @revision 1
 * @outputs 2
 * @minimumVersion 24.08.1.3441
 */

if(Variables.TryGetValue("file.Size", out var oSize) == false || oSize is long size == false)
{
	Flow.FailureReason = "file.Size not set in variables";
	Logger.ELog(Flow.FailureReason);
	return -1;
}
if(size > 1_000_000_000)
	return 1;
return 2;
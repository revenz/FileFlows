/**
* Checks if a file is older than the specified days 
* @author John Andrews 
* @uid fbdd7b81-5112-43eb-a371-6aaf9b42977d
* @revision 3
* @minimumVersion 1.0.0.0
* @param {int} Days The number of days to check how old the file is 
* @param {bool} UseLastWriteTime If the last write time should be used, otherwise the creation time will be 
* @output The file is older than the days specified 
* @output the file is not older than the days specified
*/
function Script(Days, UseLastWriteTime)
{
	var fi = FileInfo(Flow.WorkingFile); 
	let date = UseLastWriteTime ? fi.LastWriteTime : fi.CreationTime;
    
    // time difference 
    let timeDiff = new Date().getTime() - date;
    // convert that time to number of days 
    let dayDiff = Math.round(timeDiff / (1000 * 3600 * 24));
    
    Logger.ILog(`File is ${dayDiff} days old`);
    
	return dayDiff > Days ? 1 : 2;
}
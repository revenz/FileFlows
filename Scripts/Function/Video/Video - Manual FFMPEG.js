/**
 * Custom FFMPEG command
 * @revision 3
 * @outputs 1
 * @minimumVersion 1.0.0.0
 */

let output = Flow.TempPath + '/' + Flow.NewGuid() + '.mkv';
let ffmpeg = Flow.GetToolPath('ffmpeg');
let process = Flow.Execute({
	command: ffmpeg,
	argumentList: [
		'-i',
		Variables.file.FullName,
		'-c:v',
		'libx265',
		'-c:a',
		'copy',
		output
	]
});

if(process.standardOutput)
	Logger.ILog('Standard output: ' + process.standardOutput);
if(process.standardError)
	Logger.ILog('Standard error: ' + process.standardError);

if(process.exitCode !== 0){
	Logger.ELog('Failed processing ffmpeg: ' + process.exitCode);
	return -1;
}

Flow.SetWorkingFile(output);
return 1;
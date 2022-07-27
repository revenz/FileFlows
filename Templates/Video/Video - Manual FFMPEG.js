// @outputs 1

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
if(process.starndardError)
	Logger.ILog('Standard error: ' + process.starndardError);

if(process.exitCode !== 0){
	Logger.ELog('Failed processing ffmpeg: ' + process.exitCode);
	return -1;
}

Flow.SetWorkingFile(output);
return 1;
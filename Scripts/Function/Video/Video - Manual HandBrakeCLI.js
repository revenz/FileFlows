/**
 * Custom Handbrake CLI command
 * @revision 2
 * @outputs 1
 * @minimumVersion 1.0.0.0
 */

// You need to configure HandBrakeCLI as a tool under the tools page for this to work
// refer to the FFMPEG tool as a reference
let handbrakecli = Flow.GetToolPath('HandBrakeCLI');

// generate a unique filename to save the output from handbrakecli to
let output = Flow.TempPath + '/' + Flow.NewGuid() + '.mkv';

let process = Flow.Execute({
	command: handbrakecli,
	argumentList: [
		'-i',
		Variables.file.FullName,
		'-o',
		output,
		'-Z',
		'H.265 MKV 1080p30',
        '-e',
        'x265',
        '-E',
        'copy',
        '--audio-fallback',
        'aac',
        '--all-audio',
        '--all-subtitles',
        '--decomb',
        '--enable-qsv-decoding'
	]
});

// ensure handbrake exited with a success exit code
if(process.exitCode !== 0)
{
	Logger.ELog('Failed processing HandBrakeCLI: ' + process.exitCode);
	return -1;
}

// ensure the output file exists
if(Flow.FileExists(output) !== true)
{
	Logger.ELog('Output file does not exist from HandBrakeCLI: ' + output);
	return -1;
}

// update the working file in the flow to the newly created file
Flow.SetWorkingFile(output);
return 1;
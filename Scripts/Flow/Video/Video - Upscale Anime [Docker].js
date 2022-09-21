/**
 * Upscales anime using dandere2x inside a docker container
 * Requires TempPathHost environment variable to be set if running inside a docker container
 * @author John Andrews
 * @revision 1
 * @minimumVersion 1.0.4.0
 * @output file was upscaled
 */
 function Script()
 {
    // copy the file into temporary directory (if not already in the temp directory)
    let wf = Flow.CopyToTemp();
    let shortFile =  wf.substring(wf.lastIndexOf(Flow.IsWindows ? '\\' : '/') + 1);
    let output = Flow.NewGuid() + '.' + shortFile.substring(shortFile.lastIndexOf('.') + 1);
    let tempPath = Flow.TempPathHost;

    Logger.ILog('ShortFile: ' + shortFile);
    Logger.ILog('Output: ' + output);
    Logger.ILog('TempPath: ' + tempPath);

    let process = Flow.Execute({
        command: 'docker',
        argumentList: [
            'run',
            '--gpus',
            'all',
            '--rm',
            '-v',
            tempPath + ':/host',
            'akaikatto/dandere2x',
            '-p', 'singleprocess',
            '-ws', './workspace/',
            '-i', '/host/' + shortFile,
            '-o', '/host/' + output
        ]
    });

    if(process.standardOutput)
        Logger.ILog('Standard output: ' + process.standardOutput);
    if(process.starndardError)
        Logger.ILog('Standard error: ' + process.starndardError);

    if(process.exitCode !== 0){
        Logger.ELog('Failed processing: ' + process.exitCode);
        return -1;
    }

    output = Flow.TempPath + '/' + output;
    Flow.SetWorkingFile(output);
    return 1;
 }
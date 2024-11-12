/**
 * @author reven
 * @uid a688f844-988f-4541-90a6-0fc189fea1c4
 * @description Upscales anime using Dandere2x inside a docker container. Requires TempPathHost environment variable to be set if running inside a docker container
 * @revision 4
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
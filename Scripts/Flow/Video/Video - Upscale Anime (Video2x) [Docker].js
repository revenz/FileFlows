/**
 * @author reven
 * @uid ebbea155-596c-4be9-93bf-24858f6b0765
 * @revision 4
 * @description Upscales anime using Video2x inside a docker container. Requires TempPathHost environment variable to be set if running inside a docker container
 * @param {int} Height The height of the video, e.g 720, 1080
 * @param {int} Processes The number of processes to use, if in doubt, set this to 1
 * @output file was upscaled
 */
 function Script(Height, Processes)
 {
    if(Processes < 1)
        Processes = 1;

    // copy the file into temporary directory
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
            '--rm',
            '--privileged',
            '--gpus',
            'all,"capabilities=compute,utility,graphics,display"',
            //'all',
            '--env',
            'DISPLAY:$DISPLAY',
            '-v',
            tempPath + ':/host',
            '-v',
            tempPath + ':/tmp',
            'ghcr.io/k4yt3x/video2x:5.0.0-beta6',
            '-i', '/host/' + shortFile,
            '-o', '/host/' + output,
            '-p' + Processes, 
            'upscale',
            '-h', '' + Height,
            '-a', 'waifu2x',
            '-n3'
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
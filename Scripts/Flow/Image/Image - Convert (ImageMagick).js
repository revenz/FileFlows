/**
 * @author John Andrews
 * @uid 61aa25f3-74ed-44f6-a097-89149aea62c3
 * @description Converts images using ImageMagick
 * @revision 4
 * @param {int} Quality The quality of the image, between 1 and 100
 * @param {string} Extension The extension of the converted image
 * @output Image was converted
 */
 function Script(Quality, Extension)
 {
    if(!Extension)
        Extension = 'jpg';
    let output = Flow.TempPath + '/' + Flow.NewGuid() + '.' + Extension;

    let process = Flow.Execute({
        command: 'convert',
        argumentList: [
            Flow.WorkingFile,
            '-quality',
            '' + Quality,
            output,
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

    Flow.SetWorkingFile(output);
    return 1;
 }
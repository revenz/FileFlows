/**
 * Important: This script was made to be ran with the docker version of FileFlows. Results on other platforms may vary.
 * Prerequisites:
 *  Video - Devedse - InstallFfmpegBtbN.js
 *  Video - Devedse - InstallAbAv1.js
 * Executes the ab-av1 command.
 * @author Devedse
 * @revision 2
 * @minimumVersion 1.0.0.0
 * @param {int} Preset The preset to use
 * @param {string} SvtArguments The --svt arguments to pass to AbAv1
 * @param {string} PixFormat The --pix-format argument to pass to AbAv1
 * @param {string} MinVmaf The VMAF value to go for (e.g. 95.0)
 * @param {bool} Thorough Keep searching until a crf is found no more than min_vmaf+0.05 or all possibilities have been attempted.
 * @output The command succeeded
 * @output Variables.AbAv1CRFValue: The detected CRF value
 */
function Script(Preset,SvtArguments,PixFormat,MinVmaf,Thorough)
{

    let targetDirectory = '/app/Data/tools/ab-av1/';

    let ffmpegBtbnDirectory = Variables.FfmpegBtbn;
    if(!ffmpegBtbnDirectory){
        Logger.ELog('Variables.FfmpegBtbn was not set. Ensure the InstallFfmpegBtbN script was run before this script.');
        return -1;
    }
    Logger.ILog('FfmpegBtbn directory: ' + ffmpegBtbnDirectory);

    // Get the input file path from Flow.WorkingFile
    var fi = FileInfo(Flow.WorkingFile);

    // Check if ab-av1 exists
    let abAv1Path = targetDirectory + 'ab-av1';
    let checkAbAv1 = Flow.Execute({
        command: 'ls',
        argumentList: [abAv1Path]
    });
    if(checkAbAv1.exitCode !== 0){
        Logger.ELog('ab-av1 not found at ' + abAv1Path);
        return -1;
    }

    let abav1Command = `${abAv1Path} crf-search --svt ${SvtArguments} --pix-format ${PixFormat} -i '${fi.FullName}' --min-vmaf ${MinVmaf} --preset ${Preset}`
    if (Thorough == true) {
        abav1Command += ' --thorough'
    }

    let executeAbAv1 = Flow.Execute({
    command: 'sh',
    argumentList: [
            '-c',
            `PATH=${Variables.FfmpegBtbn}:$PATH ` + 
            abav1Command
        ]
    });

    if(executeAbAv1.exitCode !== 0){
        Logger.ELog('Failed to execute ab-av1: ' + executeAbAv1.exitCode);
        return -1;
    }

    Logger.ILog('ab-av1 executed successfully.');


    // Parse the output to find the CRF value
    let output = executeAbAv1.output;
    let crfValueMatch = output.match(/crf (\d+) VMAF.*predicted video stream size/);
    if (crfValueMatch && crfValueMatch.length > 1) {
        let crfValue = crfValueMatch[1];
        Logger.ILog('Detected CRF value vased on VMAF: ' + crfValue);

        Logger.ILog('Set Variables.AbAv1CRFValue to ' + crfValue);
        // Set the CRF value for use in another node
        Variables.AbAv1CRFValue = crfValue;
        return 1;
    } else {
        Logger.ELog('CRF value not found in output');
        return 0;
    }

    return 1;
}
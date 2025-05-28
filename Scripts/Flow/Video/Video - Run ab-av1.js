/**
 * @description Run ab-av1 to find the best CRF of a video for a given VMAF. This is saved to Variables.AbAv1CRFValue if found.
Important: This script was made to be ran with the docker version of FileFlows. Results on other platforms may vary.
 * @help Dependencies (DockerMods): AutoVMAF, FFmpeg7-BtbN
If CRF is found, it is saved to Variables.AbAv1CRFValue.
Executes the ab-av1 command.
 * @author CanofSocks
 * @uid e31fbd4d-dc96-4ae6-9122-a9f30c102b1d
 * @revision 1
 * @param {string} Preset The preset to use
 * @param {string} Encoder The target encoder
 * @param {string} EncOptions A '|' separated list of additional options to pass to ab-av1. The first '=' symbol will be used to infer that this is an option with a value. Passed to ffmpeg like "x265-params=lossless=1" -> ['-x265-params', 'lossless=1'] 
 * @param {string} PixFormat The --pix-format argument to pass to AbAv1
 * @param {string} MinVmaf The VMAF value to go for (e.g. 95.0)
 * @param {int} MaxEncodedPercent Maximum percentage of predicted size
 * @param {string} MinCRF The minimum CRF
 * @param {string} MaxCRF The maximum CRF
 * @param {int} MinSamples Minimum number of samples per video
 * @param {bool} Thorough Keep searching until a crf is found no more than min_vmaf+0.05 or all possibilities have been attempted.
 * @param {string} AdditionalOptions (Advanced) Additional ab-av1 options to pass.
 * @output The command succeeded
 * @output Variables.AbAv1CRFValue: The detected CRF value
 */
function Script(Preset,Encoder,EncOptions,PixFormat,MinVmaf,MaxEncodedPercent,MinCRF,MaxCRF,MinSamples,Thorough,AdditionalOptions)
{

    if (!ToolPath("ab-av1", "/opt/autocrf")) {
        return -1;
    }
    if (!ToolPath("ffmpeg", "/opt/autocrf")) {
        return -1;
    }
    if (!ToolPath("ffmpeg", "/opt/ffmpeg-static/bin")) {
        return -1;
    }

    // Get the input file path from Flow.WorkingFile
    var fi = FileInfo(Flow.WorkingFile);

    let abav1Command = ` crf-search --temp-dir ${Variables.temp} -i "${fi.FullName}"`
    if(PixFormat){
        abav1Command += ` --pix-format ${PixFormat}`
    }
    if(MinVmaf){
        abav1Command += ` --min-vmaf ${MinVmaf}`
    }
    if(Preset){
        abav1Command += ` --preset ${Preset}`
    }
    if(MaxEncodedPercent){
        abav1Command += ` --max-encoded-percent ${MaxEncodedPercent}`
    }
    if(MinSamples){
        abav1Command += ` --min-samples ${MinSamples}`
    }
    if(String(Encoder).trim().length > 0){
        abav1Command += ` --encoder ${Encoder}`
    }
    if(String(MinCRF).trim().length > 0){
        abav1Command += ` --min-crf ${MinCRF}`
    }
    if(String(MaxCRF).trim().length > 0){
        abav1Command += ` --max-crf ${MaxCRF}`
    }
    if (String(Thorough) == true) {
        abav1Command += ' --thorough'
    }
    if (String(EncOptions).trim().length > 0){
        let encoptions = String(EncOptions).trim().split("|");
        for (let i = 0; i < encoptions.length; i++) {
            abav1Command += ` --enc ${encoptions[i]}`
        }
    }

    if(String(AdditionalOptions).trim().length > 0){
        abav1Command += ` ${AdditionalOptions}`
    }

    let returnValue = search(abav1Command);

    Logger.ILog(returnValue.message)

    if (returnValue.error == true){
        Logger.ELog(`${returnValue.message}`)
        Flow.fail(`AutoCRF: ${returnValue.message}`);
        return -1
    }

    if (returnValue.winner){
        Variables.AbAv1CRFValue = returnValue.winner.crf
        Logger.ILog(`Set CRF value to ${Variables.AbAv1CRFValue}`)
        return 2        
    } else {
        return 1
    }
    return 1
}

// Stolen from lawrence
function search(abav1Command){

    let abAv1 = ToolPath("ab-av1", "/opt/autocrf");
    let path = abAv1.replace(/[^\/]+$/, "");
    abav1Command = `${abAv1} ${abav1Command}`
    var executeArgs = new ExecuteArgs();
    executeArgs.command = 'sh';
    executeArgs.argumentList = [
                '-c',
                `PATH=${path}:$PATH ` + 
                abav1Command
            ];
    let returnValue = {
        data: [
            // { crf: 12, score: 12.34, size: 90 }
        ],
        error: false,
        winner: null,
        command: `${abav1Command}`,
        message: "",
    };

    executeArgs.add_Error((line) => {
        line = line.substring(line.indexOf(" ") + 1);
        let matches = "";

        if (
            (matches = line.match(
                /encoding sample ([0-9]+)\/([0-9]+).* crf ([0-9]+)/i
            ))
        ) {
            // crf = matches[0];
            Flow.AdditionalInfoRecorder("Sampling", `CRF ${matches[3]}`, 1);
            Flow.PartPercentageUpdate((100 / matches[2]) * matches[1]);
        }
        if (
            (matches = line.match(
                /eta ([0-9]+) (seconds|minutes|hours|days|weeks)/i
            ))
        ) {
            Flow.AdditionalInfoRecorder(
                "ETA",
                `${matches[1]} ${matches[2]}`,
                1
            );
        }
        if (
            (matches = line.match(
                /(crf ([0-9]+) )?VMAF ([0-9.]+) predicted.*\(([0-9.]+)%/i
            ))
        ) {
            returnValue.data.push({
                crf: matches[2].trim(),
                score: matches[3].trim(),
                size: matches[4].trim(),
            });
        }
        if ((matches = line.match(/crf ([0-9]+) successful/i))) {
            for (const line of returnValue.data) {
                if (line.crf == matches[1]) {
                    returnValue.winner = line;
                }
            }
        }
    });

    let executeAbAv1 = Flow.Execute(executeArgs);

    if (executeAbAv1.exitCode !== 0) {
        if (executeAbAv1.output.includes("Failed to find a suitable crf")) {
            returnValue.message = "Failed to find a suitable crf";
        } else {
            Logger.WLog(executeAbAv1.output);
            returnValue.message = "Failed to execute ab-av1: " + executeAbAv1.standardError;
            returnValue.error = true;
        }
    }

    table(returnValue);

    return returnValue;
}

function ToolPath(tool, path) {
    if (Flow.IsDocker) {
        if (System.IO.File.Exists(`${path}/${tool}`)) {
            return `${path}/${tool}`;
        } else {
            Logger.ELog("You may need to remove the FFmpeg6 DockerMod");
            Logger.WLog(`Cannot find ${path}/${tool}`);
            Logger.WLog(
                `AutoCRF: If you're absolutely sure you installed the DockerMods please restart the node`
            );
            Flow.Fail(
                `AutoCRF: Missing required DockerMods: AutoCRF, FFmpeg7, FFmpeg7-BtbN`
            );
            return null;
        }
    }

    let toolPath = Flow.GetToolPath(tool);

    if (toolPath) return toolPath;

    Flow.Fail(
        `${tool} cannot be found! Please create a Variable called "${tool}" that points too the correct location, please see ffmpeg as an example`
    );
}

function table(output) {
    Logger.ILog(" ");
    Logger.ILog("| CRF | Score | Size |");
    Logger.ILog("----------------------");
    for (const line of output.data) {
        let crf = line.crf.toString().padStart(3);
        let score = line.score.toString().padStart(5);
        let size = line.size.toString().padStart(3);
        Logger.ILog(`| ${crf} | ${score} | ${size}% |`);
    }

    if (output.winner) {
        let crf = output.winner.crf.toString().padStart(3);
        let score = output.winner.score.toString().padStart(5);
        let size = output.winner.size.toString().padStart(3);
        Logger.ILog("----------------------");
        Logger.ILog(`| ${crf} | ${score} | ${size}% |`);
    }
    Logger.ILog(" ");
}

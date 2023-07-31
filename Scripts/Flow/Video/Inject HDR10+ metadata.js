/**
 * Injects The Hdr10+ Metadata from the original file into the current working file
 * @author https://github.com/GrimJu
 * @version 1.0.0
 * @output Success, metadata was injected and new working file was created 
 * @output Fail
 */
function Script()
{
    let hdr10plus_tool_path = Flow.GetToolPath('hdr10plus_tool');
    if( !hdr10plus_tool_path){
        Logger.ELog("No hdr10plus_tool found");
        return 2;
    }

    let ffmpeg = Flow.GetToolPath('ffmpeg');
    let process = Flow.Execute({
        command: ffmpeg,
        argumentList: [
            '-i',
            Variables.file.Orig.FullName,
            '-map',
            '0:v:0',
            '-c',
            'copy',
            "-nostdin",
            "-y",
            "originalHEVCStream.hevc"
        ]
    });

    processLogs(process);


    process = Flow.Execute({
        command: hdr10plus_tool_path,
        argumentList:[
            "extract", "-i","originalHEVCStream.hevc","-o","metadata.json"
        ]
    })
    processLogs(process);

    process = Flow.Execute({
        command: ffmpeg,
        argumentList: [
            '-i',
            Variables.file.FullName,
            '-map',
            '0:v:0',
            '-c',
            'copy',
            "-nostdin",
            "-y",
            "newHEVCStream.hevc"
        ]
    });
    processLogs(process);

    process = Flow.Execute({
        command: hdr10plus_tool_path,
        argumentList:[
            "inject","-j","metadata.json", "-i","newHEVCStream.hevc","-o","outputHEVCStream.hevc"
        ]
    })
    processLogs(process);

    process = Flow.Execute({
        command: ffmpeg,
        argumentList: [
            "-i",
            "outputHEVCStream.hevc",
            '-i',
            Variables.file.FullName,
            '-map',
            '0:v:0',
            "-map",
            "1",
            "-map", 
            "-1:v:0",
            '-c',
            'copy',
            "-nostdin",
            "-y",
            `output.${Variables.file.Extension}`
        ]
    });
    processLogs(process);

    Flow.SetWorkingFile(`output.${Variables.file.Extension}`)
    return 1;
} 

function processLogs(process){
    if(process.standardOutput)
        Logger.ILog('Standard output: ' + process.standardOutput);
    if(process.standardError)
        Logger.ILog('Standard error: ' + process.standardError);

    if(process.exitCode !== 0){
        Logger.ELog('Failed processing: ' + process.exitCode);
        return 2;
    }
}

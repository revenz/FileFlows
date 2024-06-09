/**
 * @author GrimJu
 * @uid 594f7f1e-2a8e-4858-b83b-38d971bf48d1
 * @revision 5
 * @description Injects The Hdr10+ Metadata from the original file into the current working file
 * @param {bool} FailFlowOnNoInject If set to true the flow will fail when the script fails to inject any metadata
 * @output Success, metadata was injected and new working file was created 
 * @output Fail
 */
function Script(FailFlowOnNoInject) {
    let hdr10plus_tool_path = Flow.GetToolPath('hdr10plus_tool');
    let processOutcome;
    if (!hdr10plus_tool_path) {
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

    processOutcome = processLogs(process, FailFlowOnNoInject);
    if(processOutcome !== 0){
        return processOutcome;
    }
    


    process = Flow.Execute({
        command: hdr10plus_tool_path,
        argumentList: [
            "extract", "-i", "originalHEVCStream.hevc", "-o", "metadata.json"
        ]
    })

    processOutcome = processLogs(process, FailFlowOnNoInject);
    if(processOutcome !== 0){
        return processOutcome;
    }

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
    
    processOutcome = processLogs(process, FailFlowOnNoInject);
    if(processOutcome !== 0){
        return processOutcome;
    }

    process = Flow.Execute({
        command: hdr10plus_tool_path,
        argumentList: [
            "inject", "-j", "metadata.json", "-i", "newHEVCStream.hevc", "-o", "outputHEVCStream.hevc"
        ]
    })
    
    processOutcome = processLogs(process, FailFlowOnNoInject);
    if(processOutcome !== 0){
        return processOutcome;
    }

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
    
    processOutcome = processLogs(process, FailFlowOnNoInject);
    if(processOutcome !== 0){
        return processOutcome;
    }

    Flow.SetWorkingFile(`output.${Variables.file.Extension}`)
    return 1;
}

function processLogs(process, FailFlowOnNoInject) {
    //Logger.ILog("Exit code: " + process.exitCode)
    if (process.exitCode !== 0) {
        Logger.ELog('Failed with errorcode: ' + process.exitCode);
        if (FailFlowOnNoInject) {
            Logger.ILog("Failing flow")
            return -1;
        }
        else {
            Logger.ILog("Continuing flow execution")
            return 2;
        }
    }   
    
    if (process.standardError) {
        Logger.ELog('Standard error: ' + process.standardError);
        if (FailFlowOnNoInject) {
            return -1;
        }
        else {
            return 2;
        }
    }
    
    if (process.standardOutput) {
        //catch errors in stdout
        if (`${process.standardOutput}`.toLowerCase().includes("error") && FailFlowOnNoInject && process.exitCode === 0) {
            return -1;
        }
        else if(/error/i.test(process.standardOutput) === true && process.exitCode === 0){
            return 2;
        }
        else{
            return 0;
        }
    }

}

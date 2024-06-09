/**
 * @author Luigi311
 * @uid deabeae0-5233-4a8a-8663-893386695abc
 * @description Calculate vmaf score of working file compared to original file
 * @revision 4
 * @param {int} n_threads Amount of threads to use for calculation, 0 for auto (non windows only)
 * @param {string} json_file Json file containing vmaf information, empty for ${Flow.TempPath}/${file.NameNoExtension}.json
 * @output VMAF succeeded Variables.VMAF
 */
function Script(n_threads, json_file)
{
    if(!n_threads || n_threads === 0) {
        // Calculate amount of threads to use for vmaf
        let thread_process = Flow.Execute({
            command: 'nproc',
            argumentList: []
        });

        let threads;
        if(thread_process.standardOutput) {
            threads = thread_process.standardOutput
            Logger.ILog('threads: ' + threads);
        }
        if(thread_process.starndardError)
            Logger.ILog('nproc error: ' + thread_process.starndardError);

        if(thread_process.exitCode !== 0){
            Logger.ELog('Failed to get threads: ' + thread_process.exitCode);
            return -1;
        }

        // Max 12 threads due to poor vmaf scaling
        n_threads = Math.min(threads, 12);
        Logger.ILog(`Setting n_threads to ${n_threads}`);
    }

    // Set the default json file to a random guid to avoid issues with file names 
    // and move it to the correct name at the end
    let temp_json_file = `${Flow.TempPath}/${Flow.NewGuid()}.json`;

    let ffmpeg = Flow.GetToolPath('ffmpeg');
    let vmaf_process = Flow.Execute({
        command: ffmpeg,
        argumentList: [
            '-hide_banner',
            '-y',
            '-thread_queue_size',
            '1024',
            '-r',
            '60',
            '-i',
            Variables.file.FullName,
            '-r',
            '60',
            '-i',
            Variables.file.Orig.FullName,
            '-lavfi',
            `libvmaf=n_threads=${n_threads}:log_fmt=json:log_path='${temp_json_file}'`,
            '-f',
            'null',
            '-'
        ]
    });

    if(vmaf_process.standardOutput)
        Logger.ILog('Standard output: ' + vmaf_process.standardOutput);
    if(vmaf_process.starndardError)
        Logger.ILog('Standard error: ' + vmaf_process.starndardError);

    if(vmaf_process.exitCode !== 0){
        Logger.ELog('Failed processing vmaf: ' + vmaf_process.exitCode);
        return -1;
    }

    // Parse vmaf score out of ffmpeg output
    let vmaf_match = vmaf_process.output.match(/VMAF score: (\d+\.?\d*)/);
    if(!vmaf_match){
        Logger.ILog("VMAF match failed");
        return -1;
    }
    Logger.ILog("Vmaf Match: " + vmaf_match);

    let vmaf = vmaf_match.join().split(",")[1].trim();
    if(!vmaf) {
        Logger.ILog("VMAF match failed");
        return -1;
    }

    Logger.ILog("VMAF Score: " + vmaf);
    Variables.VMAF = parseFloat(vmaf);

    if(!json_file || String(json_file).trim().length === 0) {
        Logger.ILog(`Setting json_file to ${Flow.TempPath}/${Variables.file.NameNoExtension}.json`)
        json_file = `${Flow.TempPath}/${Variables.file.NameNoExtension}.json`;
    }

    System.IO.File.Move(temp_json_file, json_file);

    return 1;
}
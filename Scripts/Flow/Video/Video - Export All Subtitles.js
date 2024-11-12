/**
 * @author reven
 * @uid fc30c661-4901-4474-8487-619a6257494b
 * @revision 10
 * @description Exports all subtitles from a video file
 * @param {string} FileName Optional full filename of the video to extract subtitles from, if not passed in the current working file will be used
 * @output Subtitles were exported
 * @output No subtitles found to export
 */
 function Script(FileName)
 {
    let vi = null;
    if(('' + FileName))
    {
        Logger.ILog(`Executing PluginMethod: VideoNodes.GetVideoInfo('${FileName}')`);
        vi = PluginMethod("VideoNodes", "GetVideoInfo", [FileName]);
    }
    else
    {
        FileName = Variables.file.FullName;   
        vi = Variables.vi?.VideoInfo;
    }

    if(!vi)
    {
        Logger.WLog("No video information found");
        return 2;
    }

    if(!vi.SubtitleStreams?.length)
    {
        Logger.WLog("No subtitle streams found");
        return 2;
    }

    let ffmpeg = Flow.GetToolPath('ffmpeg');
    if(!ffmpeg)
    {
        Logger.ILog('FFmpeg not found');
        return -1;
    }

    // this sets the extract location to the the working directory
    var path = new System.IO.FileInfo(Variables.file.FullName).Directory.FullName;
    var fileName = new System.IO.FileInfo(FileName).Name;
    let prefix = fileName.substring(0, fileName.lastIndexOf('.') + 1);
    prefix = System.IO.Path.Combine(path, prefix);

    let extracted = 0;

    let imageSubtitles = /dvbsub|dvdsub|pgs|xsub/i;

    for(let i = 0; i < vi.SubtitleStreams.length; i++){
        let sub = vi.SubtitleStreams[i];

        let isImage = imageSubtitles.test(sub.Codec);

        let subfile = prefix + (!!sub.Language ? sub.Language + '.' : '') + (i == 0 ? '' : i + '.') + (isImage ? 'sup' : 'srt');

        if(System.IO.File.Exists(subfile))
        {
            Logger.ILog('Subtitle file already exists skipping: ' + subfile);
            continue;
        }
        Logger.ILog('Extracting subtitle: ' + subfile);

        let argsList = ['-hide_banner', '-i', FileName];
        argsList.push('-map');
        argsList.push('0:s:' + i);
        if(isImage){
            argsList.push('-c:s');
            argsList.push('copy');
        }
        argsList.push('-strict');
        argsList.push('2')
        argsList.push(subfile);
        let process = Flow.Execute({
            command: ffmpeg,
            argumentList: argsList
        });
        if(process.exitCode !== 0){
            Logger.WLog(`Failed to extract subtitle ${i}: ${process.output}`);
            continue;
        }
        Logger.ILog(`Extracted subtitle ${i}: ${subfile}`);
        ++extracted;
    }

    if(extracted === 0){
        Logger.ILog('Did not extract any subtitles');
        return 2;
    }   


    Logger.ILog(`Extracted ${extracted} subtitle${extracted === 1 ? '' : 's'}`);
    return 1;
 }
/**
 * Exports all subtitles from a video file
 * @author John Andrews
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @output Subtitles were exported
 * @output No subtitles found to export
 */
 function Script()
 {
    let vi = Variables.vi?.VideoInfo;
    if(!vi || !vi.SubtitleStreams?.length)
      return 2; // no video information found

    let ffmpeg = Flow.GetToolPath('ffmpeg');
    if(!ffmpeg)
    {
        Logger.ILog('FFMPEG not found');
        return -1;
    }

    let prefix = Variables.file.FullName.substring(0, Variables.file.FullName.lastIndexOf('.') + 1);

    let extracted = 0;

    let imageSubtitles = /dvbsub|dvdsub|pgs|xsub/i;

    for(let i = 0; i < vi.SubtitleStreams.length; i++){
        let sub = vi.SubtitleStreams[i];

        let isImage = imageSubtitles.test(sub.Codec);

        let subfile = prefix + (!!sub.Language ? sub.Language + '.' : '') + (i == 0 ? '' : i + '.') + (isImage ? 'sup' : 'srt');

        let argsList = ['-hide_banner', '-i', Variables.file.FullName];
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
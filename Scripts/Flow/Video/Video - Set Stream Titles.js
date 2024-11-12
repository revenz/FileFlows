/**
 * @author reven
 * @uid 2b38c39e-8536-4df8-9878-684e3f02fed8
 * @description Updates audio and or subtitle titles to format `Language / Codec / Bitrate / SampleRate` or `Language / Forced / Codec`
 * @revision 6
 * @param {bool} Audio If audio stream titles should be updated
 * @param {bool} Subtitle If subtitle stream titles should be updated
 * @param {bool} LeaveCommentaryAlone If commentary streams should be left alone and not renamed
 * @output Titles were changed
 * @output No changes were made
 */
function Script(Audio, Subtitles, LeaveCommentaryAlone)
{
    let forceChange = false;

    let audioLength = Variables?.FfmpegBuilderModel?.AudioStreams?.length;
    let subtitleLength = Variables?.FfmpegBuilderModel?.SubtitleStreams?.length;

    if(Audio && audioLength)
    {
        Logger.ILog('Found audio streams: ' + audioLength);
        for(let i=0;i<audioLength;i++)
        {
            let as = Variables.FfmpegBuilderModel.AudioStreams[i];
            if(!as)
                continue; // error handling

            if(LeaveCommentaryAlone && as.Title && /commentary/i.test(as.Title))
                continue;

            let params = [];
            if(as.Language)
            {
                let lang = LanguageHelper.GetEnglishFor(as.Language) || as.Language;
                params.push(lang);
            }

            if(as.Codec)
                params.push(as.Codec.toUpperCase());

            if(as.Channels)
            {
                let channels = parseFloat(as.Channels.toFixed(1), 10);
                switch(channels)
                {
                    case 1:
                        params.push('Mono');
                        break;
                    case 2:
                        params.push('Stereo');
                        break;
                    default:
                        params.push(channels);
                        break;
                }
            }
            let bitrate = as.Bitrate || as.Stream?.Bitrate;
            if(bitrate)
                params.push(bitrate / 1000 + ' kbps');
            let sampleRate = as.SampleRate || as.Stream?.SampleRate;
            if(sampleRate && sampleRate > 0)
                params.push(sampleRate / 1000 + 'kHz');

            let title = params.join(' / ');
            as.Title = title;
            as.Stream.Title = title;
            forceChange = true;
        }
    }


    if(Subtitles && subtitleLength)
    {
        Logger.ILog('Found subtitle streams: ' + subtitleLength);
        for(let i=0;i<subtitleLength;i++)
        {
            let sub = Variables.FfmpegBuilderModel.SubtitleStreams[i];
            if(!sub)
                continue; // no title already

            let params = [];
            if(sub.Language)
            {
                let lang = LanguageHelper.GetEnglishFor(sub.Language) || sub.Language;
                params.push(lang);
            }

            if(sub.Forced || sub.Stream.Forced)
                params.push('Forced');

            if(sub.Codec)
                params.push(sub.Codec.toUpperCase());
                
            let title = params.join(' / ');
            sub.Title = title;
            sub.Stream.Title = title;
            forceChange = true;
        }
    }


    if(!forceChange)
        return 2;

    Variables.FfmpegBuilderModel.ForceEncode = true;
    return 1;
}
/**
 * @author CanOfSocks
 * @uid f25d9fc6-adfa-4c53-bf03-cc8fd6a98e9c
 * @description Sets the bitrate for audio streams based off the number of channels - in place of "Quality" options found on some audio codecs
 * @revision 3
 * @param {int} Bitrate Desired bitrate per channel (default: 64000)
 * @param {string} Codec Desired codec for each channel (default: ac3)
 * @param {int} Buffer Buffer above desired bitrate to not transcode (default: 0)
 * @param {string} Channels Default mix/number of audio channels if not found (default: 2)
 * @output Changes made to audio
 * @output No changes made to audio
 */
function Script(Bitrate, Codec, Buffer, Channels){

    //Sanatise inputs
    let codec = "ac3"
    if(Codec){
        codec = `${Codec}`
    }
    let desiredBitrate = 64000;
    if(Bitrate){
        desiredBitrate = Number(Bitrate);
    }
    let acceptedDifference = 0;
    if(Buffer){
        acceptedDifference = Number(Buffer);
    }
    if(Channels && !isNaN(Channels)){   //Checks if channel is number mixdown e.g. 2, 5.1, 7.1 etc, sanatises if it is
        Channels = Number(Channels);
    } else if(Channels && /mono/i.test(`${Channels}`) === true){    //mono is only option that is not a default for a word input as stereo matches default of 2
        Channels = 1;
    } else {
        Channels = 2;
    }

    let change = false; //assume no changes needed

    for(let i=0;i<Variables?.FfmpegBuilderModel?.AudioStreams?.length;i++)
    {
        let builder = Variables.FfmpegBuilderModel.AudioStreams[i];
        let as = Variables.vi?.VideoInfo?.AudioStreams[i];

        if(builder.Deleted){    //ignore if already deleted
            continue;
        }
        let currentBitrate = as.Bitrate;
        if(currentBitrate === undefined) 
            currentBitrate = 0;

        //let channels = as.Channels;
        let channels = Math.ceil(as.Channels*10)/10;    //rounds channels up to 1 dp - primarily for readability of logs

        //If channels is 0, then no information is available, set to default. Assumes 0 if channels are less than mono (somehow)
        if(channels === 0 || channels < 1){
            channels = Channels;
        }

        //Full channels sets to ffmpeg equivalent channels for -ac, usually rounds up
        let fullChannels = 2;
        if(channels > 7){ 
            fullChannels = 8;
        } else if (channels > 5){
            fullChannels = 6;
        } else if(channels >=2){
            fullChannels = 2;
        } else if(channels >= 1){
            fullChannels = 1;
        }
        

        let perChannel = (currentBitrate/fullChannels)
        let newBitrate = 0;
        Logger.ILog(`Current total bitrate for audio track ${builder.Stream.TypeIndex}: ${currentBitrate}`);
        Logger.ILog(`Current channels for audio track ${builder.Stream.TypeIndex}: ${channels}`);
        Logger.ILog(`Current bitrate per channel for audio track ${builder.Stream.TypeIndex}: ${perChannel}`);
        
        if((currentBitrate/fullChannels) >= (desiredBitrate+acceptedDifference) || (currentBitrate/fullChannels) === 0){
            newBitrate = desiredBitrate * fullChannels;
            
            Logger.ILog(`New total bitrate: ${newBitrate}`);
            let params = [
                /*"-c:a:{index}",*/ codec, 
                "-b:a:{index}", newBitrate,
                "-ac:a:{index}", fullChannels
            ];            
            
            for (let j = 0; j < params.length; j++) {
                builder.EncodingParameters.Add(params[j]);
            }
            
            change = true;
        }
        
    }
    if(change){
        return 1;
    }else{
        return 2;
    }
}

/**
 * Checks if a video files bitrate is greater than a specific bitrate
 * @revision 1
 * @outputs 2
 * @minimumVersion 24.08.1.3441
 */

// check if the bitrate for a video is over a certain amount
var MAX_BITRATE = 3_000_000; // bitrate is 3,000 KBps

if(Variables.TryGetValue("vi.VideoInfo", out var oVideoInfo) == false || oVideoInfo is FileFlows.VideoNodes.VideoInfo videoInfo == false)
{
    Logger.ILog("Failed to locate VideoInformation in variables");
    return -1;
}
Logger.ILog("Got video information.");

var video = videoInfo.VideoStreams.FirstOrDefault();
if(video == null)
{
    Logger.ILog("No video streams detected.");
    return -1;
}

// get the video stream
var bitrate = video.Bitrate;

if(bitrate < 1)
{
	// video stream doesn't have bitrate information
	// need to use the overall bitrate
	var overall = videoInfo.Bitrate;
	if(overall < 1)
		return 0; // couldn't get overall bitrate either

	// overall bitrate includes all audio streams, so we try and subtract those
	var calculated = overall;
	if(videoInfo.AudioStreams.Count > 0) // check there are audio streams
	{
		foreach(var audio in videoInfo.AudioStreams)
		{
			if(audio.Bitrate > 0)
				calculated -= audio.Bitrate;
			else{
				// audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
				// this is a guess, but it should get us close
				calculated -= (overall * 0.05f);
			}
		}
	}
	bitrate = calculated;
}

// check if the bitrate is over the maximum bitrate
if(bitrate > MAX_BITRATE)
	return 1; // it is, so call output 1
return 2; // it isn't so call output 2
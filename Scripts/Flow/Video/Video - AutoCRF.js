/**
 * @name Video - AutoCRF
 * @uid d08608d8-3b50-4784-ad68-fd4ed102577c
 * @description Finds the correct CRF using VMAF score based on a
maximum BitRate and your selected codec types
 * @help Put me between 'FFMPEG Builder: Executor' and 'FFMPEG Builder: Executor'
Required DockerMods: AutoCRF, FFmpeg7, FFmpeg7-BtbN

This Flow Element two outputs:
1: Connect to 'FFmpeg Builder Video Manual' with the 'Parameters' set to...
   {ManualParameters}
2: The video is in an acceptable codec and bitrate and should *not* be encoded

This script will always try and convert the video to the target codec by trying to calculate a CRF for a smaller file.
In the event of it not finding a suitable CRF and the codec isn't in FallBackCodecs it will do BitRate encoding automatically

Recommended defaults:
    FallBackCodecs: hevc, h.264
    MaxBitRate: 11.5 MBps for SDR and 23.5 MBps Dolby Vison

All parameters can also be overridden using Variables for example
    TargetCodec = hevc_nvenc
    MaxBitRate = 12
    FallBackCodecs = hevc|h.264|mpeg4|custom
    SVT = lookahead=64:film-grain=8
    KeyInt = 240
 * @author Lawrence Curtis
 * @revision 2
 * @param {('hevc_qsv'|'hevc'|'av1_qsv'|'libsvtav1')} TargetCodec Which codec you want as the output
 * @param {('hevc'|'h264'|'av1'|'vp9'|'mpeg2'|'mpeg4')[]} FallBackCodecs Video codecs that you are happy to keep if no CRf can be found
 * @param {int} MaxBitRate The maximum acceptable bitrate in MBps
 * @param {bool} FixDolby5 Create a SDR fallback for Dolby Vision profile 5 (aka the green/purple one) [CPU decode]
 * @param {bool} UseTags Create tags (premium feature) such as "Copy", "CRF 17", "Fallback"
 * @param {bool} ErrorOnFail Error on CRF detection fail rather than fallback
 * @param {bool} TestMode This doesn't calculate you a score, instead just tells you if it would need too
 * @output CRF found - recommend video manual
 * @output The command succeeded
 */
    function Script() {
        // Checking dependencies
        if (!ToolPath("ab-av1", "/opt/autocrf")) {
            return -1;
        }
        if (!ToolPath("ffmpeg", "/opt/autocrf")) {
            return -1;
        }
        if (!ToolPath("ffmpeg", "/opt/ffmpeg-static/bin")) {
            return -1;
        }
        if (!ToolPath("ffmpeg", "/usr/local/bin")) {
            return -1;
        }
    
        if (Variables.FixDolby5) {
            FixDolby5 = Variables.FixDolby5;
        }
    
        if (Variables.UseTags) {
            UseTags = Variables.UseTags;
        }
    
        if (Variables.ErrorOnFail) {
            ErrorOnFail = Variables.ErrorOnFail;
        }
    
        if (Variables.TargetCodec) {
            TargetCodec = Variables.TargetCodec;
        }
    
        if (!TargetCodec) {
            TargetCodec = 'hevc';
        }
    
        // FallBackCodecs
        if (Variables.FallBackCodecs) {
            FallBackCodecs = Variables.FallBackCodecs.split("|");
        }
        FallBackCodecs = FallBackCodecs.map((name) => name.toLowerCase());
    
        // MaxBitRate
        if (Variables.MaxBitRate) {
            MaxBitRate = Variables.MaxBitRate;
        }
        if (!MaxBitRate) {
            Flow.Fail(
                "AutoCRF: Please set the Max Bit Rate or Variable MaxBitRate"
            );
            return -1;
        }
        if (MaxBitRate > 50) {
            Flow.Fail("AutoCRF: MaxBitRate should be set in MBps e.g. 12");
            return -1;
        }
    
        let targetBitRate = MaxBitRate * 1024 * 1024;
    
        // Video info
        let video = Variables.FfmpegBuilderModel?.VideoStreams[0];
        if (!video) {
            Flow.Fail(
                "AutoCRF: Cannot find video information, please check for FFmpeg Builder Start"
            );
            return -1;
        }
        let videoCodec = video.Stream.Codec;
        // let videoBitRate = Variables.vi.VideoInfo.Bitrate;
    
        let videoBitRate = Variables.vi.VideoInfo.VideoStreams[0].Bitrate;
    
        if (!videoBitRate) {
            Logger.WLog("FileFlows has no bitrate info about the video, calculating...")
            // video stream doesn't have bitrate information
            // need to use the overall bitrate
            let overall = Variables.vi.VideoInfo.Bitrate;
            if (!overall)
                return 0; // couldn't get overall bitrate either
    
            // overall bitrate includes all audio streams, so we try and subtract those
            let calculated = overall;
            if (Variables.vi.VideoInfo.AudioStreams?.length) // check there are audio streams
            {
                for (let audio of Variables.vi.VideoInfo.AudioStreams) {
                    if (audio.Bitrate > 0)
                        calculated -= audio.Bitrate;
                    else {
                        // audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
                        // this is a guess, but it should get us close
                        calculated -= (overall * 0.05);
                    }
                }
            }
            videoBitRate = calculated;
        }
    
        let bitratePercent = Math.floor((100 / videoBitRate) * targetBitRate);
    
        // Video Description
        let videoDescription = `${bytesToHuman(videoBitRate)} ${videoCodec}`;
        let videoColors = [];
        if (video.Stream.HDR) {
            videoColors.push("HDR");
        }
        if (video.Stream.DolbyVision) {
            videoColors.push("DoVi");
        }
        if (videoColors.length) {
            videoDescription = videoDescription.concat(
                " (",
                videoColors.join(" / "),
                ")"
            );
        }
    
        // Okay, Let's go!
        let forceEncode = false;
        let firstTryPercentage = 85;
        let secondTryPercentage = 100;
        let firstTryScore = 97;
        let secondTryScore = 95;
        let preset = 'slow';
    
        if (Variables.FirstTryPercentage) {
            firstTryPercentage = Variables.FirstTryPercentage;
        }
    
        if (Variables.SecondTryPercentage) {
            secondTryPercentage = Variables.SecondTryPercentage;
        }
    
        if (Variables.FirstTryScore) {
            firstTryScore = Variables.FirstTryScore;
        }
    
        if (Variables.SecondTryScore) {
            secondTryScore = Variables.SecondTryScore;
        }
    
        if (Variables.Preset) {
            preset = Variables.Preset;
        }
    
        Logger.ILog(`Video is ${videoDescription}`);
    
        if (video.Stream.DolbyVision && !video.Stream.HDR && FixDolby5) {
            Logger.ILog("Video is DoVi without a fallback, so were creating one");
            forceEncode = true;
            video.filter.Add(
                "tonemapx=tonemap=bt2390:transfer=smpte2084:matrix=bt2020:primaries=bt2020"
            );
        }
    
        // If the bitrate is more than we want then we don't care what the codec is
        if (videoBitRate > targetBitRate) {
            Logger.WLog("Unacceptable bitrate");
            Logger.WLog(
                `Bitrate is ${bytesToHuman(
                    videoBitRate
                )}, higher than ${MaxBitRate} MBps`
            );
            Logger.ILog(`Will fallback to bitrate encoding`);
            forceEncode = true;
    
            if (firstTryPercentage > bitratePercent) {
                firstTryPercentage = bitratePercent;
            }
            secondTryPercentage = bitratePercent;
        } else {
            targetBitRate = videoBitRate;
        }
    
        // The bitrate is good so we check if the codec is already hevc
        if (!forceEncode && TargetCodec.includes(videoCodec)) {
            Logger.ILog(
                `Bitrate (${bytesToHuman(
                    videoBitRate
                )}) and codec (${videoCodec}) are acceptable`
            );
            if (UseTags) {
                Flow.AddTags(["Copy"]);
            }
            return 2;
        }
    
        if (TestMode) {
            return 1;
        }
    
        if (!forceEncode && !FallBackCodecs.includes(videoCodec)) {
            Logger.WLog("Unacceptable codec");
            Logger.WLog(
                `Codec is ${videoCodec} not ${FallBackCodecs.join(", ")}`
            );
            Logger.ILog(`Will fallback to bitrate encoding`);
            forceEncode = true;
        }
    
        Logger.ILog(
            `Targeting ${firstTryPercentage}% size, ${firstTryScore}% VMAF`
        );
        let attempt = search(firstTryPercentage, firstTryScore, videoBitRate, preset);
        if (!attempt.winner) {
            Logger.ILog(
                `First attempt failed retrying with ${secondTryPercentage}% size, ${secondTryScore}% VMAF`
            );
            attempt = search(secondTryPercentage, secondTryScore, videoBitRate, preset);
        }
    
        if (attempt.winner) {
            if (TargetCodec.includes('av1')) {
                Variables.ManualParameters = `${attempt.command} -crf:v ${attempt.winner.crf}`;
            } else {
                Variables.ManualParameters = `${attempt.command} -global_quality:v ${attempt.winner.crf}`;
            }
            Logger.ILog(
                `Attempt successful with ${attempt.winner.size}% size, ${attempt.winner.score}% VMAF`
            );
            Flow.AdditionalInfoRecorder("Score", attempt.winner.score, 1000);
            Flow.AdditionalInfoRecorder("CRF", attempt.winner.crf, 1000);
            if (UseTags) {
                Flow.AddTags(["VMAF", `CRF ${attempt.winner.crf}`]);
            }
            return 1;
        }
    
        if (attempt.error) {
            Logger.ELog(attempt.message);
            Flow.fail(`AutoCRF: ${attempt.message}`);
            return -1;
        }
    
        // fallback
        if (forceEncode) {
            // setup bitrate encode
            Variables.ManualParameters = attempt.command;
            let t = targetBitRate / 1024.00 / 1024.00;
    
            let obs = [
                "-b:v:{index}",
                `${t.toFixed(2)}M`,
                "-minrate",
                `${(t * 0.75).toFixed(2)}M`,
                "-maxrate",
                `${t * 1.25.toFixed(2)}M`,
                "-bufsize",
                `${Math.round(t)}M`,
            ];
            obs.forEach((ob) => {
                video.AdditionalParameters.Add(ob);
            });
            if (UseTags) {
                Flow.AddTags(["Fallback", bytesToHuman(targetBitRate)]);
            }
            Logger.ILog(
                `Falling back to bitrate encoding as video is unacceptable ${bytesToHuman(
                    targetBitRate
                )}`
            );
            Flow.AdditionalInfoRecorder("Score", "Not found", 1000);
            Flow.AdditionalInfoRecorder("CRF", bytesToHuman(targetBitRate), 1000);
            // Falling back bitrate encode as we could not find a suitable CRF
            return 1;
        } else {
            Logger.ILog("Falling back to copy as codec and bitrate are acceptable");
            // Falling back to copy as we could not find a suitable CRF
            if (UseTags) {
                Flow.AddTags(["Copy"]);
            }
            return 2;
        }
    }
    
    function search(bitratePercent, targetPercent, videoBitRate, preset) {
        let abAv1 = ToolPath("ab-av1", "/opt/autocrf");
        let path = abAv1.replace(/[^\/]+$/, "");
    
        let returnValue = {
            data: [
                // { crf: 12, score: 12.34, size: 90 }
            ],
            error: false,
            winner: null,
            command: `${TargetCodec} -preset ${preset}`,
            message: "",
        };
    
        if (Variables.SVT) {
            returnValue.command = `${returnValue.command} -svtav1-params ${Variables.SVT}`
        }
    
        if (Variables.KeyInt) {
            returnValue.command = `${returnValue.command} -g ${Variables.KeyInt}`
        } else {
            returnValue.command = `${returnValue.command} -g ${Math.round(Variables.vi.VideoInfo.VideoStreams[0].FramesPerSecond) * 10}`
        }
    
        if (TargetCodec.includes("qsv")) {
            returnValue.command = `${returnValue.command} -look_ahead 1 -extbrc 1 -look_ahead_depth 40`
        }
    
        let videoPixelFormat = "yuv420p";
    
        if (Variables.vi.VideoInfo.VideoStreams[0].Is10Bit) {
            videoPixelFormat = "yuv420p10le";
            returnValue.command = `${returnValue.command} -pix_fmt:v:0 p010le`;
        }
    
        let targetBitRate = (bitratePercent / 100) * videoBitRate;
    
        Logger.ILog(
            `Searching for CRF under ${bytesToHuman(
                targetBitRate
            )} @ ${targetPercent}% original quality`
        );
    
        let fileName = Flow.FileService.GetLocalPath(Variables.file.FullName).Value
    
        if (Flow.IsLinux) {
            fileName = `"${fileName}"`;
        }
    
        var executeArgs = new ExecuteArgs();
        executeArgs.command = abAv1;
        executeArgs.argumentList = [
            "crf-search",
            "-i",
            fileName,
            "--preset",
            preset,
            "-e",
            TargetCodec,
            "--temp-dir",
            Flow.TempPath,
            "--min-vmaf",
            targetPercent,
            "--max-encoded-percent",
            bitratePercent,
            "--pix-format",
            videoPixelFormat,
            "--min-crf",
            "5",
            "--max-crf",
            "25",
            "--min-samples",
            "5",
            //"--sample-duration",
            //"5s",
        ];
    
        if (Variables.SVT) {
            executeArgs.argumentList = executeArgs.argumentList.concat(
                ["--svt", Variables.SVT]
            );
        }
    
        if (Variables.KeyInt) {
            executeArgs.argumentList = executeArgs.argumentList.concat(
                ["--keyint", Variables.KeyInt]
            );
        }
    
    
        if (Flow.IsLinux) {
            executeArgs.command = "bash";
            let args = executeArgs.argumentList.join(" ");
            let cache = Flow.TempPath.replace(/[^\/]+$/, "");
    
            executeArgs.argumentList = [
                "-c",
                `XDG_CACHE_HOME=${cache} PATH=${path}:\$PATH ${abAv1} ${args}`,
                "/dev/null",
            ];
        }
    
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
                if (ErrorOnFail) {
                    returnValue.error = true;
                }
            } else {
                Logger.WLog(executeAbAv1.output);
                returnValue.message =
                    "Failed to execute ab-av1: " + executeAbAv1.exitCode;
                returnValue.error = true;
            }
        }
    
        table(returnValue);
    
        return returnValue;
    }
    
    function bytesToHuman(b) {
        var u = 0,
            s = 1024;
        while (b >= s || -b >= s) {
            b /= s;
            u++;
        }
        return (u ? b.toFixed(2) + " " : b) + " KMGTPEZY"[u] + "Bps";
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

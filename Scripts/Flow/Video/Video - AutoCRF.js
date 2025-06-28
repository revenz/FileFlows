/**
 * @name Video - AutoCRF
 * @uid d08608d8-3b50-4784-ad68-fd4ed102577c
 * @description Finds the correct CRF using VMAF score based on a
maximum BitRate and your selected codec types, years of testing and community input
 * @help Put me between 'FFMPEG Builder: Start' and 'FFMPEG Builder: Executor'
Required DockerMods: AutoCRF, FFmpeg FileFlows Edition

~~~~~~~~~~~~~~~~~ NEW ~~~~~~~~~~~~~~~~~
!!Please remove FFmpeg Builder Video Manual from output 1, it is no longer required!!
~~~~~~~~~~~~~~~~~ NEW ~~~~~~~~~~~~~~~~~
This script will always try and convert the video to the target codec by trying to calculate a CRF for a smaller file.

In the event of it not finding a suitable CRF and the codec isn't in FallBackCodecs it will do BitRate encoding automatically

This Flow Element two outputs:
1: The video will be encoded
2: The video is in an acceptable codec and bitrate
You do not need to hook up any of the FFmpeg Builder flow elements!

~~~~~~~~~~~~~~~~~ NEW ~~~~~~~~~~~~~~~~~
Should you wish to test custom parameters you can do so by using the flow element before this script (e.g. -x265-params or -qsv_device)
Good defaults for QSV encodes that yield better results
Ability to force all encodes to 10bit, this will very slightly improve your score
Changed default to veryslow (QSV), this can be changed in a variable called Preset
~~~~~~~~~~~~~~~~~ NEW ~~~~~~~~~~~~~~~~~

Recommended defaults:
    FallBackCodecs: hevc, h.264
    MaxBitRate: 11.5 MBps for SDR and 23.5 MBps Dolby Vision

All parameters can also be overridden using Variables for example
    TargetCodec = hevc_nvenc
    MaxBitRate = 12
    FallBackCodecs = hevc|h.264|mpeg4|custom
    SVT = lookahead=64:film-grain=8
    KeyInt = 240
    Threads = 4
    Preset = slow

For further help or feature requests find me in the discord
 * @author lawrence
 * @revision 15
 * @param {('hevc_qsv'|'hevc_nvenc'|'hevc'|'av1_qsv'|'libsvtav1'|'av1_nvenc'|'h264_qsv'|'h264'|'h264_nvenc'|'hevc_vaapi')} TargetCodec Which codec you want as the output
 * @param {('hevc'|'h264'|'av1'|'vp9'|'mpeg2'|'mpeg4')[]} FallBackCodecs Video codecs that you are happy to keep if no CRf can be found
 * @param {int} MaxBitRate The maximum acceptable bitrate in MBps
 * @param {bool} FixDolby5 Create a SDR fallback for Dolby Vision profile 5 (aka the green/purple one) [CPU decode]
 * @param {bool} UseTags Create tags (premium feature) such as "Copy", "CRF 17", "Fallback"
 * @param {bool} ErrorOnFail Error on CRF detection fail rather than fallback
 * @param {bool} TestMode This doesn't calculate you a score, instead just tells you if it would need too
 * @output Video will be encoded
 * @output Video will not be encoded
 */
function Script() {
    // Checking dependencies
    if (!ToolPath("ab-av1", "/app/common/autocrf")) {
        return -1;
    }
    let ffmpeg = ToolPath("ffmpeg", "/app/common/autocrf");
    if (!ffmpeg) {
        return -1;
    }
    if (!ToolPath("ffmpeg", "/app/common/ffmpeg-static")) {
        // check for urination
        if (!ToolPath("ffmpeg", "/opt/ffmpeg-uranite-static/bin")) {
            return -1;
        }
    }
    if (!ToolPath("ffmpeg", "/usr/local/bin")) {
        return -1;
    }

    if (Variables["FixDolby5"]) {
        FixDolby5 = Variables["FixDolby5"];
    }

    if (Variables["UseTags"]) {
        UseTags = Variables["UseTags"];
    }

    if (Variables["ErrorOnFail"]) {
        ErrorOnFail = Variables["ErrorOnFail"];
    }

    if (Variables["TargetCodec"]) {
        TargetCodec = Variables["TargetCodec"];
    }

    if (!TargetCodec) {
        TargetCodec = "hevc";
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
    if (MaxBitRate > 110) {
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
        Logger.WLog(
            "FileFlows has no bitrate info about the video, calculating..."
        );
        // video stream doesn't have bitrate information
        // need to use the overall bitrate
        let overall = Variables.vi.VideoInfo.Bitrate;
        if (!overall) return 0; // couldn't get overall bitrate either

        // overall bitrate includes all audio streams, so we try and subtract those
        let calculated = overall;
        if (Variables.vi.VideoInfo.AudioStreams?.length) {
            // check there are audio streams
            for (let audio of Variables.vi.VideoInfo.AudioStreams) {
                if (audio.Bitrate > 0) calculated -= audio.Bitrate;
                else {
                    // audio doesn't have bitrate either, so we just subtract 5% of the original bitrate
                    // this is a guess, but it should get us close
                    calculated -= overall * 0.05;
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
    let preset = "slow";

    if (TargetCodec.includes("qsv")) {
        preset = "veryslow";
    }

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

    if (Variables["Preset"]) {
        preset = Variables["Preset"];
    }

    if (Variables.FfmpegBuilderModel.ForceEncode) {
        forceEncode = true;
    }

    Logger.ILog(`Video is ${videoDescription}`);

    if (video.Stream.DolbyVision && !video.Stream.HDR && FixDolby5) {
        Logger.ILog("Video is DoVi without a fallback, so were creating one");
        forceEncode = true;
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
        Logger.WLog(`Codec is ${videoCodec} not ${FallBackCodecs.join(", ")}`);
        Logger.ILog(`Will fallback to bitrate encoding`);
        forceEncode = true;
    }

    let returnValue = {
        data: [
            // { crf: 12, score: 12.34, size: 90 }
        ],
        error: false,
        winner: null,
        command: `${TargetCodec} -preset ${preset}`,
        message: "",
    };

    Logger.ILog(
        `Targeting ${firstTryPercentage}% size, ${firstTryScore}% VMAF`
    );
    let attempt = search(
        returnValue,
        firstTryPercentage,
        firstTryScore,
        videoBitRate,
        preset
    );
    if (!attempt.winner) {
        Logger.ILog(
            `First attempt failed retrying with ${secondTryPercentage}% size, ${secondTryScore}% VMAF`
        );
        returnValue.data = [];
        returnValue.command = `${TargetCodec} -preset ${preset}`;
        attempt = search(
            returnValue,
            secondTryPercentage,
            secondTryScore,
            videoBitRate,
            preset
        );
    }

    if (attempt.winner) {
        let crf_arg = "-crf";
        if (TargetCodec.includes("_vaapi")) {
            crf_arg = "-q";
        }
        if (TargetCodec.includes("_vulkan")) {
            crf_arg = "-qp";
        }
        if (TargetCodec.includes("_nvenc")) {
            crf_arg = "-cq";
        }
        if (TargetCodec.includes("_qsv")) {
            crf_arg = "-global_quality";
        }

        attempt.command
            .split(" ")
            .concat(`${crf_arg}:v`, attempt.winner.crf)
            .forEach((obj) => {
                Variables.FfmpegBuilderModel?.VideoStreams[0].EncodingParameters.Add(
                    obj
                );
            });

        Logger.ILog(
            `Attempt successful with ${attempt.winner.size}% size, ${attempt.winner.score}% VMAF`
        );
        Logger.ILog(
            `Encoding settings: ${attempt.command} ${crf_arg}:v ${attempt.winner.crf}`
        );
        Flow.AdditionalInfoRecorder("Score", attempt.winner.score, 1000);
        Flow.AdditionalInfoRecorder("CRF", attempt.winner.crf, 1000);
        if (UseTags) {
            Flow.AddTags(["VMAF", `CRF ${attempt.winner.crf}`]);
        }
    }

    if (attempt.error) {
        Logger.ELog(attempt.message);
        Flow.fail(`AutoCRF: ${attempt.message}`);
        return -1;
    }

    if (video.Stream.DolbyVision && !video.Stream.HDR && FixDolby5) {
        Logger.ILog("Testing for openCL");
        let process = Flow.Execute({
            command: ffmpeg,
            argumentList: [
                "-hwaccel",
                "opencl",
                "-f",
                "lavfi",
                "-i",
                "testsrc=size=640x480:rate=25",
                "-t",
                "1",
                "-c:v",
                "libx264",
                "-f",
                "null",
                "-",
            ],
        });
        if (process.exitCode == 0) {
            [
                "-init_hw_device",
                "opencl=ocl",
                "-filter_hw_device",
                "ocl",
            ].forEach((ob) => {
                Variables.FfmpegBuilderModel.CustomParameters.Add(ob);
            });
            video.filter.Add(
                "format=p010le,hwupload=derive_device=opencl,tonemap_opencl=tonemap=bt2390:transfer=smpte2084:matrix=bt2020:primaries=bt2020:format=p010le,hwdownload,format=p010le"
            );
        } else {
            Logger.WLog(
                "Could not find openCL, you may want the oneVPL DockerMod"
            );
            video.filter.Add(
                "tonemapx=tonemap=bt2390:transfer=smpte2084:matrix=bt2020:primaries=bt2020"
            );
        }
        if (TargetCodec.includes("qsv")) {
            Logger.WLog(
                "QSV does not support dolby vision 5 decode properly so we are disabling it"
            );
            Variables.NoQSV = true;
            Variables.NoVAAPI = true;
        }
    }

    if (attempt.winner) {
        return 1;
    }

    // fallback
    if (forceEncode) {
        // setup bitrate encode
        attempt.command.split(" ").forEach((obj) => {
            Variables.FfmpegBuilderModel?.VideoStreams[0].EncodingParameters.Add(
                obj
            );
        });

        let t = targetBitRate / 1024.0 / 1024.0;

        let obs = [
            "-b:v:{index}",
            `${t.toFixed(2)}M`,
            "-minrate",
            `${(t * 0.75).toFixed(2)}M`,
            "-maxrate",
            `${t * (1.25).toFixed(2)}M`,
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
        Logger.ILog(`Encoding settings: ${attempt.command}`);
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

function search(
    returnValue,
    bitratePercent,
    targetPercent,
    videoBitRate,
    preset
) {
    let abAv1 = ToolPath("ab-av1", "/app/common/autocrf");
    let path = abAv1.replace(/\/[^\/]+$/, "");

    if (Variables["SVT"]) {
        returnValue.command = `${returnValue.command} -svtav1-params ${Variables["SVT"]}`;
    }

    if (Variables["KeyInt"]) {
        returnValue.command = `${returnValue.command} -g ${Variables["KeyInt"]}`;
    } else {
        returnValue.command = `${returnValue.command} -g ${
            Math.round(Variables.vi.VideoInfo.VideoStreams[0].FramesPerSecond) *
            10
        }`;
    }

    if (TargetCodec.includes("qsv")) {
        returnValue.command = `${returnValue.command} -look_ahead 1 -extbrc 1 -look_ahead_depth 40`;
    }

    let videoPixelFormat = "yuv420p";

    if (Variables.Force10Bit) {
        ForceTenBit = Variables.Force10Bit;
    }

    if (Variables.ForceTenBit) {
        ForceTenBit = Variables.ForceTenBit;
    }

    if (Variables.vi.VideoInfo.VideoStreams[0].Is10Bit) {
        videoPixelFormat = "yuv420p10le";
        Variables.FfmpegBuilderModel.VideoStreams[0].Bits = 10;
        if (TargetCodec.includes("hevc") || TargetCodec.includes("265")) {
            returnValue.command = `${returnValue.command} -pix_fmt:v:0 p010le -profile:v:0 main10`;
        }
    }

    let targetBitRate = (bitratePercent / 100) * videoBitRate;

    Logger.ILog(
        `Searching for CRF under ${bytesToHuman(
            targetBitRate
        )} @ ${targetPercent}% original quality`
    );

    var executeArgs = new ExecuteArgs();
    executeArgs.command = abAv1;
    executeArgs.argumentList = [
        "crf-search",
        "-i",
        Flow.FileService.GetLocalPath(Variables.file.FullName).Value,
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
    ];

    if (Variables.SVT) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--svt",
            Variables.SVT,
        ]);
    }

    if (Variables.KeyInt) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--keyint",
            Variables.KeyInt,
        ]);
    }

    if (Variables["Threads"]) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--vmaf",
            `n_threads=${Variables["Threads"]}`,
        ]);
    } else {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--vmaf",
            `n_threads=4`,
        ]);
    }

    if (Variables["SubSample"]) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--vmaf",
            `n_subsample=${Variables["SubSample"]}`,
        ]);
    }

    if (Variables["SampleDuration"]) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--sample-duration",
            Variables["SampleDuration"],
        ]);
    } else {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--sample-duration",
            "20s",
        ]);
    }

    if (TargetCodec.includes("vaapi")) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--enc-input",
            "hwaccel=vaapi",
            "--enc-input",
            "hwaccel_output_format=vaapi",
        ]);

        // VAAPI can only handle values divisible by 8
        let w = Variables.vi.VideoInfo.VideoStreams[0].Width;
        let h = Variables.vi.VideoInfo.VideoStreams[0].Height;

        if (w % 8 !== 0 || h % 8 !== 0) {
            if (w <= 1920 || h <= 1080) {
                w = 1920;
                h = 1080;
            }

            if (w <= 3840 || h <= 2160) {
                w = 3840;
                h = 2160;
            }

            // AMD can only use vaapi if you can divide the pixels by 8
            executeArgs.argumentList = executeArgs.argumentList.concat([
                "--vfilter",
                `hwupload,scale_vaapi=${w}:${h}`,
                "--reference-vfilter",
                `scale=${w}:${h}:flags=bicubic`,
            ]);
        }
    }

    if (Variables.FfmpegBuilderModel.CustomParameters.length) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--enc",
            Variables.FfmpegBuilderModel.CustomParameters.join(" ")
                .replace(/-([\S]+)\s([\S]+)/g, "$1=$2")
                .replace(/" "/g, ":"),
        ]);
    }

    if (TargetCodec.includes("qsv")) {
        executeArgs.argumentList = executeArgs.argumentList.concat([
            "--enc",
            "look_ahead=1:extbrc=1:look_ahead_depth=40",
        ]);
    }

    if (!Flow.IsWindows) {
        let cache = Flow.TempPath.replace(/[^\/]+$/, "");
        let existingPath = System.Environment.GetEnvironmentVariable("PATH");

        executeArgs.EnvironmentalVariables[
            "PATH"
        ] = `${path}${System.IO.Path.PathSeparator}${existingPath}`;
        executeArgs.EnvironmentalVariables["XDG_CACHE_HOME"] = cache;
    }

    if (Flow.IsDocker) {
        executeArgs.EnvironmentalVariables["XDG_CACHE_HOME"] = "/app/common/autocrf/cache";
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
                /(crf ([0-9.]+) )VMAF ([0-9.]+) predicted.*\(([0-9.]+)%/i
            ))
        ) {
            returnValue.data.push({
                crf: matches[2].trim(),
                score: matches[3].trim(),
                size: matches[4].trim(),
            });
        }
        if ((matches = line.match(/crf ([0-9.]+) successful/i))) {
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
        let crf = line.crf.toString().substring(0, 4).padStart(4);
        let score = line.score.toString().padStart(5);
        let size = line.size.toString().padStart(3);
        Logger.ILog(`|${crf} | ${score} | ${size}% |`);
    }

    if (output.winner) {
        let crf = output.winner.crf.toString().substring(0, 4).padStart(4);
        let score = output.winner.score.toString().padStart(5);
        let size = output.winner.size.toString().padStart(3);
        Logger.ILog("----------------------");
        Logger.ILog(`|${crf} | ${score} | ${size}% |`);
    }
    Logger.ILog(" ");
}

function ToolPath(tool, path) {
    if (Flow.IsDocker) {
        if (System.IO.File.Exists(`${path}/${tool}`)) {
            return `${path}/${tool}`;
        } else {
            Logger.ELog("You may need to remove other FFmpeg DockerMods");
            Logger.WLog(`Cannot find ${path}/${tool}`);
            Logger.WLog(
                "AutoCRF: If you're absolutely sure you installed the DockerMods please restart the node"
            );
            Logger.WLog(
                "AutoCRF: Please remove FFmpeg Builder Video Manual from output 1, it is no longer required"
            );
            Flow.Fail(
                "AutoCRF: Please also remove FFmpeg Builder Video Manual, Missing required DockerMods: AutoCRF, FFmpeg FileFlows Edition"
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

function roundToNearestEight(number) {
    return Math.round(number / 8) * 8;
}

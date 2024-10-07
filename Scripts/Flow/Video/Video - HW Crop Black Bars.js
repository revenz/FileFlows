/**
 * @name Video - HW Crop Black Bars
 * @description Run after Crop Black Bars.
 * This is designed to have one enabled.
 * If your flow needs both enabled please disable the relevant card in the node variables
 * e.g. NoQsv = true or NoNvidia = true
 * @author Lawrence Curtis
 * @uid 22e8d770-ffba-4a5c-887c-c7224ef29809
 * @revision 1
 * @param {bool} EnableQSV Enable Intel GPU Acceleration
 * @param {bool} EnableNvidia Enable Nvidia GPU Acceleration
 * @output Converted
 * @output Not convertion required
 */
function Script(EnableQSV, EnableNvidia) {
    let video = Variables.FfmpegBuilderModel.VideoStreams[0];

    if (!EnableQSV && !EnableNvidia) {
        Logger.ILog("Neither Intel or Nvidia acceleration selected")
        return 2;
    }
    if (EnableQSV && Variables.NoQSV) {

        Logger.ILog("Intel acceration is disabled")
        return 2;
    }
    if (EnableNvidia && Variables.NoNvidia) {
        Logger.ILog("Nvidia acceration is disabled")
        return 2;
    }

    for (var i = 0; i < video.filter.length; i++) {
        var filter = video.filter[i];
        let matches = filter.match(/crop=(.*)/);
        if (matches) {
            video.filter.RemoveAt(i);
            const parts = matches[1].split(":");

            if (EnableQSV) {
                video.filter.Add(
                    `vpp_qsv=cw=${parts[0]}:ch=${parts[1]}:cx=${parts[2]}:cy=${parts[3]}`
                );
            }

            if (EnableNvidia) {
                video.AdditionalParameters.Add("-crop");
                video.AdditionalParameters.Add(matches[1]);
            }
            return 1;
        }
    }

    return 2;
}

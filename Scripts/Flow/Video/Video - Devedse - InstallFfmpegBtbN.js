/**
 * Important: This script was made to be ran with the docker version of FileFlows. Results on other platforms may vary.
 * Checks for the latest release of FFmpeg, compares with the stored version,
 * and updates if the latest version is newer.
 * @author Devedse
 * @revision 4
 * @minimumVersion 1.0.0.0
 * @output The command succeeded
 */
function Script()
{
    let targetDirectory = '/app/Data/tools/ffmpegbtbn/';
    let versionFilePath = targetDirectory + 'version.txt';

    // Fetch the latest release information
    let releaseInfo = Flow.Execute({
        command: 'curl',
        argumentList: ['-s', 'https://api.github.com/repos/BtbN/FFmpeg-Builds/releases/latest']
    });
    if(releaseInfo.exitCode !== 0){
        Logger.ELog('Failed to fetch release information.');
        return -1;
    }

    // Parse the JSON for the latest version
    let latestVersion;
    try {
        let releaseData = JSON.parse(releaseInfo.output);
        latestVersion = releaseData.name;
    } catch (error) {
        Logger.ELog('Error parsing JSON: ' + error);
        return -1;
    }

    Logger.ILog('Latest FFmpeg version online: ' + latestVersion);

    // Read the existing version if the file exists
    let readVersion = Flow.Execute({
        command: 'cat',
        argumentList: [versionFilePath]
    });

    if (readVersion.exitCode === 0 && readVersion.output.trim() === latestVersion) {
        Logger.ILog('FFmpeg is up-to-date.');

        Variables.FfmpegBtbn = targetDirectory;
        Logger.ILog('Set Variables.FfmpegBtbn to ' + targetDirectory);
        
        return 1;
    }
    
    Logger.ILog('FFmpeg is not up to date (' + readVersion.output.trim() + '), downloading new version...');

    let assetUrl = '';
    try {
        let releaseData = JSON.parse(releaseInfo.output);

        // Loop through assets to find the one ending with .tar.xz
        for (let asset of releaseData.assets) {
            if (asset.browser_download_url.endsWith('linux64-gpl.tar.xz')) {
                assetUrl = asset.browser_download_url;
                break;
            }
        }

        if (assetUrl) {
            Logger.ILog('Download URL: ' + assetUrl);
        } else {
            Logger.ELog('No asset found ending with linux64-gpl.tar.xz');
            return -1;
        }
    } catch (error) {
        Logger.ELog('Error parsing JSON: ' + error);
        return -1;
    }

    // URL of the release and target directory
    let archivePath = targetDirectory + 'ffmpeg.tar.xz';

    // Ensure target directory exists
    let mkdir = Flow.Execute({
        command: 'mkdir',
        argumentList: ['-p', targetDirectory]
    });
    if(mkdir.exitCode !== 0){
        Logger.ELog('Failed to create directory: ' + targetDirectory);
        return -1;
    }

    // Download the file
    let download = Flow.Execute({
        command: 'curl',
        argumentList: ['-L', '-o', archivePath, assetUrl]
    });
    if(download.exitCode !== 0){
        Logger.ELog('Failed to download file: ' + assetUrl);
        return -1;
    }

    // Extract the contents of the bin directory directly into the target directory
    let extract = Flow.Execute({
        command: 'tar',
        argumentList: ['-xvf', archivePath, '-C', targetDirectory, '--strip-components=2', 'ffmpeg-master-latest-linux64-gpl/bin/']
    });
    if(extract.exitCode !== 0){
        Logger.ELog('Failed to extract contents of bin directory to ' + targetDirectory);
        return -1;
    }

    // Make all files without an extension in the target directory executable
    let makeExecutable = Flow.Execute({
        command: 'find',
        argumentList: [targetDirectory, '-type', 'f', '!', '-name', '*.*', '-exec', 'chmod', '+x', '{}', ';']
    });
    if(makeExecutable.exitCode !== 0){
        Logger.ELog('Failed to make files executable in ' + targetDirectory);
        return -1;
    }

    Logger.ILog('Made all files without an extension in ' + targetDirectory + ' executable');

    // After successful extraction, write the latest version to the version file
    let writeVersion = Flow.Execute({
        command: 'sh',
        argumentList: ['-c', `echo "${latestVersion}" > "${versionFilePath}"`]
    });

    if(writeVersion.exitCode !== 0){
        Logger.ELog('Failed to write new version to file.');
        return -1;
    }

    Logger.ILog('FFmpeg extracted successfully to ' + targetDirectory);

    // Set the variable with the directory path
    Variables.FfmpegBtbn = targetDirectory;
    Logger.ILog('Set Variables.FfmpegBtbn to ' + targetDirectory);

    return 1;
}
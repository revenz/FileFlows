/**
 * Important: This script was made to be ran with the docker version of FileFlows. Results on other platforms may vary.
 * Checks for the latest release of ab-av1, compares with the stored version,
 * and updates if the latest version is newer.
 * @author Devedse
 * @revision 4
 * @minimumVersion 1.0.0.0
 * @output The command succeeded
 */
function Script()
{
    let targetDirectory = '/app/Data/tools/ab-av1/';
    let versionFilePath = targetDirectory + 'version.txt';

    // Fetch the latest release information
    let releaseInfo = Flow.Execute({
        command: 'curl',
        argumentList: ['-s', 'https://api.github.com/repos/alexheretic/ab-av1/releases/latest']
    });
    if(releaseInfo.exitCode !== 0){
        Logger.ELog('Failed to fetch release information.');
        return -1;
    }

    // Parse the JSON for the latest version
    let latestVersion;
    try {
        let releaseData = JSON.parse(releaseInfo.output);
        latestVersion = releaseData.tag_name;
    } catch (error) {
        Logger.ELog('Error parsing JSON: ' + error);
        return -1;
    }

    Logger.ILog('Latest version online: ' + latestVersion);

    // Read the existing version if the file exists
    let readVersion = Flow.Execute({
        command: 'cat',
        argumentList: [versionFilePath]
    });

    if (readVersion.exitCode === 0 && readVersion.output.trim() === latestVersion) {
        Logger.ILog('ab-av1 is up-to-date.');
        return 1;
    }
    
    
    Logger.ILog('ab-av1 is not up to date (' + readVersion.output.trim() + '), downloading new version...');

    // Install zstd if not already installed
    let installZstd = Flow.Execute({
        command: 'apt-get',
        argumentList: ['update']
    });
    if(installZstd.exitCode !== 0){
        Logger.ELog('Failed to update package list.');
        return -1;
    }

    installZstd = Flow.Execute({
        command: 'apt-get',
        argumentList: ['install', '-y', 'zstd']
    });
    if(installZstd.exitCode !== 0){
        Logger.ELog('Failed to install zstd.');
        return -1;
    }


    let assetUrl = '';
    try {
        // Parse the JSON response
        let releaseData = JSON.parse(releaseInfo.output);

        // Loop through assets to find the one ending with .tar.zst
        for (let asset of releaseData.assets) {
            if (asset.browser_download_url.endsWith('.tar.zst')) {
                assetUrl = asset.browser_download_url;
                break;
            }
        }

        if (assetUrl) {
            Logger.ILog('Download URL: ' + assetUrl);
        } else {
            Logger.ELog('No asset found ending with .tar.zst');
            return -1;
        }
    } catch (error) {
        Logger.ELog('Error parsing JSON: ' + error);
        return -1;
    }


    // URL of the release and target directory
    let targetDirectory = '/app/Data/tools/ab-av1/';
    let archivePath = targetDirectory + 'ab-av1.tar.zst';

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

    // Extract the file
    let extract = Flow.Execute({
        command: 'tar',
        argumentList: ['--use-compress-program=unzstd', '-xvf', archivePath, '-C', targetDirectory]
    });
    if(extract.exitCode !== 0){
        Logger.ELog('Failed to extract file to ' + targetDirectory);
        return -1;
    }

    // After successful extraction, write the latest version to the version file
    let writeVersion = Flow.Execute({
        command: 'sh',
        argumentList: ['-c', `echo "${latestVersion}" > "${versionFilePath}"`]
    });

    if(writeVersion.exitCode !== 0){
        Logger.ELog('Failed to write new version to file.');
        return -1;
    }

    Logger.ILog('File extracted successfully to ' + targetDirectory);
    return 1;
}
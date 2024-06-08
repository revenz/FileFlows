import { Sonarr } from 'Shared/Sonarr';
/**
 * This script will send a rename command to Sonarr
 * @autor Anthony Clerici
 * @version 1.0.0
 * @revision 1
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not found or not needed to be renamed
 */
function Script(URI, ApiKey) {
    let sonarr = new Sonarr(URI, ApiKey);
    const folderPath = Variables.folder.FullName;
    const currentFileName = Variables.file.Name;
    let newFileName = null;
    let episodeFileId = null;

    // Find series name from sonarr
    let series = findSeries(folderPath, sonarr);

    if (!series) {
        Logger.WLog('Series not found for path: ' + folderPath);
        return 2;
    }

    try {
        // Ensure series is refreshed before renaming
        let refreshBody = {
            seriesId: series.id
        }
        let refreshData = sonarr.sendCommand('RescanSeries', refreshBody)
        Logger.ILog(`Series refreshed`);

        // Wait for the completion of the refresh scan
        let refreshCompleted = sonarr.waitForCompletion(refreshData.id, sonarr);
        if (!refreshCompleted) {
            Logger.ILog('Refresh not completed');
            return -1;
        }

        let renamedEpisodes = fetchRenamedFiles(series.id, sonarr);
        if (!renamedEpisodes) {
            Logger.ILog('No episodes need to be renamed');
            return 2;
        }

        Logger.ILog(`Searching for an episode previously named ${currentFileName}`);
        renamedEpisodes.every((episode) => {
            if (episode.existingPath.endsWith(currentFileName)) {
                episodeFileId = episode.episodeFileId;
                newFileName = System.IO.Path.GetFileName(episode.newPath);
                Logger.ILog(`Found it, renaming file ${episodeFileId} to ${newFileName}`);
                return false;
            }
            return true;
        });

        if (newFileName === null) {
            Logger.WLog('No matching episode found to rename.');
            return 2;
        }

        let renameBody = {
            seriesId: series.id,
            files: [episodeFileId]
        }
        let renameResponse = sonarr.sendCommand('RenameFiles', renameBody, URI, ApiKey);
        let renameCompleted = sonarr.waitForCompletion(renameResponse.id);

        if (!renameCompleted) {
            Logger.ILog('Rename not completed');
            return -1;
        }
        Logger.ILog(`Episode ${episodeFileId} successfully renamed. Setting as working file.`)

        // Sonarr has successfully renamed the file, set new filename as working directory
        let newFilePath = System.IO.Path.Combine(Variables.folder.FullName, newFileName);
        Flow.SetWorkingFile(newFilePath);
        return 1;

    } catch (error) {
        Logger.WLog('Error: ' + error.message);
        return -1;
    }
}

// Repeatedly try finding a show by shortening the path
function findSeries(filePath, sonarr) {
    let currentPath = filePath;
    let show = null;

    while (currentPath) {
        show = sonarr.getShowByPath(currentPath);
        if (show) {
            Logger.ILog('Show found: ' + show.id);
            return show;
        }

        // If no show is found, go up 1 dir
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (currentPath === null || currentPath === "") {
            Logger.WLog('Unable to find show file at path ' + filePath);
            return null;
        }
    }
}

function fetchRenamedFiles(seriesId, sonarr) {
    let endpoint = 'rename';
    let queryParams = `seriesId=${seriesId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);
    return response;
}
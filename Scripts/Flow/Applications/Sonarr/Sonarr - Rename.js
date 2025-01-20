import { Sonarr } from 'Shared/Sonarr';

/**
 * @description This script will rename the file through Sonarr
 * @author Shaun Agius, Anthony Clerici
 * @revision 13
 * @uid 3464db0c-7262-4dce-9088-d1ad628da60e
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:8989)
 * @param {string} ApiKey API Key
 * @output Item renamed successfully
 * @output Rename not required for item
 * @output Item not found
 */
function Script(URI, ApiKey) {
    // Remove trailing / from URI
    URI = URI.replace(/\/$/, '');
    let sonarr = new Sonarr(URI, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const ogFileName = Variables.file.Orig.FileName;
    let currentFileName = Variables.file.FullName;
    let newFilePath = null;

    // Find series name from sonarr
    let [series, basePath] = findSeries(folderPath, sonarr);

    if (series?.id === undefined) {
        Logger.WLog('Series not found for path: ' + folderPath);
        return 3;
    } else {
        Logger.ILog(`Series found: ${series.title}`);
    }

    // Find episode
    let [ogEpisodeFile, episode] = fetchEpisode(currentFileName, series, sonarr);

    if (episode?.id !== undefined) {
        Logger.ILog(`Original episode found: Season ${episode.seasonNumber} Episode: ${episode.episodeNumber}`)
    } else {
        Logger.WLog(`Episode could not be extracted from series`);
        return 3;
    }

    // Ensure series is refreshed before renaming
    let refreshData = refreshSeries(series.id, sonarr);

    // Wait for the completion of the scan
    let refreshCompleted = sonarr.waitForCompletion(refreshData.id, sonarr);
    if (!refreshCompleted) {
        Logger.WLog('refresh failed');
        return -1;
    }

    // init new file objects
    let newEpisodeFile = null;
    let newEpisodeFileId = null;

    // Sometimes sonarr doesn't autodetect the transcoded files so we need to manually import it for sonarr to rename it
    let manualImport = fetchManualImportFile(ogFileName, series.id, episode.seasonNumber, sonarr);
    if (manualImport) {
        Logger.ILog('Updated file not auto-detected by Sonarr. Manually importing')

        let importCommand = manuallyImportFile(manualImport, episode.id, sonarr)

        let importCompleted = sonarr.waitForCompletion(importCommand.id, sonarr);
        if (!importCompleted) {
            Logger.WLog('import not completed');
            return -1;
        }

        // Refresh for newly imported episode
        refreshData = refreshSeries(series.id, sonarr);
        // Wait for the completion of the scan
        refreshCompleted = sonarr.waitForCompletion(refreshData.id, sonarr);
        if (!refreshCompleted) {
            Logger.WLog('refresh failed');
            return -1;
        }

        // Set new episodeFile and episode
        [newEpisodeFile, episode] = fetchEpisode(currentFileName, series, sonarr);
        newEpisodeFileId = newEpisodeFile.id;

        // Sonarr likely unmonitored episode in this scenario, set to monitored
        toggleMonitored([episode.id], URI, ApiKey);
    } else {
        Logger.ILog(`Manual import not needed`);
    }

    let renamedEpisodes = fetchRenamedFiles(series.id, sonarr);
    if (!renamedEpisodes) {
        Logger.ILog('No episodes need to be renamed');
        return 2;
    }

    // Find file id of newly grabbed file
    if (!newEpisodeFileId) {
        [newEpisodeFile, episode] = fetchEpisode(currentFileName, series, sonarr);
        newEpisodeFileId = newEpisodeFile.id;
    }

    Logger.ILog(`Searching for episode file with id ${newEpisodeFileId}`);
    renamedEpisodes.every((renamedFile) => {
        if (renamedFile.episodeFileId === newEpisodeFileId) {
            let newFileName = renamedFile.newPath;
            // Get differences in the path
            Logger.ILog(`Base path: ${basePath}`);
            // Construct new filepath
            Logger.ILog(`New path: ${newFileName}`);
            newFilePath = System.IO.Path.Combine(basePath, newFileName);
            Logger.ILog(`Found it, renaming file ${newEpisodeFileId} to ${newFilePath}`);
            return false;
        }
        return true;
    });

    if (newFilePath === null) {
        Logger.WLog("Episode doesn't need renaming");
        return 2;
    }

    let renameBody = {
        seriesId: series.id,
        files: [newEpisodeFileId]
    }
    let renameResponse = sonarr.sendCommand('RenameFiles', renameBody, URI, ApiKey);
    let renameCompleted = sonarr.waitForCompletion(renameResponse.id);

    if (!renameCompleted) {
        Logger.ILog('Rename not completed');
        return -1;
    }
    Logger.ILog(`Episode file ${newEpisodeFileId} successfully renamed. Setting as working file.`)

    // Sonarr has successfully renamed the file, set new filename as working directory
    Flow.SetWorkingFile(newFilePath);
    return 1;

}

// Repeatedly try finding a show by shortening the path
function findSeries(filePath, sonarr) {
    let currentPath = filePath;
    let show = null;

    let allSeries = sonarr.getAllShows();
    let seriesFolders = {};

    // Map each folder back to its series
    for (let series of allSeries) {
        let folderName = System.IO.Path.GetFileName(series.path);
        seriesFolders[folderName] = series;
    }

    while (currentPath) {
        // Get childmost piece of path to work with different remote paths
        let currentFolder = System.IO.Path.GetFileName(currentPath);

        if (seriesFolders[currentFolder]) {
            show = seriesFolders[currentFolder];
            Logger.ILog('Show found: ' + show.id);
            return [show, currentPath];
        }

        // If no show is found, go up 1 dir
        Logger.ILog(`Show not found at ${currentPath}. Trying ${System.IO.Path.GetDirectoryName(currentPath)}`)
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (!currentPath) {
            Logger.WLog('Unable to find show file at path ' + filePath);
            return [null, null];
        }
    }
    return [null, null];
}

function fetchRenamedFiles(seriesId, sonarr) {
    let endpoint = 'rename';
    let queryParams = `seriesId=${seriesId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);
    return response;
}

function fetchEpisodeFile(path, series, sonarr) {
    Logger.ILog(`Searching for ${path}`);
    let allFiles = sonarr.getFilesInShow(series);

    for (let file of allFiles) {
        if (file.path.endsWith(path)) {
            return file;
        }
    }
    Logger.WLog(`Episode file not found in series ${series.title}`);
    return null
}

function fetchEpisodeFromId(episodeFileId, sonarr) {
    let endpoint = 'episode';
    let queryParams = `episodeFileId=${episodeFileId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);

    return response[0];
}

function fetchManualImportFile(currentFileName, seriesId, seasonNumber, sonarr) {
    let endpoint = 'manualimport';
    let queryParams = `seriesId=${seriesId}&filterExistingFiles=true&seasonNumber=${seasonNumber}`;
    let response = sonarr.fetchJson(endpoint, queryParams);

    for (let file of response) {
        if (file.path.endsWith(currentFileName) && file.episodes.length === 0) {
            return file;
        }
    }

    return null;
}

function manuallyImportFile(fileToImport, episodeId, sonarr) {
    let body = {
        files: [
            {
                path: fileToImport.path,
                folderName: fileToImport.folderName,
                seriesId: fileToImport.series.id,
                episodeIds: [episodeId],
                quality: fileToImport.quality,
                languages: fileToImport.languages,
                indexerFlags: fileToImport.indexerFlags,
                releaseType: fileToImport.releaseType,
                releaseGroup: fileToImport.releaseGroup
            }
        ],
        importMode: 'auto',
    }

    return sonarr.sendCommand('manualImport', body)
}

function refreshSeries(seriesId, sonarr) {
    let refreshBody = {
            seriesIds: [seriesId],
            isNewSeries: false
        }
    return sonarr.sendCommand('RefreshSeries', refreshBody)
}

function toggleMonitored(episodeIds, URI, ApiKey, monitored=true) {
    let endpoint = `${URI}/api/v3/episode/monitor`;
    let jsonData = JSON.stringify(
        {
            episodeIds: episodeIds,
            monitored: monitored
        }
    );

    http.DefaultRequestHeaders.Add("X-API-Key", ApiKey);
    let response = http.PutAsync(endpoint, JsonContent(jsonData)).Result;

    http.DefaultRequestHeaders.Remove("X-API-Key");

    if (response.IsSuccessStatusCode) {
        let responseData = JSON.parse(response.Content.ReadAsStringAsync().Result);
        Logger.ILog(`Monitored toggled for ${episodeIds}`);
        return responseData;
    } else {
        let error = response.Content.ReadAsStringAsync().Code;
        Logger.WLog("API error when manually imoporting. code " + error);
        return null;
    }
}

function fetchEpisode(currentFileName, series, sonarr) {
    let episodeFile = fetchEpisodeFile(currentFileName, series, sonarr);

    if (!episodeFile) {
        return [null, null];
    }

    let episodeFileId = episodeFile.id;
    let episode = fetchEpisodeFromId(episodeFileId, sonarr);

    return [episodeFile, episode];
}

import { Sonarr } from 'Shared/SonarrTest';

/**
 * @description This script will send a rename command to Sonarr
 * @author Shaun Agius, Anthony Clerici
 * @revision 9
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not renamed
 * @output Item not found
 */
function Script(URI, ApiKey) {
    let sonarr = new Sonarr(URI, ApiKey);
    const folderPath = Variables.folder.FullName;
    const currentFileName = Variables.file.Name;
    const ogPath = Variables.file.Orig.FileName;
    let newFileName = null;

    // Find series name from sonarr
    let series = findSeries(folderPath, sonarr);

    if (!series) {
        Logger.WLog('Series not found for path: ' + folderPath);
        return 3;
    }

    let episodeFile = fetchEpisodeFile(ogPath, series, sonarr)

    if (!episodeFile) {
        Logger.WLog('Episode not in series ' + series.id);
        return 3;
    }

    let episodeFileId = episodeFile.id;
    let episode = fetchEpisode(episodeFileId, sonarr);

    try {
        // Ensure series is refreshed before renaming
        let rescanData = rescanSeries(series.id, sonarr);

        // Wait for the completion of the scan
        let rescanCompleted = sonarr.waitForCompletion(rescanData.id, sonarr);
        if (!rescanCompleted) {
            Logger.WLog('Rescan failed');
            return -1;
        }

        // Sometimes sonarr doesn't autodetect the transcoded files so we need to manually import it for sonarr to rename it
        let manualImport = fetchManualImportFile(currentFileName, series.id, sonarr);
        if (manualImport) {
            Logger.ILog('Updated file not auto-detected by Sonarr. Manually importing')

            let importCommand = manuallyImportFile(manualImport, episode.id, sonarr)

            let importCompleted = sonarr.waitForCompletion(importCommand.id, sonarr);
            if (!importCompleted) {
                Logger.WLog('import not completed');
                return -1;
            }

            // Refresh for newly imported episode
            rescanData = rescanSeries(series.id, sonarr);
            // Wait for the completion of the scan
            rescanCompleted = sonarr.waitForCompletion(rescanData.id, sonarr);
            if (!rescanCompleted) {
                Logger.WLog('Rescan failed');
                return -1;
            }

            // Reset episodeFile and episode
            episodeFile = fetchEpisodeFile(currentFileName, series, sonarr)

            episodeFileId = episodeFile.id;
            episode = fetchEpisode(episodeFileId, sonarr);

            // Sonarr likely unmonitored episode in this scenario, set to monitored
            toggleMonitored([episode.id], URI, ApiKey);
        }

        let renamedEpisodes = fetchRenamedFiles(series.id, sonarr);
        if (!renamedEpisodes) {
            Logger.ILog('No episodes need to be renamed');
            return 2;
        }

        Logger.ILog(`Searching for episode file with id ${episodeFileId}`);
        renamedEpisodes.every((renamedFile) => {
            if (renamedFile.episodeFileId === episodeFileId) {
                newFileName = System.IO.Path.GetFileName(renamedFile.newPath);
                Logger.ILog(`Found it, renaming file ${episodeFileId} to ${newFileName}`);
                return false;
            }
            return true;
        });

        if (newFileName === null) {
            Logger.WLog("Episode doesn't need renaming");
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
        Logger.ILog(`Episode file ${episodeFileId} successfully renamed. Setting as working file.`)

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
            return show;
        }

        // If no show is found, go up 1 dir
        Logger.ILog(`Show not found at ${currentPath}. Trying ${System.IO.Path.GetDirectoryName(currentPath)}`)
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (!currentPath) {
            Logger.WLog('Unable to find show file at path ' + filePath);
            return null;
        }
    }
    return null;
}

function fetchRenamedFiles(seriesId, sonarr) {
    let endpoint = 'rename';
    let queryParams = `seriesId=${seriesId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);
    return response;
}

function fetchEpisodeFile(path, series, sonarr) {
    let allFiles = sonarr.getFilesInShow(series);

    for (let file of allFiles) {
        if (file.path.endsWith(path)) {
            return file;
        }
    }
    return null
}

function fetchEpisode(episodeFileId, sonarr) {
    let endpoint = 'episode';
    let queryParams = `episodeFileId=${episodeFileId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);

    return response[0];
}

function fetchManualImportFile(currentFileName, seriesId, sonarr) {
    let endpoint = 'manualimport';
    let queryParams = `seriesId=${seriesId}&filterExistingFiles=true`;
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

function rescanSeries(seriesId, sonarr) {
    let refreshBody = {
            seriesId: seriesId
        }
    return sonarr.sendCommand('RescanSeries', refreshBody)
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
        let error = response.Content.ReadAsStringAsync().Result;
        Logger.WLog("API error: " + error);
        return null;
    }
}
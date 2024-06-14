import { Sonarr } from 'Shared/Sonarr';

/**
 * @description This script will send a rename command to Sonarr
 * @author Shaun Agius
 * @revision 8
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
    let episode = sonarr.fetchEpisodeFromFileId(episodeFileId);

    try {
        // Ensure series is refreshed before renaming
        let rescanData = sonarr.rescanSeries(series.id);

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
            rescanData = sonarr.rescanSeries(series.id);
            // Wait for the completion of the scan
            rescanCompleted = sonarr.waitForCompletion(rescanData.id, sonarr);
            if (!rescanCompleted) {
                Logger.WLog('Rescan failed');
                return -1;
            }

            // Reset episodeFile and episode
            episodeFile = fetchEpisodeFile(currentFileName, series, sonarr)

            episodeFileId = episodeFile.id;
            episode = sonarr.fetchEpisodeFromFileId(episodeFileId);

            // Sonarr likely unmonitored episode in this scenario, set to monitored
            sonarr.toggleMonitored([episode.id], URI, ApiKey);
        }

        let renamedEpisodes = sonarr.fetchRenamedFiles(series.id);
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

function fetchEpisodeFile(path, series, sonarr) {
    let allFiles = sonarr.getFilesInShow(series);

    for (let file of allFiles) {
        if (file.path.endsWith(path)) {
            return file;
        }
    }
    return null
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

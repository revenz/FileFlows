import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - Trigger Manual Import
 * @uid 8fe63a47-4a97-4b08-99f4-368d29279ad2
 * @description Trigger Sonarr to manually import the media.
 * @help Performs Sonarr import to the TV Show
 * Run last after file move.
 * @author iBuSH
 * @revision 7
 * @param {string} URL Sonarr root URL and port (e.g. http://sonarr:8989).
 * @param {string} ApiKey Sonarr API Key.
 * @param {string} ImportPath The output path for import triggering (default Working File).
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows).
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy).
 * @param {int} TimeOut Set in seconds the timeout for waiting completion (default 60 seconds, max 3600 seconds).
 * @output Command sent
 */
function Script(URL, ApiKey, ImportPath, UseUnmappedPath, MoveMode, TimeOut) {
    URL = (URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI']).replace(/\/+$/g, '');
    ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
    ImportPath = ImportPath || Variables.file.FullName;
    TimeOut = TimeOut ? Math.min(TimeOut, 3600) * 1000 : 60000;  // ms
    ImportPath = UseUnmappedPath ? Flow.UnMapPath(ImportPath) : ImportPath;
    const importMode = MoveMode ? 'move' : 'copy';
    
    const sonarr = new Sonarr(URL, ApiKey);

    /*── seriesId / episodeId detection ──────────────────────────────────*/
    const searchPattern = Variables.file.Orig.FileNameNoExtension;
    let seriesId = Variables['Sonarr.seriesId'] ?? Variables.TVShowInfo?.id ?? null;
    let episodeIds = Variables['Sonarr.episodeIds'] ?? Variables.TVShowInfo?.EpisodesInfo ?? null;

    Logger.ILog(`Sonarr URL: ${URL}`);
    Logger.ILog(`Triggering Path: ${ImportPath}`);
    Logger.ILog(`Import Mode: ${importMode}`);

    if (seriesId && episodeIds) {
        Logger.ILog(`seriesId=${seriesId}, episodeIds=[${episodeIds.join(', ')}] → ManualImport`);
    } else {
        Logger.ILog('No seriesId/episodeIds, Trying parsing from File/Folder name');
        const parsedSeries = parseSeries(searchPattern, sonarr)
        seriesId = parsedSeries?.id ?? null
        episodeIds = parsedSeries?.episodeIds ?? null
        Logger.ILog(
            seriesId && episodeIds
                ? `seriesId=${seriesId}, episodeIds=[${episodeIds.join(', ')}] → ManualImport`
                : 'No seriesId/episodeIds → downloadedEpisodesScan'
        );
    }
        

    /*─ Execute first workflow, then fail-over if needed ─*/
    let result;
    if (seriesId && episodeIds) {
        result = manualImportWorkflow(sonarr, ImportPath, importMode, seriesId, episodeIds, TimeOut);
        if (result !== 1) {
            Logger.WLog('ManualImport failed, falling back to downloadedEpisodesScan.');
            result = scanWorkflow(sonarr, ImportPath, importMode, TimeOut);
        }
    } else {
        result = scanWorkflow(sonarr, ImportPath, importMode, TimeOut);
    }

    return result;
}

/*───────────────────────────── ManualImport ─────────────────────────────*/

/**
 * @description Perform ManualImport flow and wait until it finishes (or times out).
 * @param {Sonarr}  sonarr - Sonarr API instance
 * @param {string}  importPath - Folder/File path given to seires import
 * @param {string}  mode - 'move' or 'copy'
 * @param {number}  seriesId - Sonarr seriesId to attach to the import
 * @param {number}  episodeIds - Sonarr episodeIds to attach to the import
 * @param {number}  timeout - Timeout in milliseconds
 * @returns {number} 1 on success, −1 on failure
 */
function manualImportWorkflow(sonarr, path, mode, seriesId, episodeIds, timeout) {
    const candidates = getManualImportCandidates(sonarr, path);
    if (!candidates.length) {
        Logger.WLog('No candidates returned by fetching ManualImport');
        return -1;
    }

    /* choose the first candidate that has quality info */
    const cand = candidates.find(c => c.quality && c.quality.quality);
    if (!cand) {
        Logger.WLog('No ManualImport candidate contained quality information.');
        return -1;
    }

    const fileObj = buildManualImportFile(cand, seriesId, episodeIds, path);
    const cmdBody = { name: 'ManualImport', files: [fileObj], importMode: mode };
    const cmdId   = sendManualImportCommand(sonarr, cmdBody);
    if (cmdId === null) {
        Logger.WLog('Failed sending ManualImport command');
        return -1;
    }

    return waitForCommand(sonarr, cmdId, timeout);
}

/**
 * @description Retrieve candidate files from Sonarr’s **Manual Import** analyser.
 * @param {Sonarr} sonarr - Sonarr API instance
 * @param {string} importPath - Folder/File path to inspect
 * @returns {Array<object>} Array of candidate objects (empty array on error)
 */
function getManualImportCandidates(sonarr, importPath) {
    try {
        const query = buildQueryParams({ folder: importPath });
        const resp  = sonarr.fetchJson('manualimport', query) || [];
        Logger.ILog(`ManualImport returned ${resp.length} candidate(s).`);
        return resp;
    } catch (e) {
        Logger.ELog(`Fetch candidates from ManualImport failed: ${e.message}`);
        return [];
    }
}

/**
 * Build a fully-populated file object for Sonarr ManualImport.
 */
/**
 * @description Build the minimal file-object required by Sonarr's ManualImport POST body.
 * @param {object} src – Single candidate object returned by ManualImport
 * @param {number} seriesId – Sonarr seriesId to which the file is linked
 * @param {number} episodeIds – Sonarr episodeIds to which the file is linked
 * @returns {object} File descriptor for ManualImport
 */
function buildManualImportFile(src, seriesId, episodeIds, importPath) {
    const path = src.path || importPath;
    const fallbackFolder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) ?? Variables.folder.Name;

    return {
        path: path,
        folderName: src.folderName || src.name || fallbackFolder,
        seriesId: seriesId,
        episodeIds: episodeIds,
        releaseGroup: src.releaseGroup || 'Sonarr',
        quality: src.quality,
        languages: src.languages || [ { id:0, name:'Unknown' } ],
        indexerFlags: src.indexerFlags ?? 0,
        releaseType: src.releaseType ?? 'unknown'
    };
}

/**
 * @description Send the **ManualImport** command to Sonarr and return command-id
 * @param {Sonarr} sonarr – Sonarr API instance
 * @param {object} cmdBody – Complete payload for ManualImport command
 * @returns {number|null} Command id on success, null on failure
 */
function sendManualImportCommand(sonarr, cmdBody) {
    try {
        const resp = sonarr.sendCommand('ManualImport', cmdBody);
        const cmdId   = resp?.id;
        Logger.ILog(cmdId ? `ManualImport queued (cmdId=${cmdId}).` : 'ManualImport failed.');
        return cmdId ?? null;
    } catch (e) {
        Logger.ELog(`Send command ManualImport failed: ${e.message}`);
        return null;
    }
}

/*──────────────────────── downloadedEpisodesScan branch ─────────────────────*/

/**
 * @description Perform the downloadedEpisodesScan flow (when seriesId is absent).
 * @param {Sonarr}  sonarr – Sonarr API instance
 * @param {string}  importPath – Folder/File path for downloadedEpisodesScan
 * @param {string}  mode – 'move' or 'copy'
 * @param {number}  timeout – Timeout in milliseconds
 * @returns {number} 1 on success, −1 on failure
 */
function scanWorkflow(sonarr, importPath, mode, timeout) {
    const cmdId = sendDownloadedEpisodesScan(sonarr, importPath, mode);
    if (cmdId === null) {
        Logger.WLog('Faild sending downloadedEpisodesScan command');
        return -1;
    }

    return waitForCommand(sonarr, cmdId, timeout);
}

/**
 * @description Send the **downloadedEpisodesScan** command.
 * @param {Sonarr} sonarr - Sonarr API instance
 * @param {string} importPath - Folder/File path for the scan
 * @param {string} mode  - 'move' or 'copy'
 * @returns {number|null} command id or null
 */
function sendDownloadedEpisodesScan(sonarr, importPath, mode) {
    try {
        const resp = sonarr.sendCommand('downloadedEpisodesScan', { path: importPath, importMode: mode });
        const cmdId   = resp?.id;
        Logger.ILog(cmdId ? `downloadedEpisodesScan queued (cmdId=${cmdId}).` : 'downloadedEpisodesScan failed.');
        return cmdId ?? null;
    } catch (e) {
        Logger.ELog(`Send command downloadedEpisodesScan failed: ${e.message}`);
        return null;
    }
}

/*────────────────────────────── helpers ──────────────────────────────────*/

/**
 * @description Poll Sonarr until the command id completes or timeout is reached.
 * @param {Sonarr} sonarr - Sonarr API instance
 * @param {number}  cmdId - Command id returned by sendCommand
 * @param {number}  timeoutMs - Maximum time to wait in milliseconds
 * @returns {number} 1 on success, −1 on timeout
 */
function waitForCommand(sonarr, cmdId, timeoutMs) {
    const ok = sonarr.waitForCompletion(cmdId, timeoutMs);
    if (ok) {
        Logger.DLog(`Command ${cmdId} completed.`);
        Logger.ILog('Import completed successfully');
        return 1;
    }
    Logger.WLog('Import timed out or failed.');
    return -1;
}

/**
 * @description Parse the series name using Sonarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Parsed Series object, or null if none.
 */
function parseSeries(searchPattern, sonarr) {
    let endpoint = 'parse'
    let sp = null;

    Logger.ILog(`Trying to Parse Series name using Sonarr Parsing`);;

    if (!searchPattern) {
        Logger.WLog('No pattern passed in to find series');
        return null;
    } else {
        sp = searchPattern.toLowerCase();
    }

    try {
        const queryParams = buildQueryParams({ title: sp });
        const item = sonarr.fetchJson(endpoint, queryParams);

        if (item?.series?.title) {
            if (item.episodes) {
                item.series.episodeIds = item.episodes.map(ep => ep.id);
            }

            return item.series;
        }
        Logger.WLog(`The ${endpoint} endpoint did not recognise this title.`);
        return null;
    } catch (e) {
        Logger.ELog(`Error fetching Sonarr ${endpoint} endpoint: ${e.message}`);
        return null;
    }
}

/**
 * @description Constructs a query string from the given parameters
 * @param {Object} params - Key-value pairs to be converted into a query string
 * @returns {string} The constructed query string
 */
function buildQueryParams(obj) {
    return Object.entries(obj)
        .map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`)
        .join('&');
}

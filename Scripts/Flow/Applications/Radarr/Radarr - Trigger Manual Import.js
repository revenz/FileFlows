import { Radarr } from 'Shared/Radarr';

/**
 * @name Radarr - Trigger Manual Import
 * @uid 0e522a46-ed76-4b40-bd1f-b3baac64264c
 * @description Trigger Radarr to manually import the media.
 * @help Performs Radarr import to the Movie
 * Run last after file move.
 * @author iBuSH
 * @revision 7
 * @param {string} URL Radarr root URL and port (e.g. http://radarr:7878).
 * @param {string} ApiKey Radarr API Key.
 * @param {string} ImportPath The output path for import triggering (default Working File).
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows).
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy).
 * @param {int} TimeOut Set in seconds the timeout for waiting completion (default 60 seconds, max 3600 seconds).
 * @output Command sent
 */
function Script(URL, ApiKey, ImportPath, UseUnmappedPath, MoveMode, TimeOut) {
    URL = (URL || Variables['Radarr.Url'] || Variables['Radarr.URI']).replace(/\/+$/g, '');
    ApiKey = ApiKey || Variables['Radarr.ApiKey'];
    ImportPath = ImportPath || Variables.file.FullName;
    TimeOut = TimeOut ? Math.min(TimeOut, 3600) * 1000 : 60000;  // ms
    ImportPath = UseUnmappedPath ? Flow.UnMapPath(ImportPath) : ImportPath;
    const importMode = MoveMode ? 'move' : 'copy';

    const radarr = new Radarr(URL, ApiKey);

    /*── movieId detection ──────────────────────────────────*/
    const searchPattern = Variables.file.Orig.FileNameNoExtension;
    let movieId = Variables['Radarr.movieId'] ?? Variables.MovieInfo.id ?? parseMovie(searchPattern, radarr) ?? null;

    Logger.ILog(`Radarr URL: ${URL}`);
    Logger.ILog(`Triggering Path: ${ImportPath}`);
    Logger.ILog(`Import Mode: ${importMode}`);
    Logger.ILog(movieId ? `movieId: ${movieId} → ManualImport` : 'No movieId → downloadedMoviesScan');

    /*─ Execute first workflow, then fail-over if needed ─*/
    let result;
    if (movieId) {
        result = manualImportWorkflow(radarr, ImportPath, importMode, movieId, TimeOut);
        if (result !== 1) {
            Logger.WLog('ManualImport failed, falling back to downloadedMoviesScan.');
            result = scanWorkflow(radarr, ImportPath, importMode, TimeOut);
        }
    } else {
        result = scanWorkflow(radarr, ImportPath, importMode, TimeOut);
    }

    return result;

}

/*───────────────────────────── ManualImport ─────────────────────────────*/

/**
 * @description Perform ManualImport flow (when movieId is present) and wait until it finishes (or times out).
 * @param {Radarr}  radarr - Radarr API instance
 * @param {string}  importPath - Folder/File path given to movie import
 * @param {string}  mode - 'move' or 'copy'
 * @param {number}  movieId - Radarr movieId to attach to the import
 * @param {number}  timeout - Timeout in milliseconds
 * @returns {number} 1 on success, −1 on failure
 */
function manualImportWorkflow(radarr, importPath, mode, movieId, timeout) {
    const candidates = getManualImportCandidates(radarr, importPath);
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

    const fileObj = buildManualImportFile(cand, movieId, importPath);
    const cmdBody = { name: 'ManualImport', files: [fileObj], importMode: mode };
    const cmdId = sendManualImportCommand(radarr, cmdBody);
    if (cmdId === null) {
        Logger.WLog('Failed sending ManualImport command');
        return -1;
    }

    return waitForCommand(radarr, cmdId, timeout);
}

/**
 * @description Retrieve candidate files from Radarr’s **Manual Import** analyser.
 * @param {Radarr} radarr – Radarr API instance
 * @param {string} importPath – Folder/File path to inspect
 * @returns {Array<object>} Array of candidate objects (empty array on error)
 */
function getManualImportCandidates(radarr, importPath) {
    try {
        const query = buildQueryParams({ folder: importPath });
        const resp = radarr.fetchJson('manualimport', query) || [];
        Logger.ILog(`ManualImport returned ${resp.length} candidate(s).`);
        return resp;
    } catch (e) {
        Logger.ELog(`Fetch candidates from ManualImport failed: ${e.message}`);
        return [];
    }
}

/**
 * @description Build the minimal file-object required by Radarr's ManualImport POST body.
 * @param {object} src – Single candidate object returned by ManualImport
 * @param {number} movieId – Radarr movieId to which the file is linked
 * @returns {object} File descriptor for ManualImport
 */
function buildManualImportFile(src, movieId, importPath) {
    const path =  src.path || importPath
    const fallbackFolder = System.IO.Path.GetFileName(System.IO.Path.GetDirectoryName(path)) ?? Variables.folder.Name;

    return {
        path: path,
        folderName: src.folderName || src.name || fallbackFolder,
        movieId: movieId,
        releaseGroup: src.releaseGroup || 'Radarr',
        quality: src.quality,
        languages: src.languages || [ { id:0, name:'Unknown' } ],
        indexerFlags: src.indexerFlags ?? 0
    };
}

/**
 * @description Send the **ManualImport** command to Radarr and return command-id
 * @param {Radarr} radarr – Radarr API instance
 * @param {object} cmdBody – Complete payload for ManualImport command
 * @returns {number|null} Command id on success, null on failure
 */
function sendManualImportCommand(radarr, cmdBody) {
    try {
        const resp = radarr.sendCommand('ManualImport', cmdBody);
        const cmdId = resp?.id;
        Logger.ILog(cmdId ? `ManualImport queued (cmdId=${cmdId}).` : 'ManualImport failed.');
        return cmdId ?? null;
    } catch (e) {
        Logger.ELog(`Send command ManualImport failed: ${e.message}`);
        return null;
    }
}

/*──────────────────────────── downloadedMoviesScan ────────────────────────────*/

/**
 * @description Perform the downloadedMoviesScan flow (when movieId is absent).
 * @param {Radarr}  radarr – Radarr API instance
 * @param {string}  importPath – Folder/File path for downloadedMoviesScan
 * @param {string}  mode – 'move' or 'copy'
 * @param {number}  timeout – Timeout in milliseconds
 * @returns {number} 1 on success, −1 on failure
 */
function scanWorkflow(radarr, importPath, mode, timeout) {
    const cmdId = sendDownloadedMoviesScan(radarr, importPath, mode);
    if (cmdId === null) {
        Logger.WLog('Faild sending downloadedMoviesScan command');
        return -1;
    }

    return waitForCommand(radarr, cmdId, timeout);
}

/**
 * @description Send the **downloadedMoviesScan** command.
 * @param {Radarr} radarr - Radarr API instance
 * @param {string} importPath - Folder/File path for the scan
 * @param {string} mode  - 'move' or 'copy'
 * @returns {number|null} command id or null
 */
function sendDownloadedMoviesScan(radarr, importPath, mode) {
    try {
        const resp = radarr.sendCommand('downloadedMoviesScan', { path: importPath, importMode: mode });
        const cmdId = resp?.id;
        Logger.ILog(cmdId ? `downloadedMoviesScan queued (cmdId=${cmdId}).` : 'downloadedMoviesScan failed.');
        return cmdId ?? null;
    } catch (e) {
        Logger.ELog(`Send command downloadedMoviesScan failed: ${e.message}`);
        return null;
    }
}

/*────────────────────────────── helpers ──────────────────────────────────*/

/**
 * @description Poll Radarr until the command id completes or timeout is reached.
 * @param {Radarr} radarr - Radarr API instance
 * @param {number}  cmdId - Command id returned by sendCommand
 * @param {number}  timeoutMs - Maximum time to wait in milliseconds
 * @returns {number} 1 on success, −1 on timeout
 */
function waitForCommand(radarr, cmdId, timeoutMs) {
    const ok = radarr.waitForCompletion(cmdId, timeoutMs);
    if (ok) {
        Logger.DLog(`Command ${cmdId} completed.`);
        Logger.ILog('Import completed successfully');
        return 1;
    }
    Logger.WLog('Import timed out or failed.');
    return -1;
}

/**
 * @description Parse the movie name using Radarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Parsed movie object, or null if none.
 */
function parseMovie(searchPattern, radarr) {
    let endpoint = 'parse'
    let sp = null;

    Logger.ILog(`Trying to Parse Movie name using Radarr Parsing`);

    if (!searchPattern) {
        Logger.WLog('No pattern passed in to find movie');
        return null;
    } else {
        sp = searchPattern.toLowerCase();
    }

    try {
        const queryParams = buildQueryParams({ title: sp });
        const item = radarr.fetchJson(endpoint, queryParams);

        if (item?.movie?.title) {
            Logger.ILog(`Found Movie: ${item.movie.title}`);
            return item.movie.id;
        }
        Logger.WLog(`The ${endpoint} endpoint did not recognise this title.`);
        return null;
    } catch (e) {
        Logger.ELog(`Error fetching Radarr ${endpoint} endpoint: ${e.message}`);
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

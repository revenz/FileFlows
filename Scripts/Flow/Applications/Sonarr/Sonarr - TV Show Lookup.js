import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - TV Show Lookup
 * @description This script looks up a TV Show from Sonarr and retrieves its metadata
 * @author iBuSH
 * @uid 9f25c573-1c3c-4a1e-8429-5f1fc69fc6d8
 * @revision 6
 * @param {string} URL Sonarr root URL and port (e.g., http://sonarr:1234)
 * @param {string} ApiKey API Key for Sonarr
 * @param {bool} UseFolderName Whether to use the folder name instead of the file name for the search pattern.<br>If the folder starts with "Season", "Staffel", "Saison", or "Specials", the parent folder will be used.
 * @output TV Show found
 * @output TV Show NOT found
 */
function Script(URL, ApiKey, UseFolderName) {
    URL = URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI'];
    ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
    const sonarr = new Sonarr(URL, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const searchPattern = UseFolderName ? getSeriesFolderName(folderPath) : Variables.file.Orig.FileNameNoExtension;

    Logger.ILog(`Sonarr URL: ${URL}`);
    Logger.ILog(`Lookup TV Show: ${searchPattern}`);

    // Search for the series in Sonarr by path, queue, or download history
    let series = searchSeriesByPath(searchPattern, sonarr) ||
                 searchInQueue(searchPattern, sonarr) ||
                 searchInDownloadHistory(searchPattern, sonarr) ||
                 searchInGrabHistory(searchPattern, sonarr) ||
                 parseSeriesName(searchPattern, sonarr);

    if (!series) {
        Logger.ILog(`No result found for: ${searchPattern}`);
        return 2; // TV Show not found
    }

    updateSeriesMetadata(series);
    return 1; // TV Show found
}

/**
 * @description Updates the series metadata in the global variables based on the Sonarr series data
 * @param {Object} series - Series object returned from Sonarr API
 */
function updateSeriesMetadata(series) {
    const lang = LanguageHelper.GetIso2Code(series.originalLanguage.name);

    Variables["tvshow.Title"] = series.title;
    Logger.ILog(`Detected Title: ${series.title}`);
    Variables["tvshow.Year"] = series.year;
    Logger.ILog(`Detected Year: ${series.year}`);

    // Extract the url of the poster image
    const poster = series.images?.find(image => image.coverType === 'poster');
    if (poster && poster.remoteUrl) {
        Variables["tvshow.PosterUrl"] = poster.remoteUrl;
        Logger.ILog(`Detected Poster URL: ${poster.remoteUrl}`);
        Flow.SetThumbnail(poster.remoteUrl); // Set the FileFlows Thumbnail
    } else {
        Logger.WLog("No poster image found.");
    }

    Variables.VideoMetadata = {
        Title: series.title,
        Description: series.overview,
        Year: series.year,
        ReleaseDate: series.firstAired,
        OriginalLanguage: lang,
        Genres: series.genres
    };

    Variables.TVShowInfo = series;
    Variables.OriginalLanguage = lang;
    Logger.ILog(`Detected Original Language: ${lang}`);
}

/**
 * @description Extracts the folder name from the provided folder path.
 * * If the folder name contains keywords like Season, Staffel, Saison, or Specials, it uses the parent folder.
 * @param {string} folderPath - The full path of the folder
 * @returns {string} The folder name
 */
function getSeriesFolderName(folderPath) {
    const regex = /(Season|Staffel|Saison|Specials)/i;

    if (regex.test(folderPath)) {
        folderPath = System.IO.Path.GetDirectoryName(folderPath);
    }

    return System.IO.Path.GetFileName(folderPath);
}

/**
 * @description Searches for a series by file or folder path in Sonarr
 * @param {string} searchPattern - The search string to use (from the folder or file name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Series object if found, or null if not found
 */
function searchSeriesByPath(searchPattern, sonarr) {
    Logger.ILog(`Searching by Series path`);

    try {
        const series = sonarr.getShowByPath(searchPattern);
        return series || null;
    } catch (error) {
        Logger.ELog(`Error searching series by path: ${error.message}`);
        return null;
    }
}

/**
 * @description Parse the series name using Sonarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Parsed Series object, or null if none.
 */
function parseSeriesName(searchPattern, sonarr) {
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
        const queryParams   = buildQueryParams({ title: sp });
        const item = sonarr.fetchJson(endpoint, queryParams);

        if (item?.series?.title) {
            Logger.ILog(`Found TV Show: ${item.series.title}`);
            return item.series;
        }
        Logger.WLog(`The ${endpoint} endpoint did not recognise this title.`);
        return null;
    } catch (error) {
        Logger.ELog(`Error fetching Sonarr ${endpoint} endpoint: ${error.message}`);
        return null;
    }
}

/**
 * @description Searches the Sonarr queue for a series based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Series object if found, or null if not found
 */
function searchInQueue(searchPattern, sonarr) {
    Logger.ILog(`Searching in Queue`);

    return searchSonarrAPI('queue', searchPattern, sonarr, (item, sp) => {
        return item.outputPath?.toLowerCase().includes(sp);
    });
}

/**
 * @description Searches the Sonarr download history for a series based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Series object if found, or null if not found
 */
function searchInDownloadHistory(searchPattern, sonarr) {
    Logger.ILog(`Searching in Download History`);

    return searchSonarrAPI('history', searchPattern, sonarr, (item, sp) => {
        return item.data?.droppedPath?.toLowerCase().includes(sp);
    }, { eventType: 3 });   // 3 == downloaded
}

/**
 * @description Searches the Sonarr grabbed history for a series based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @returns {Object|null} Series object if found, or null if not found
 */
function searchInGrabHistory(searchPattern, sonarr) {
    Logger.ILog(`Searching in Grab History`);

    return searchSonarrAPI('history', searchPattern, sonarr, (item, sp) => {
        return item.sourceTitle?.toLowerCase().includes(sp);
    }, { eventType: 1 });   // 1 == grabbed
}

/**
 * @description Generic function to search Sonarr API (queue or history) based on a search pattern
 * @param {string} endpoint - The Sonarr API endpoint to search (queue or history)
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @param {Function} matchFunction - A function that determines if an item matches the search pattern
 * @param {Object} [extraParams={}] - Additional query parameters for the API request
 * @returns {Object|null} Series object if found, or null if not found
 */
function searchSonarrAPI(endpoint, searchPattern, sonarr, matchFunction, extraParams = {}) {
    let page = 1;
    const pageSize = 1000;
    const includeSeries = 'true';
    let sp = null;

    if (!searchPattern) {
        Logger.WLog('No pattern passed in to find TV Show');
        return null;
    } else {
        sp = searchPattern.toLowerCase();
    }

    try {
        while (true) {
            const queryParams = buildQueryParams({ page, pageSize, includeSeries, ...extraParams });
            const json = sonarr.fetchJson(endpoint, queryParams);
            const items = json.records;

            if (items.length === 0) {
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            const matchingItem = items.find(item => matchFunction(item, sp));
            if (matchingItem) {
                Logger.ILog(`Found TV Show: ${matchingItem.series.title}`);
                return matchingItem.series;
            }

            if (endpoint === 'queue') {
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            page++;
        }
    } catch (error) {
        Logger.ELog(`Error fetching Sonarr ${endpoint} endpoint: ${error.message}`);
        return null;
    }
}

/**
 * @description Constructs a query string from the given parameters
 * @param {Object} params - Key-value pairs to be converted into a query string
 * @returns {string} The constructed query string
 */
function buildQueryParams(params) {
    return Object.keys(params)
        .map(key => `${encodeURIComponent(key)}=${encodeURIComponent(params[key])}`)
        .join('&');
}
import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - TV Show Lookup
 * @description This script looks up a TV Show from Sonarr and retrieves its metadata
 * @help Performs a search on Sonarr for a TV Show.
 * Stores the Metadata inside the variable 'TVShowInfo'.
 * @author iBuSH
 * @uid 9f25c573-1c3c-4a1e-8429-5f1fc69fc6d8
 * @revision 9
 * @param {string} URL Sonarr root URL and port (e.g., http://sonarr:8989)
 * @param {string} ApiKey API Key for Sonarr
 * @param {bool} UseFolderName Whether to use the folder name instead of the file name for the search pattern.<br>- If the folder starts with "Season", "Staffel", "Saison", or "Specials", the parent folder will be used.<br>- Best option if your downlad library is the same as your media library.<br>- If lookup returning with more then 2 episodes then it will fallback to file name search pattern.
 * @output TV Show found
 * @output TV Show NOT found
 */
function Script(URL, ApiKey, UseFolderName) {
    URL = (URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI']).replace(/\/+$/g, '');
    ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
    const sonarr = new Sonarr(URL, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const fileNameNoExt = Variables.file.Orig.FileNameNoExtension;
    const searchPattern = UseFolderName ? getSeriesFolderName(folderPath) : fileNameNoExt;

    Logger.ILog(`Sonarr URL: ${URL}`);
    Logger.ILog(`Lookup TV Show: ${searchPattern}`);

    /*──────────── Primary lookup sequence ────────────*/
    let series = (UseFolderName && searchSeriesByPath(searchPattern, sonarr)) ||
                 searchInQueue(searchPattern, sonarr) ||
                 searchInGrabHistory(searchPattern, sonarr) ||
                 searchInDownloadHistory(searchPattern, sonarr) ||
                 (!UseFolderName && parseSeries(searchPattern, sonarr));    // skip parse if folder-name search

    /*──────────── Secondary refinement ───────────────*/
    if (series && series.EpisodesInfo && series.EpisodesInfo.length > 1) {
        Logger.WLog(`More than two episodes detected (${series.EpisodesInfo.length}). Refining match...`);

        /*───────── Parse filename to know which episodes we expect ─────────*/
        const parsedSeries = parseSeries(fileNameNoExt, sonarr, true);
        const wantedSeason   = parsedSeries?.parsedEpisodeInfo?.seasonNumber ?? null;
        const wantedEpisodes = parsedSeries?.parsedEpisodeInfo?.episodeNumbers ?? [];

        /*──────── Keep only the matched episodes ──────────────────────────*/
        if (wantedSeason !== null && wantedEpisodes.length) {
            const matched = (series.EpisodesInfo || []).filter(ep => 
                ep.seasonNumber === wantedSeason &&
                wantedEpisodes.includes(ep.episodeNumber)
            );

            series.EpisodesInfo = matched;          // Overwrite with matched only
            Logger.ILog(
                matched.length
                    ? `Retained episodes after match: [ ${matched.map(e => `S${e.seasonNumber}E${e.episodeNumber}`).join(', ')} ]`
                    : 'No matching episodes retained after refinement.'
            );
        }
    }

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
    const isoLang  = LanguageHelper.GetIso2Code(series.originalLanguage?.name);

    Object.assign(Variables, {
        'tvshow.Title': series.title,
        'tvshow.Year': series.year,
        'Sonarr.seriesId': series.id ?? null,
        'Sonarr.episodeIds': series.EpisodesInfo?.map(ep => ep.id) ?? [],
        TVShowInfo: series,
        OriginalLanguage: isoLang,
        VideoMetadata: {
            Title: series.title,
            Description: series.overview,
            Year: series.year,
            ReleaseDate: series.firstAired,
            OriginalLanguage: isoLang,
            Genres: series.genres
        }
    });

    Logger.ILog(`Detected Title: ${series.title}`);
    Logger.ILog(`Detected Year: ${series.year}`);
    Variables["Sonarr.seriesId"] = series.id ?? null;
    Variables["Sonarr.episodeIds"] = series.EpisodesInfo && series.EpisodesInfo.length ? series.EpisodesInfo.map(ep => ep.id) : [];
    
    Logger.ILog(`Detected Original Language: ${isoLang }`);
    Logger.ILog(
        series.EpisodesInfo && series.EpisodesInfo.length
            ? `Detected Sonarr seriesId: ${series.id} - episodeIds gathered (${series.EpisodesInfo.length}): [ ${series.EpisodesInfo.map(e => e.id).join(', ')} ]`
            : `Detected Sonarr seriesId: ${series.id} (no episodeIds)`
    );

    // Extract the url of the poster image
    const poster = series.images?.find(image => image.coverType === 'poster');
    if (poster && poster.remoteUrl) {
        Variables["tvshow.PosterUrl"] = poster.remoteUrl;
        Flow.SetThumbnail(poster.remoteUrl); // Set the FileFlows Thumbnail
    } else {
        Logger.WLog("No poster image found.");
    }
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
    } catch (e) {
        Logger.ELog(`Error searching series by path: ${e.message}`);
        return null;
    }
}

/**
 * @description Parse the series name using Sonarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @param {bool} fullOutput - Get full output or only the series data
 * @returns {Object|null} Parsed Series object, or null if none.
 */
function parseSeries(searchPattern, sonarr, fullOutput=false) {
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
                item.series.EpisodesInfo = item.episodes;
            }

            return fullOutput ? item : item.series;
        }
        Logger.WLog(`The ${endpoint} endpoint did not recognise this title.`);
        return null;
    } catch (e) {
        Logger.ELog(`Error fetching Sonarr ${endpoint} endpoint: ${e.message}`);
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
    const includeEpisode = 'true';
    let sp = null;

    let seriesObj = null;           // will hold first series we encounter
    let matchedPath = null;         // to ensure we join only the same file
    const episodes = [];            // collect all matching episodes here
    const seenIds = new Set();    // track episode IDs to avoid duplicates
    let stop = false;               // flag to break out early

    if (!searchPattern) {
        Logger.WLog('No pattern passed in to find TV Show');
        return null;
    } else {
        sp = searchPattern.toLowerCase();
    }

    try {
        while (!stop) {
            const queryParams = buildQueryParams({ page, pageSize, includeSeries, includeEpisode, ...extraParams });
            const json = sonarr.fetchJson(endpoint, queryParams);
            const items = json.records;

            if (items.length === 0 && !seriesObj) {
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            if (items.length === 0) {
                Logger.WLog(`Reached the end of ${endpoint} endpoint.`);
                break;
            }

            for (const item of items) {
                if (matchFunction(item, sp)) {
                    const path = item.outputPath || item.data?.droppedPath || item.sourceTitle

                    /* first hit → establish series & path lock */
                    if (!seriesObj) {
                        seriesObj = item.series;
                        matchedPath = path;
                    }

                    // ensure it belongs to the same physical file
                    if (item.series.id === seriesObj.id && path === matchedPath) {
                        if (item.episode && !seenIds.has(item.episode.id)) {
                            seenIds.add(item.episode.id);
                            episodes.push(item.episode);
                        }
                    } else {
                        stop = true;
                        break;
                    }
                }
            };

            if (endpoint === 'queue' || page === 10) {
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            page++;
        }
    } catch (e) {
        Logger.ELog(`Error fetching Sonarr ${endpoint} endpoint: ${e.message}`);
        return null;
    }

    if (!seriesObj) return null;

    if (episodes.length) seriesObj.EpisodesInfo = episodes;

    return seriesObj;
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
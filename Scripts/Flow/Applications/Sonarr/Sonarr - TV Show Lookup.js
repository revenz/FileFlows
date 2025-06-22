import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - TV Show Lookup
 * @description This script looks up a TV Show from Sonarr and retrieves its metadata
 * @help Performs a search on Sonarr for a TV Show.
 * Stores the Metadata inside the variable 'TVShowInfo'.
 * @author iBuSH
 * @uid 9f25c573-1c3c-4a1e-8429-5f1fc69fc6d8
 * @revision 7
 * @param {string} URL Sonarr root URL and port (e.g., http://sonarr:8989)
 * @param {string} ApiKey API Key for Sonarr
 * @param {bool} UseFolderName Whether to use the folder name instead of the file name for the search pattern.<br>If the folder starts with "Season", "Staffel", "Saison", or "Specials", the parent folder will be used.<br>If lookup returning with more then 2 episodes then it will fallback to file name search pattern.
 * @output TV Show found
 * @output TV Show NOT found
 */
function Script(URL, ApiKey, UseFolderName) {
    URL = URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI'];
    ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
    const sonarr = new Sonarr(URL, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const searchPattern = UseFolderName ? getSeriesFolderName(folderPath) : Variables.file.Orig.FileNameNoExtension;
    const fileNameNoExt = Variables.file.Orig.FileNameNoExtension;

    Logger.ILog(`Sonarr URL: ${URL}`);
    Logger.ILog(`Lookup TV Show: ${searchPattern}`);

    /*──────────── Primary lookup sequence ────────────*/
    let series = searchInQueue(searchPattern, sonarr) ||
                 searchInGrabHistory(searchPattern, sonarr) ||
                 searchInDownloadHistory(searchPattern, sonarr) ||
                 (!UseFolderName && parseSeries(searchPattern, sonarr));

    /*──────────── Secondary refinement ───────────────*/
    if (series && series.EpisodesInfo && series.EpisodesInfo.length > 1) {
        Logger.WLog(
            `More than two episodes detected (${series.EpisodesInfo.length}). `
          + 'Refining to keeps only the episodes that match the season/episode numbers parsed from the file name.'
        );

        /*───────── Parse filename to know which episodes we expect ─────────*/
        const fileNameNoExt = Variables.file.Orig.FileNameNoExtension;
        const parsedSeries = parseSeries(fileNameNoExt, sonarr, true);
        const wantedSeason   = parsedSeries?.parsedEpisodeInfo?.seasonNumber ?? null;
        const wantedEpisodes = parsedSeries?.parsedEpisodeInfo?.episodeNumbers ?? [];

        /*──────── Keep only the matched episodes ──────────────────────────*/
        if (wantedSeason !== null && wantedEpisodes.length) {
            const matched = (series.EpisodesInfo || []).filter(
                ep =>
                    ep.seasonNumber === wantedSeason &&
                    wantedEpisodes.includes(ep.episodeNumber)
            );

            series.EpisodesInfo = matched;          // Overwrite with matched only

            Logger.ILog(
                matched.length
                    ? `Episodes retained after match: [ ${matched.map(e => `S${e.seasonNumber}E${e.episodeNumber}`).join(', ')} ]`
                    : 'No matching episodes retained.'
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
    const lang = LanguageHelper.GetIso2Code(series.originalLanguage.name);

    Variables["tvshow.Title"] = series.title;
    Logger.ILog(`Detected Title: ${series.title}`);
    Variables["tvshow.Year"] = series.year;
    Logger.ILog(`Detected Year: ${series.year}`);

    Variables.VideoMetadata = {
        Title: series.title,
        Description: series.overview,
        Year: series.year,
        ReleaseDate: series.firstAired,
        OriginalLanguage: lang,
        Genres: series.genres
    };

    Variables["Sonarr.seriesId"] = series.id ?? null;
    Variables["Sonarr.episodeIds"] = series.EpisodesInfo && series.EpisodesInfo.length ? series.EpisodesInfo.map(ep => ep.id) : [];
    
    Variables.TVShowInfo = series;
    Variables.OriginalLanguage = lang;
    Logger.ILog(`Detected Original Language: ${lang}`);
    Logger.ILog(
        series.EpisodesInfo.length
            ? `Found TV Show: ${series.title} (id=${series.id}) - episodes gathered: ${series.EpisodesInfo.length} [ ${series.EpisodesInfo.map(e => e.id).join(', ')} ]`
            : `Found TV Show: ${series.title} (id=${series.id}) (no episode info)`
    );

    // Extract the url of the poster image
    const poster = series.images?.find(image => image.coverType === 'poster');
    if (poster && poster.remoteUrl) {
        Variables["tvshow.PosterUrl"] = poster.remoteUrl;
        Logger.ILog(`Detected Poster URL: ${poster.remoteUrl}`);
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
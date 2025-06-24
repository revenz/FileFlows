import { Sonarr } from 'Shared/Sonarr';

/**
 * @description This script looks up a TV Show from Sonarr and retrieves its metadata by its library path.
 * @help Performs a search on Sonarr for a TV Show using its full path in the library.
 Stores the Metadata inside the variable 'TVShowInfo'.
 * @author allquiet
 * @revision 1
 * @param {string} URL Sonarr root URL and port (e.g., http://sonarr:8989)
 * @param {string} ApiKey API Key for Sonarr
 * @output TV Show found
 * @output TV Show NOT found
 */
function Script(URL, ApiKey) {
    URL = (URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI']).replace(/\/+$/g, '');
    ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
    const sonarr = new Sonarr(URL, ApiKey);

    // Variables.folder.Orig.FullName typically gives the full path to the season folder (e.g., /data/TV Shows/My Show/Season 01)
    // We need the root series folder path for the Sonarr series lookup.
    // The `getSeriesFolderName` function you provided is designed to get the *name* of the series folder.
    // We need the *full path* to the series folder.
    let seriesRootPath = Variables.folder.Orig.FullName;

    // Use getSeriesFolderName's logic to potentially go up a directory if it's a season folder.
    // This is crucial because Sonarr stores the root series path.
    const regex = /(Season|Staffel|Saison|Specials)/i;
    if (regex.test(System.IO.Path.GetFileName(seriesRootPath))) {
        seriesRootPath = System.IO.Path.GetDirectoryName(seriesRootPath);
    }

    Logger.ILog(`Sonarr URL: ${URL}`);
    Logger.ILog(`Looking up TV Show by path: ${seriesRootPath}`);

    /*──────────── Primary lookup sequence ────────────*/
    // Directly search by the series root path.
    let series = searchByPath(seriesRootPath, sonarr);

    /*──────────── Secondary refinement ───────────────*/
    // Refine episode information if multiple are detected,
    // which can happen if the original file contains multiple episodes.
    if (series && series.EpisodesInfo && series.EpisodesInfo.length > 1) {
        Logger.WLog(`More than two episodes detected (${series.EpisodesInfo.length}). Refining match...`);

        /*───────── Parse filename to know which episodes we expect ─────────*/
        const fileNameNoExt = Variables.file.Orig.FileNameNoExtension;
        const parsedSeries = parseSeries(fileNameNoExt, sonarr, true);
        const wantedSeason = parsedSeries?.parsedEpisodeInfo?.seasonNumber ?? null;
        const wantedEpisodes = parsedSeries?.parsedEpisodeInfo?.episodeNumbers ?? [];

        /*──────── Keep only the matched episodes ──────────────────────────*/
        if (wantedSeason !== null && wantedEpisodes.length) {
            const matched = (series.EpisodesInfo || []).filter(ep =>
                ep.seasonNumber === wantedSeason &&
                wantedEpisodes.includes(ep.episodeNumber)
            );

            series.EpisodesInfo = matched;     // Overwrite with matched only
            Logger.ILog(
                matched.length
                    ? `Retained episodes after match: [ ${matched.map(e => `S${e.seasonNumber}E${e.episodeNumber}`).join(', ')} ]`
                    : 'No matching episodes retained after refinement.'
            );
        }
    }

    if (!series) {
        Logger.ILog(`No result found for path: ${seriesRootPath}`);
        return 2; // TV Show not found
    }

    updateSeriesMetadata(series);
    return 1; // TV Show found
}

/**
 * @description Searches all Sonarr series for a match based on the provided series root path.
 * @param {string} seriesPath - The full path of the series directory in the library.
 * @param {Object} sonarr - Sonarr API instance.
 * @returns {Object|null} Series object if found, or null if not found.
 */
function searchByPath(seriesPath, sonarr) {
    Logger.ILog(`Searching all series by path: ${seriesPath}`);
    try {
        // Fetch all series from Sonarr
        const allSeries = sonarr.fetchJson('series', ''); // No query parameters needed for all series

        if (!allSeries || allSeries.length === 0) {
            Logger.WLog('No series found in Sonarr.');
            return null;
        }

        // Normalize the search path for comparison (e.g., ensure consistent slashes and trailing slash)
        const normalizedSearchPath = normalizePath(seriesPath);

        for (const s of allSeries) {
            if (s.path) {
                const normalizedSonarrPath = normalizePath(s.path);
                // Compare normalized paths directly
                if (normalizedSonarrPath === normalizedSearchPath) {
                    Logger.ILog(`Found series "${s.title}" by path: ${s.path}`);

                    // Fetch episodes for the found series.
                    // This is necessary because the /api/v3/series endpoint doesn't include episode details.
                    // We need to get episodes to fill `EpisodesInfo` for downstream logic.
                    const episodeQueryParams = buildQueryParams({ seriesId: s.id });
                    const episodes = sonarr.fetchJson('episode', episodeQueryParams);
                    s.EpisodesInfo = episodes || []; // Attach episodes to the series object

                    return s;
                }
            }
        }
        Logger.ILog(`No series found matching path: ${seriesPath}`);
        return null;
    } catch (e) {
        Logger.ELog(`Error fetching Sonarr series: ${e.message}`);
        return null;
    }
}

/**
 * @description Normalizes a given path for consistent comparison.
 * Converts backslashes to forward slashes and ensures a trailing slash.
 * @param {string} path - The path to normalize.
 * @returns {string} The normalized path.
 */
function normalizePath(path) {
    if (!path) return '';
    let normalized = path.replace(/\\/g, '/'); // Convert backslashes to forward slashes
    if (!normalized.endsWith('/')) {
        normalized += '/'; // Ensure trailing slash
    }
    return normalized.toLowerCase(); // Convert to lowercase for case-insensitive comparison
}


/**
 * @description Updates the series metadata in the global variables based on the Sonarr series data
 * @param {Object} series - Series object returned from Sonarr API
 */
function updateSeriesMetadata(series) {
    const isoLang = LanguageHelper.GetIso2Code(series.originalLanguage?.name);

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

    Logger.ILog(`Detected Original Language: ${isoLang}`);
    Logger.ILog(
        series.EpisodesInfo.length
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
 * @description Parse the series name using Sonarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} sonarr - Sonarr API instance
 * @param {bool} fullOutput - Get full output or only the series data
 * @returns {Object|null} Parsed Series object, or null if none.
 */
function parseSeries(searchPattern, sonarr, fullOutput = false) {
    let endpoint = 'parse'
    let sp = null;

    Logger.ILog(`Trying to Parse Series name using Sonarr Parsing`);

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
 * @description Constructs a query string from the given parameters
 * @param {Object} params - Key-value pairs to be converted into a query string
 * @returns {string} The constructed query string
 */
function buildQueryParams(params) {
    return Object.keys(params)
        .map(key => `${encodeURIComponent(key)}=${encodeURIComponent(params[key])}`)
        .join('&');
}
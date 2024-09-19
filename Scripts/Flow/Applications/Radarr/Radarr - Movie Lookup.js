import { Radarr } from 'Shared/Radarr';

/**
 * @name Radarr - Movie Lookup
 * @description This script looks up a Movie from Radarr and retrieves its metadata
 * @author Idan Bush
 * @uid 1153e3fb-e7bb-4162-87ad-5c15cd9c081f
 * @revision 1
 * @param {string} URL Radarr root URL and port (e.g., http://radarr:1234)
 * @param {string} ApiKey API Key for Radarr
 * @param {bool} UseFolderName Whether to use the folder name instead of the file name for search
 * @output Movie found
 * @output Movie not found
 */
function Script(URL, ApiKey) {
    let URL = ((URL) ? URL : Variables['Radarr.Url']);
    let ApiKey = ((ApiKey) ? ApiKey : Variables['Radarr.ApiKey']);
    const radarr = new Radarr(URL, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const filePath = Variables.file.Orig.FullName;
    const searchPattern = UseFolderName ? getMovieFolderName(folderPath) : Variables.file.Orig.FileNameNoExtension;

    Logger.ILog(`Radarr URL: ${URL}`);
    Logger.ILog(`Lookup name: ${searchPattern}`);

    // Search for the movie in Radarr by path, queue, or download history
    let movie = searchMovieByPath(searchPattern, radarr) ||
                searchInQueue(searchPattern, radarr) ||
                searchInDownloadHistory(searchPattern, radarr);

    if (!movie) {
        Logger.ILog(`No result found for: ${searchPattern}`);
        return 2; // Movie not found
    }

    updateMovieMetadata(movie);
    return 1; // Movie found
}

/**
 * @description Updates the movie metadata in the global variables based on the Radarr movie data
 * @param {Object} movie - Movie object returned from Radarr API
 */
function updateMovieMetadata(movie) {
    const lang = LanguageHelper.GetIso2Code(movie.originalLanguage.name);

    Variables["movie.Title"] = movie.title;
    Logger.ILog(`Detected Movie Title: ${movie.title}`);
    Variables["movie.Year"] = movie.year;
    Logger.ILog(`Detected Movie Year: ${movie.year}`);

    Variables.VideoMetadata = {
        Title: movie.title,
        Description: movie.overview,
        Year: movie.year,
        ReleaseDate: movie.firstAired,
        OriginalLanguage: lang,
        Genres: movie.genres
    };

    Variables.MovieInfo = movie;
    Variables.OriginalLanguage = lang;
    Logger.ILog(`Detected Original Language: ${lang}`);
}

/**
 * @description Extracts the folder name from the provided folder path
 * @param {string} folderPath - The full path of the folder
 * @returns {string} The folder name
 */
function getMovieFolderName(folderPath) {
    return System.IO.Path.GetFileName(folderPath);
}

/**
 * @description Searches for a movie by file or folder path in Radarr
 * @param {string} searchPattern - The search string to use (from the folder or file name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchMovieByPath(searchPattern, radarr) {
    try {
        const movie = radarr.getMovieByPath(searchPattern);
        return movie || null;
    } catch (error) {
        Logger.ELog(`Error searching movie by path: ${error.message}`);
        return null;
    }
}

/**
 * @description Searches the Radarr queue for a movie based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchInQueue(searchPattern, radarr) {
    return searchRadarrAPI('queue', searchPattern, radarr, (item, sp) => {
        return item.outputPath.toLowerCase().includes(sp);
    });
}

/**
 * @description Searches the Radarr download history for a movie based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchInDownloadHistory(searchPattern, radarr) {
    return searchRadarrAPI('history', searchPattern, radarr, (item, sp) => {
        return item.data?.droppedPath?.toLowerCase().includes(sp);
    }, { eventType: 3 });
}

/**
 * @description Generic function to search Radarr API (queue or history) based on a search pattern
 * @param {string} endpoint - The Radarr API endpoint to search (queue or history)
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @param {Function} matchFunction - A function that determines if an item matches the search pattern
 * @param {Object} [extraParams={}] - Additional query parameters for the API request
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchRadarrAPI(endpoint, searchPattern, radarr, matchFunction, extraParams = {}) {
    let page = 1;
    const pageSize = 1000;
    const includeMovie = 'true'
    let sp = null;

    if (!searchPattern) {
        Logger.WLog('No pattern passed in to find movie');
        return null;
    } else {
        sp = searchPattern.toLowerCase();
    }

    try {
        while (true) {
            const queryParams = buildQueryParams({ page, pageSize, includeMovie, ...extraParams });
            const json = radarr.fetchJson(endpoint, queryParams);
            const items = json.records;

            if (items.length === 0) {
                Logger.WLog(`Reached the end of ${endpoint} with no match.`);
                break;
            }

            const matchingItem = items.find(item => matchFunction(item, sp));
            if (matchingItem) {
                Logger.ILog(`Found Movie: ${matchingItem.movie.title}`);
                return matchingItem.movie;
            }

            if (endpoint === 'queue') {
                Logger.WLog(`Reached the end of ${endpoint} with no match.`);
                break;
            }

            page++;
        }
    } catch (error) {
        Logger.ELog(`Error fetching Radarr ${endpoint}: ${error.message}`);
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

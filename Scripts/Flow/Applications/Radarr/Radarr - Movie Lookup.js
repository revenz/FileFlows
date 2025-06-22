import { Radarr } from 'Shared/Radarr';

/**
 * @name Radarr - Movie Lookup
 * @description This script looks up a Movie from Radarr and retrieves its metadata
 * @help Performs a search on Radarr for a movie.
 * Stores the Metadata inside the variable 'MovieInfo'.
 * @author iBuSH
 * @uid 1153e3fb-e7bb-4162-87ad-5c15cd9c081f
 * @revision 7
 * @param {string} URL Radarr root URL and port (e.g., http://radarr:1234)
 * @param {string} ApiKey API Key for Radarr
 * @param {bool} UseFolderName Whether to use the folder name instead of the file name for search
 * @output Movie found
 * @output Movie not found
 */
function Script(URL, ApiKey, UseFolderName) {
    URL = URL || Variables['Radarr.Url'] || Variables['Radarr.URI'];
    ApiKey = ApiKey || Variables['Radarr.ApiKey'];
    const radarr = new Radarr(URL, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;
    const searchPattern = UseFolderName ? getMovieFolderName(folderPath) : Variables.file.Orig.FileNameNoExtension;

    Logger.ILog(`Radarr URL: ${URL}`);
    Logger.ILog(`Lookup Movie name: ${searchPattern}`);

    // Search for the movie in Radarr by path, queue, or download history
    let movie = searchInQueue(searchPattern, radarr) ||
                searchInGrabHistory(searchPattern, radarr) ||
                searchInDownloadHistory(searchPattern, radarr) ||
                parseMovie(searchPattern, radarr);

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

    Variables["Radarr.movieId"] = movie.id ?? null;
    Logger.ILog(`Detected movieId: ${movie.id}`);
    
    Variables.MovieInfo = movie;
    Variables.OriginalLanguage = lang;
    Logger.ILog(`Detected Original Language: ${lang}`);

    // Extract the url of the poster image
    const poster = movie.images?.find(image => image.coverType === 'poster');
    if (poster && poster.remoteUrl) {
        Variables["movie.PosterUrl"] = poster.remoteUrl;
        Logger.ILog(`Detected Poster URL: ${poster.remoteUrl}`);
        Flow.SetThumbnail(poster.remoteUrl); // Set the FileFlows Thumbnail
    } else {
        Logger.WLog("No poster image found.");
    }
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
 * @description Parse the movie name using Radarr parsing based on the search pattern.
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @param {bool} fullOutput - Get full output or only the movie data
 * @returns {Object|null} Parsed movie object, or null if none.
 */
function parseMovie(searchPattern, radarr, fullOutput=false) {
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
            
            return fullOutput ? item : item.movie;
        }
        Logger.WLog(`The ${endpoint} endpoint did not recognise this title.`);
        return null;
    } catch (error) {
        Logger.ELog(`Error fetching Radarr ${endpoint} endpoint: ${error.message}`);
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
    Logger.ILog(`Searching in Queue`);

    return searchRadarrAPI('queue', searchPattern, radarr, (item, sp) => {
        return item.outputPath?.toLowerCase().includes(sp);
    });
}

/**
 * @description Searches the Radarr download history for a movie based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchInDownloadHistory(searchPattern, radarr) {
    Logger.ILog(`Searching in Download History`);

    return searchRadarrAPI('history', searchPattern, radarr, (item, sp) => {
        return item.data?.droppedPath?.toLowerCase().includes(sp);
    }, { eventType: 3 });   // 3 == downloaded
}

/**
 * @description Searches the Radarr grabbed history for a movie based on the search pattern
 * @param {string} searchPattern - The search string (file or folder name)
 * @param {Object} radarr - Radarr API instance
 * @returns {Object|null} Movie object if found, or null if not found
 */
function searchInGrabHistory(searchPattern, radarr) {
    Logger.ILog(`Searching in Grab History`);

    return searchRadarrAPI('history', searchPattern, radarr, (item, sp) => {
        return item.sourceTitle?.toLowerCase().includes(sp);
    }, { eventType: 1 });   // 1 == grabbed
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
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            const matchingItem = items.find(item => matchFunction(item, sp));
            if (matchingItem) {
                Logger.ILog(`Found Movie: ${matchingItem.movie.title}`);
                
                return matchingItem.movie;
            }

            if (endpoint === 'queue') {
                Logger.WLog(`Reached the end of ${endpoint} endpoint with no match.`);
                break;
            }

            page++;
        }
    } catch (error) {
        Logger.ELog(`Error fetching Radarr ${endpoint} endpoint: ${error.message}`);
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
/**
 * @name Radarr
 * @uid 88e66e7d-f835-4620-9616-9beaa4ee42dc
 * @revision 10
 * @description Class that interacts with Radarr
 * @minimumVersion 1.0.0.0
 */
export class Radarr
{
    URL;
    ApiKey;

    constructor(URL, ApiKey)
    {
        this.URL = ((URL) ? URL : Variables['Radarr.Url']);
        if (!this.URL)
            MissingVariable('Radarr.Url');
        this.ApiKey = ((ApiKey) ? ApiKey : Variables['Radarr.ApiKey']);
        if (!this.ApiKey)
            MissingVariable('Radarr.ApiKey');
    }

    getUrl(endpoint, queryParmeters)
    {
        let url = '' + this.URL;
        if (url.endsWith('/') === false)
            url += '/';
        url = `${url}api/v3/${endpoint}?apikey=${this.ApiKey}`;
        if(queryParmeters)
            url += '&' + queryParmeters;
        return url;
    }

    fetchJson(endpoint, queryParmeters)
    {
        let url = this.getUrl(endpoint, queryParmeters);
        let response = http.GetAsync(url).Result;
        let body = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            Logger.WLog('Unable to fetch: ' + url + '\n' + body);
            return null;
        }
        return JSON.parse(body);
    }

    /**
     * Gets a movie from Radarr by its file name
     * @param {string} file the file name of the movie to lookup
     * @returns {object} a movie object if found, otherwise null
     */
    getMovieByFile(file)
    {
        if (!file)
        {
            Logger.WLog('No file name passed in to find movie');
            return null;
        }
        let movies = this.fetchJson('movie');
        if (!movies?.length)
            return null;

        let cp = file.toLowerCase();
        let movie = movies.filter(x =>
        {
            let mp = x.movieFile?.relativePath;
            if (!mp)
                return false;
            return mp.split('.')[0].toLowerCase().includes(cp.split('.')[0]);
        });
        if (movie?.length)
        {
            movie = movie[0];
            Logger.ILog('Found movie: ' + movie.title);
            return movie;
        }
        Logger.WLog('Unable to find movie file name: ' + file);
        return null;
    }

    /**
     * Gets a movie from Radarr by its path
     * @param {string} path the path of the movie to lookup
     * @returns {object} a movie object if found, otherwise null
     */
    getMovieByPath(path)
    {
        if (!path)
        {
            Logger.WLog('No path passed in to find movie');
            return null;
        }
        let movies = this.fetchJson('movie');
        if (!movies?.length)
            return null;

        let cp = path.toLowerCase();
        let movie = movies.filter(x =>
        {
            let mp = x.movieFile?.path;
            if (!mp)
                return false;
            return mp.toLowerCase().includes(cp);
        });
        if (movie?.length)
        {
            movie = movie[0];
            Logger.ILog('Found movie: ' + movie.title);
            return movie;
        }
        Logger.WLog('Unable to find movie at path: ' + path);
        return null;
    }

    /**
     * Gets the IMDb id of a movie from its full file path
     * @param {string} path the full path of the movie to lookup
     * @returns the IMDb id if found, otherwise null
     */
    getImdbIdFromPath(path)
    {
        if(!path)
            return null;
        let movie = this.getMovieByPath(path.toString());
        if (!movie)
        {
            Logger.WLog('Unable to get IMDb ID for path: ' + path);
            return null;
        }
        return movie.imdbId;
    }

    /**
     * Gets the TMDb (TheMovieDb) id of a movie from its full file path
     * @param {string} path the full path of the movie to lookup
     * @returns the TMDb id if found, otherwise null
     */
    getTMDbIdFromPath(path)
    {
        if(!path)
            return null;
        let movie = this.getMovieByPath(path.toString());
        if (!movie)
        {
            Logger.WLog('Unable to get TMDb ID for path: ' + path);
            return null;
        }
        return movie.tmdbId;
    }

    /**
     * Gets the original language of a movie from its full file path
     * @param {string} path the full path of the movie to lookup
     * @returns the original language of the movie if found, otherwise null
     */
    getOriginalLanguageFromPath(path)
    {
        if(!path)
            return null;
        let movie = this.getMovieByPath(path.toString());
        if (!movie)
        {
            Logger.WLog('Unable to get original language for path: ' + path);
            return null;
        }
        return movie.originalLanguage?.name;
    }

    /**
     * Returns movie files info for an already identified movie
     * @param {int} movieId ID of previously identified movie
     * @returns list of radarr movieFile objects
     */
    findMovieFiles(movieId) {
        let endpoint = 'moviefile';
        let queryParams = `movieId=${movieId}`;
        let response = this.fetchJson(endpoint, queryParams);
    
        Logger.ILog(`Movie found: ${movieId}`);
        return response;
    }

    /**
     * Returns files under a movie that need to be renamed
     * @param {int} movieId Previously determined ID of the movie
     * @returns list of radarr rename movie objects
     */
    fetchRenamedMovies(movieId) 
    {
        let endpoint = 'rename';
        let queryParams = `movieId=${movieId}`;
        let response = this.fetchJson(endpoint, queryParams);
        return response;
    }

    /**
     * Specifies a command for Sonarr to run. see sonarr rename script for usage
     * @param {string} commandName the name of the command to be run
     * @param {object} commandBody the body of the command to be sent
     * @returns {object} JSON of the response or null if unsuccessful
     */
    sendCommand(commandName, commandBody) {
        let endpoint = `${this.URL}/api/v3/command`;
        commandBody['name'] = commandName;
    
        let jsonData = JSON.stringify(commandBody);
        http.DefaultRequestHeaders.Add("X-API-Key", this.ApiKey);
        let response = http.PostAsync(endpoint, JsonContent(jsonData)).Result;
    
        http.DefaultRequestHeaders.Remove("X-API-Key");
    
        if (response.IsSuccessStatusCode) {
            let responseData = JSON.parse(response.Content.ReadAsStringAsync().Result);
            return responseData;
        } else {
            let error = response.Content.ReadAsStringAsync().Result;
            Logger.WLog("API error: " + error);
            return null;
        }
    }

    /**
     * Sleeps, waiting for a command to complete
     * @param {int} commandId ID of command being run
     * @param {int} timeOut time for waiting for cammand to complete before timeour, in milliseconds (30000 milliseconds is 30 seconds).
     * @returns bool whether the command ran successfully
     */
    waitForCompletion(commandId, timeOut=30000) 
    {
        const startTime = new Date().getTime();
        const timeout = isNaN(timeOut) || timeOut < 1000 ? 30000 : timeOut;
        const endpoint = `command/${commandId}`;
    
        while (new Date().getTime() - startTime <= timeout) {
            let response = this.fetchJson(endpoint, '');
            if (response.status === 'completed') {
                Logger.ILog('Command completed!');
                return true;
            } else if (response.status === 'failed') {
                Logger.WLog(`Command ${commandId} failed`)
                return false;
            }
            Logger.ILog(`Checking status: ${response.status}`);
            Sleep(1000);    // Delay before next check
        }
        Logger.WLog(`Timeout: Command ${commandId} did not complete within ${timeout / 1000} seconds.`);
        return false;
    }
}

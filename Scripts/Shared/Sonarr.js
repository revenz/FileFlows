/**
 * @name Sonarr
 * @uid 0f5836c0-d20b-4740-9824-f81b5200ec3d
 * @description Class that interacts with Sonarr
 * @revision 13
 * @minimumVersion 1.0.0.0
 */
export class Sonarr
{
    URL;
    ApiKey;

    constructor(URL, ApiKey)
    {
        this.URL = ((URL) ? URL : Variables['Sonarr.Url']);
        if (!this.URL)
            MissingVariable('Sonarr.Url');
        this.ApiKey = ((ApiKey) ? ApiKey : Variables['Sonarr.ApiKey']);
        if (!this.ApiKey)
            MissingVariable('Sonarr.ApiKey');
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
        let json = this.fetchString(url);
        if(!json)
            return null;
        return JSON.parse(json);
    }

    fetchString(url)
    {
        let response = http.GetAsync(url).Result;
        let body = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            Logger.WLog('Unable to fetch: ' + url + '\n' + body);
            return null;
        }
        return body;
    }

    /**
     * Gets all shows in Sonarr
     * @returns {object[]} a list of shows in the Sonarr
     */
    getAllShows(){
        let shows = this.fetchJson('series');
        if(!shows.length){
            Logger.WLog("No shows found");
            return [];
        }
        return shows;
    }

    /**
     * Gets a show from Sonarr by its full path
     * @param {string} path the full path of the movie to lookup
     * @returns {object} a show object if found, otherwise null
     */
    getShowByPath(path)
    {
        if (!path)
        {
            Logger.WLog('No path passed in to find show');
            return null;
        }
        let shows = this.getAllShows();
        if (!shows?.length)
            return null;

        let cp = path.toString().toLowerCase();
        let show = shows.filter(x =>
        {
            let sp = x.path.toLowerCase();
            if (!sp)
                return false;
            return sp.includes(cp);
        });
        if (show?.length === 1)
        {
            show = show[0];
            Logger.ILog('Found show: ' + show.id);
            return show;
        }
        Logger.WLog('Unable to find show file at path: ' + path);
        return null;
    }

    getFilesInShow(show){
        let files = this.fetchJson('episodefile', 'seriesId=' + show.id);
        if(!files.length){
            
            Logger.WLog("No files in show: " + show.title);
            return [];
        }
        return files;
    }

    /**
     * Gets all files in Sonarr
     * @returns {object[]} all files in the Sonarr
     */
    getAllFiles(){
        let shows = this.getAllShows();
        let files = [];
        for(let show of shows){
            let sfiles = this.getFilesInShow(show);
            if(sfiles.length){
                for(let sfile of sfiles)
                    sfile.show = show;
                files = files.concat(sfiles);
            }
        }
        Logger.ILog('Number of show files found: ' + files.length);
        return files;
    }

    /**
     * Gets a show file from Sonarr by its full path
     * @param {string} path the full path of the movie to lookup
     * @returns {object} a show file object if found, otherwise null
     */
    getShowFileByPath(path)
    {
        if (!path)
        {
            Logger.WLog('No path passed in to find show file');
            return null;
        }
        let files = this.getAllFiles();
        if (!files?.length)
            return null;

        let cp = path.toString().toLowerCase();
        let showfile = files.filter(x =>
        {
            let sp = x.path.toLowerCase();
            if (!sp)
                return false;
            return sp.includes(cp);
        });
        if (showfile?.length)
        {
            showfile = showfile[0];
            Logger.ILog('Found show file: ' + showfile.id);
            return showfile;
        }
        Logger.WLog('Unable to find show file at path: ' + path);
        return null;
    }

    /**
     * Gets the IMDb id of a show from its full file path
     * @param {string} path the full path of the show to lookup
     * @returns the IMDb id if found, otherwise null
     */
    getImdbIdFromPath(path)
    {
        if(!path)
            return null;
        let showfile = this.getShowFileByPath(path.toString());
        if (!showfile)
        {
            Logger.WLog('Unable to get IMDb ID for path: ' + path);
            return null;
        }
        return showfile.show.imdbId;
    }

    /**
     * Gets the TVDb id of a show from its full file path
     * @param {string} path the full path of the show to lookup
     * @returns the TVdb id if found, otherwise null
     */
    getTVDbIdFromPath(path)
    {
        if(!path)
            return null;
        let showfile = this.getShowFileByPath(path.toString());
        if (!showfile)
        {
            Logger.WLog('Unable to get TMDb ID for path: ' + path);
            return null;
        }
        return showfile.show.tvdbId;
    }

    /**
     * Gets the language of a show from its full file path
     * @param {string} path the full path of the show to lookup
     * @returns the language of the show if found, otherwise null
     */
    getOriginalLanguageFromPath(path)
    {
        if(!path)
            return null;
        let showfile = this.getShowFileByPath(path.toString());
        if (!showfile)
        {
            Logger.WLog('Unable to get language for path: ' + path);
            return null;
        }
        let imdbId = showfile.show.imdbId;

        let html = this.fetchString(`https://www.imdb.com/title/${imdbId}/`);
        let languages = html.match(/title-details-languages(.*?)<\/li>/);
        if(!languages)
        {
            Logger.WLog('Failed to lookup IMDb language for ' + imdbId);
            return null;
        }
        languages = languages[1];
        let language = languages.match(/primary_language=([\w]+)&/);
        if(!language)
        {
            Logger.WLog('Failed to lookup IMDb primary language for ' + imdbId);
            return null;
        }
        return language[1];
    }

    /**
         * Specifies a command for Sonarr to run. see sonarr rename script for usage
         * @param {string} commandName the name of the command to be run
         * @param {object} commandBody the body of the command to be sent
         * @returns {object} JSON of the response or null if unsuccessful
         */
    sendCommand(commandName, commandBody) 
    {
        let endpoint = `${this.URL}/api/v3/command`;
        commandBody['name'] = commandName;

        let jsonData = JSON.stringify(commandBody);
        http.DefaultRequestHeaders.Add("X-API-Key", this.ApiKey);
        let response = http.PostAsync(endpoint, JsonContent(jsonData)).Result;

        http.DefaultRequestHeaders.Remove("X-API-Key");

        if (response.IsSuccessStatusCode) {
            let responseData = JSON.parse(response.Content.ReadAsStringAsync().Result);
            Logger.ILog(`${commandName} command sent successfully`);
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

    /**
     * Fetches files Sonarr marks as able to rename
     * @param {int} seriesId ID series to fetch files for
     * @returns List of Sonarr rename objects for each file
     */
    fetchRenamedFiles(seriesId) {
        let endpoint = 'rename';
        let queryParams = `seriesId=${seriesId}`;
        let response = this.fetchJson(endpoint, queryParams);
        return response;
    }

    /**
     * Toggles 'monitored' for episodes
     * @param {list} episodeIds IDs of episodes to toggle
     * @returns Response if ran successfully otherwise null
     */
    toggleMonitored(episodeIds, monitored=true) {
        let endpoint = `${this.URL}/api/v3/episode/monitor`;
        let jsonData = JSON.stringify(
            {
                episodeIds: episodeIds,
                monitored: monitored
            }
        );
    
        http.DefaultRequestHeaders.Add("X-API-Key", this.ApiKey);
        let response = http.PutAsync(endpoint, JsonContent(jsonData)).Result;
    
        http.DefaultRequestHeaders.Remove("X-API-Key");
    
        if (response.IsSuccessStatusCode) {
            let responseData = JSON.parse(response.Content.ReadAsStringAsync().Result);
            Logger.ILog(`Monitored toggled for ${episodeIds}`);
            return responseData;
        } else {
            let error = response.Content.ReadAsStringAsync().Result;
            Logger.WLog("API error: " + error);
            return null;
        }
    }

    /**
     * Rescans all files for a series
     * @param {int} seriesId ID series to rescan
     * @returns Response of the rescan or null if unsuccessful
     */
    rescanSeries(seriesId) {
        let refreshBody = {
                seriesId: seriesId
            }
        return this.sendCommand('RescanSeries', refreshBody)
    }

    /**
     * Fetches an episode object from its file ID
     * @param {int} fileId ID of file
     * @returns Sonarr episode object
     */
    fetchEpisodeFromFileId(episodeFileId) {
        let endpoint = 'episode';
        let queryParams = `episodeFileId=${episodeFileId}`;
        let response = this.fetchJson(endpoint, queryParams);
    
        return response[0];
    }
}


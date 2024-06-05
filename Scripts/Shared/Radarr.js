// path: Scripts/Shared/Radarr.js

/**
 * Class that interacts with Radarr
 * @name Radarr
 * @revision 5
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
}
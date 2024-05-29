/**
 * Class that interacts with Radarr
 * @name Radarr
 * @revision 2
 * @minimumVersion 1.0.0.0
 */
export class Radarr
{
    URL;
    ApiKey;

    constructor()
    {
        this.URL = Variables['Radarr.Url'];
        if (!this.URL)
            MissingVariable('Radarr.Url');
        this.ApiKey = Variables['Radarr.ApiKey'];
        if (!this.ApiKey)
            MissingVariable('Radarr.ApiKey');
    }

    getUrl(endpoint)
    {
        let url = '' + this.URL;
        if (url.endsWith('/') === false)
            url += '/';
        return `${url}api/v3/${endpoint}?apikey=${this.ApiKey}`;
    }

    fetchJson(endpoint)
    {
        let url = this.getUrl(endpoint);
        let response = http.GetAsync(url).Result;
        let body = response.Content.ReadAsStringAsync().Result;
        if (!response.IsSuccessStatusCode)
        {
            Logger.WLog('Unable to fetch: ' + endpoint + '\n' + body);
            return null;
        }
        return JSON.parse(body);
    }

    /**
     * Gets a movie from Radarr by its full path
     * @param {string} path the full path of the movie to lookup
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
            return cp.includes(x.title.toLowerCase());
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
import { Radarr } from 'Shared/Radarr';
import { Language } from 'Shared/Language';

/**
 * Checks the filename against the Radarr library using the Radarr API, and gets the Movie ID. Then checks the Native Language from IMDB 
 * using the TMDB API. 
 * Doesn't need a path, just make sure you have set your Radarr API key, Radarr URL and TMDB.ApiKey in the Variables tab. 
 * Only works when working directly in the Radarr library. 
 * Sets the language in both ISO-1 and ISO-2 in Variables.NativeLanguageISO1 and Variables.NativeLanguageISO2 respectively.
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @author Grenwalls on Github, Grenwall everywhere else
 * @output Radarr language stored in NativeLanguageISO1 and NativeLanguageISO2
 * @output Could not get the language
 */
function Script() {

  var apikey = Variables['TMDB.ApiKey'];
  Logger.ILog(apikey);
  if (!apikey){
    Logger.ILog("No TMDB-key defined. Create the variable 'TMDB.ApiKey' on the Variable page and set your key.")
    return 2;
  }

try{
  const radarr = new Radarr();
  const filnamn = Variables.file.Orig.FileName.toLowerCase();
  
  const movieDataBase = radarr.fetchJson('movie');
  //Logger.ILog(movieDataBase);
  //Logger.ILog(filnamn);
  
  if (Array.isArray(movieDataBase)) {
    let matchFound = false; // Track if a match is found
    let matchedMovieId; // Store the movieId of the matched item
    for (const movieData of movieDataBase) {
      const title = movieData.movieFile.relativePath.toLowerCase();
      
      if (title === filnamn) {
        matchedMovieId = movieData.imdbId;  
        //Logger.ILog(matchedMovieId);
        matchFound = true;
        break;
      }
    }
    
    if (matchFound) {
      Logger.ILog("Match found! Movie ID: " + matchedMovieId);
      
     const url = 'http://api.themoviedb.org/3/find/' + matchedMovieId + '?external_source=imdb_id&api_key=' + apikey;
      let response = http.GetAsync(url).Result
      
      //Logger.ILog(response);
      let body = response.Content.ReadAsStringAsync().Result;
      if (!response.IsSuccessStatusCode)
        {
            Logger.ILog('Unable to fetch: ' + url);
            return 2;
        }
      const imdbdata = JSON.parse(body);
      //Logger.ILog(imdbdata);
      const originalLanguage = imdbdata.movie_results[0].original_language;

      let helper = new Language();
      let languageIso2 = helper.getIso2Code(originalLanguage);
      let languageIso1 = helper.getIso1Code(originalLanguage);
      
      Logger.ILog('Got native language in ISO2 : ' + languageIso2);
      Logger.ILog("Got native language in ISO1 : " + languageIso1);
      Variables.NativeLanguageISO1 = languageIso1;
      Variables.NativeLanguageISO2 = languageIso2;
      return 1; // Hooray, we got the language!
    } else {
      Logger.ILog("No match found.");
      return 2; // No match was found
    }
  } else {
    Logger.ILog("Invalid queue data");
     return 2; // Invalid data
  }
 } catch(error) {
    Logger.ILog("An error occurred: " + error.message);
    return 2; // Error occurred
 }
}
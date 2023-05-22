import { Radarr } from 'Shared/Radarr';
import { Language } from 'Shared/Language';

/**
 * Checks the filename against the Radarr library using the Radarr API, and gets the original language as attested by Radarr.
 * Doesn't need a path, just make sure you have set your Radarr API key and Radarr URL in the Variables tab.
 * Only works when working directly in the Radarr library. 
 * Sets the language in both ISO-1 and ISO-2 in Variables.NativeLanguageISO1 and Variables.NativeLanguageISO2 respectively.
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @author Grenwalls on Github, Grenwall everywhere else
 * @output Radarr language stored in NativeLanguageISO1 and NativeLanguageISO2
 * @output Could not get the language
 */
function Script() {
try{
  const radarr = new Radarr();
  const filnamn = Variables.file.Orig.FileName.toLowerCase();
  
  const movieDataBase = radarr.fetchJson('movie');

  
  if (Array.isArray(movieDataBase)) {
    let matchFound = false; // Track if a match is found
    let matchedMovieId; // Store the movieId of the matched item
    let originalLanguage;
    for (const movieData of movieDataBase) {
      const title = movieData.movieFile.relativePath.toLowerCase();
      
      if (title === filnamn) {
        matchedMovieId = movieData.movieFile.movieId;  // movieData.imdbId not used
        //Logger.ILog(matchedMovieId);
        originalLanguage = movieData.originalLanguage.name; // Tar språket direkt från matchen 
        //Logger.ILog('Jsonlanguage: ' + originalLanguage);
        matchFound = true;
        break;
      }
    }
    
    if (matchFound) {
      Logger.ILog("Match found! Movie ID: [ " + matchedMovieId + " ]");
      Logger.ILog("Movie language: [ " + originalLanguage + " ]");
      
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
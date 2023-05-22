import { Radarr } from 'Shared/Radarr';
import { Language } from 'Shared/Language';

/**
 * Stores ISO-639-1 in NativeLanguageISO1 and ISO-639-2 in NativeLanguageISO2 
 * Uses the Radarr API to get a list of queud up movies in Radarr, then compares the file name of the 
 * movie file to that list, and gets the movie ID. Then makes a single request for that specific movie ID and parses
 * the original language from it. Requires Radarr url and API to be set in variables tab, and for the movie files to 
 * not be renamed after Radarr requests the download. File location doesn't matter.   
 * @revision 1
 * @minimumVersion 1.0.0.0
 * @author Grenwalls on Github, Grenwall everywhere else
 * @output Language found, stored in NativeLanguageISO1 and NativeLanguageISO2
 * @output Language couldn't be matched
 */
function Script() {
try{
  const radarr = new Radarr();
  const filnamn = Variables.file.Orig.FileNameNoExtension.toLowerCase();
  

  // Fetch the queue data
  const queData = radarr.fetchJson('queue');
  //Logger.ILog(queData);

  // Check if queData has the 'records' property
  if ('records' in queData) {
    const records = queData.records;
    let matchFound = false; // Track if a match is found
    let matchedMovieId; // Store the movieId of the matched item
    for (const que of records) {
      const title = que.title.toLowerCase();
      if (title === filnamn) {
        matchedMovieId = que.movieId;
        matchFound = true;
        break;
      }
    }

    if (matchFound) {
      Logger.ILog("Match found! Movie ID: " + matchedMovieId);

      const movieData = radarr.fetchJson('movie/' + matchedMovieId);
      //Logger.ILog(movieData);

      const originalLanguage = movieData.originalLanguage.name;
      //Logger.ILog(originalLanguage); 
      

      let helper = new Language();
      let languageIso2 = helper.getIso2Code(originalLanguage);
      let languageIso1 = helper.getIso1Code(originalLanguage);
      Logger.ILog('Got native language in ISO2 : ' + languageIso2);
      Logger.ILog("Got native language in ISO1 : " + languageIso1);
      Variables.NativeLanguageISO1 = languageIso1;
      Variables.NativeLanguageISO2 = languageIso2;
      return 1; //Hooray, we got the language!
    } else {
      Logger.ILog("No match found.");
      return 2; // No match was found
      
    }
  } else {
    Logger.ILog("Invalid queue data");
    return 2; //Invalid data
  }
 }
  catch(error){
   Logger.ILog("An error occurred: " + error.message);
   return 2; // Error occurred
  }
}
import { Radarr } from '../../../Shared/Radarr';

/**
* Lookups a file in Radarr and gets its original language
* @author John Andrews 
* @revision 1
* @minimumVersion 1.0.0.0
* @param {string} Path The full file path to lookup in Radarr
* @output The language was found and stored in the variable OriginalLanguage
* @output The language was not found
*/
function Script(Path)
{
    const radarr = new Radarr();
    try
    {
        let language = radarr.getOriginalLanguageFromPath(Path);
        if(language){
            Logger.ILog('Got original language: ' + language);
            Variables.OriginalLanguage = language;
            return 1;
        }
        return 2;
    }
    catch(err)
    {
        Logger.WLog('Error in script: ' + err);
        return 2;
    }
}
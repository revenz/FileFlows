import { Sonarr } from '../../../Shared/Sonarr';


/**
 * @author reven 
 * @uid 51cf3c4f-f4a3-45e2-a083-6629397aab90
 * @revision 9
 * @description Lookups a file in Sonarr and gets its original language ISO-693-1 code for it
 * @param {string} Path The full file path to lookup in Sonarr
 * @param {bool} ISO2 If ISO-639-2 should be returned, otherwise ISO-639-1 will be used
 * @param {string} URL Sonarr root URL and port (e.g., http://sonarr:1234)
 * @param {string} ApiKey API Key for Sonarr
 * @output The language was found and stored in the variable OriginalLanguage
 * @output The language was not found
 */
function Script(Path, ISO2, URI, ApiKey)
{
    URI = URI || Variables["Sonarr.Url"] || Variables["Sonarr.URI"];
    ApiKey = ApiKey || Variables["Sonarr.ApiKey"];
    
    if(!Path)
        return 2;
    const sonarr = new Sonarr(URI, ApiKey);
    try
    {
        let language = sonarr.getOriginalLanguageFromPath(Path.toString());
        if(!language)
            return 2;
            
        language = ISO2 ? LanguageHelper.GetIso2Code(language) : LanguageHelper.GetIso1Code(language);
        Logger.ILog('Got original language: ' + language);
        Variables.OriginalLanguage = language;
        return 1;
    }
    catch(err)
    {
        Logger.WLog('Error in script: ' + err);
        return 2;
    }
}

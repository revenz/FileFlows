import { Radarr } from '../../../Shared/Radarr';

/**
 * @author reven 
 * @uid 3915f110-4b07-4e11-b7b9-50de3f5a1255
 * @description Lookups a file in Radarr and gets its original language ISO-693-1 code for it
 * @revision 7
 * @param {string} Path The full file path to lookup in Radarr
 * @param {bool} ISO2 If ISO-639-2 should be returned, otherwise ISO-639-1 will be used
 * @param {string} URL Radarr root URL and port (e.g., http://radarr:1234)
 * @param {string} ApiKey API Key for Radarr
 * @output The language was found and stored in the variable OriginalLanguage
 * @output The language was not found
 */
function Script(Path, ISO2, URI, ApiKey)
{
    URI = URI || Variables["Radarr.Url"] || Variables["Radarr.URI"];
    ApiKey = ApiKey || Variables["Radarr.ApiKey"];
    
    if(!Path)
        return 2;
    const radarr = new Radarr(URI, ApiKey);
    try
    {
        let language = radarr.getOriginalLanguageFromPath(Path.toString());
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

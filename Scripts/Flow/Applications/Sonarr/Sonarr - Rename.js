import { Sonarr } from 'Shared/Sonarr';
/**
 * This script will send a rename command to Sonarr
 * @author Shaun Agius
 * @version 1.0.0
 * @revision 3
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not found
 */
function Script(URI, ApiKey) {
    let sonarr = new Sonarr(URI, ApiKey);
    let folder = Variables.folder.FullName
    var season
    if (folder.includes("Season"))
        season = folder.indexOf('Season')-1
    else
    {
        if (folder.includes("/"))
            season = folder.lastIndexOf("/")
        else if (folder.includes("\\"))
            season = folder.lastIndexOf("\\")
    }
    let folder2 = folder.substring(0, season)
    var slash
    if (folder2.includes("/"))
        slash = folder2.lastIndexOf("/") +1
    else if (folder2.includes("\\"))
        slash = folder2.lastIndexOf("\\") +1
    let final = folder2.substring(slash)
    let series = sonarr.getShowByPath(final);
    if (!series)
        return 2;
    Logger.ILog(`Renaming ${series.title}`);
    let endpoint = `rename?seriesId=${series.id}`;
    let response = sonarr.fetchJson(endpoint);
    Logger.ILog(`Response ${response}`);
    return 1;
}
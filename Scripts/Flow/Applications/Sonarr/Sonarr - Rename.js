import { Sonarr } from 'Shared/Sonarr';
/**
 * This script will search the active queue and blocklist and research
 * For use alongside this strategy https://fileflows.com/docs/guides/sonarr-radarr
 * @author Shaun Agius
 * @version 1.0.0
 * @revision 1
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not found
 */
function Script(URI, ApiKey) {
    let sonarr = new Sonarr(URI, ApiKey);
    let series = sonarr.getShowByPath(Variables.folder.FullName);
    if (!series)
        return 2;
    Logger.ILog(`Renaming ${series.title}`);
    let endpoint = `rename?seriesId=${series.id}`;
    let response = sonarr.fetchJson(endpoint);
    Logger.ILog(`Response ${response}`);
    return 1;
}
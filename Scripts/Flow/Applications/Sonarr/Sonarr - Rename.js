import { Sonarr } from 'Shared/Sonarr';
/**
 * This script will send a rename command to Sonarr
 * @author Shaun Agius
 * @uid 5ac44abd-cfe9-4a84-904b-9424908509de
 * @version 1.0.0
 * @revision 3
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not found
 */
function Script(URI, ApiKey) {
    let sonarr = new Sonarr(URI, ApiKey);
    let rx = /([A-Za-z\(\) [0-9]*\[tvdbid\-[0-9]*\])/g
    let folder = rx.exec(Variables.folder.FullName)
    let series = sonarr.getShowByPath(folder[1]);
    if (!series)
        return 2;
    Logger.ILog(`Renaming ${series.title}`);
    let endpoint = `rename?seriesId=${series.id}`;
    let response = sonarr.fetchJson(endpoint);
    Logger.ILog(`Response ${response}`);
    return 1;
}
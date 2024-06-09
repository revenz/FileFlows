import { Sonarr } from 'Shared/Sonarr';
/**
 * @author Anthony Clerici
 * @author Shaun Agius
 * @uid 5ac44abd-cfe9-4a84-904b-9424908509de
 * @description This script will send a rename command to Sonarr
 * @revision 6
 * @version 1.0.0
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
}

// Repeatedly try finding a show by shortening the path
function findSeries(filePath, sonarr) {
    let currentPath = filePath;
    let show = null;

    while (currentPath) {
        show = sonarr.getShowByPath(currentPath);
        if (show) {
            Logger.ILog('Show found: ' + show.id);
            return show;
        }

        // If no show is found, go up 1 dir
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (currentPath === null || currentPath === "") {
            Logger.WLog('Unable to find show file at path ' + filePath);
            return null;
        }
    }
}

function fetchRenamedFiles(seriesId, sonarr) {
    let endpoint = 'rename';
    let queryParams = `seriesId=${seriesId}`;
    let response = sonarr.fetchJson(endpoint, queryParams);
    return response;
}

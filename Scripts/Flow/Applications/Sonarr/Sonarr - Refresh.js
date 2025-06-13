import { Sonarr } from 'Shared/Sonarr';

/**
 * @description This script will refresh the file through Sonarr for processing libraries in place
 * @author Shaun Agius, Anthony Clerici : Modified by Macnemarion
 * @uid 01d7fc57-2613-4e14-92bc-29b2ba259f86
 * @revision 2
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:8989)
 * @param {string} ApiKey API Key
 * @output Item refreshed successfully
 * @output Item not found
 */
function Script(URI, ApiKey) {
    //Orignally authored by Shaun Agius and Anthony Clerici as the Sonarr - Rename Script modifed to just refresh instead.
    // Remove trailing / from URI
    URI = URI.replace(/\/$/, '');
    let sonarr = new Sonarr(URI, ApiKey);
    const folderPath = Variables.folder.Orig.FullName;

    // Find series name from sonarr
    let series = findSeries(folderPath, sonarr);

    if (series?.id === undefined) {
        Logger.WLog('Series not found for path: ' + folderPath);
        return 2;
    } else {
        Logger.ILog(`Series found: ${series.title}`);
    }

    try {
        let refreshBody = {
                seriesIds: [series.id],
                isNewSeries: false
            }

        // Ensure series is refreshed
        let refreshData = sonarr.sendCommand('RefreshSeries', refreshBody);
        // Wait for the completion of the refresh
        let refreshCompleted = sonarr.waitForCompletion(refreshData.id);
        if (!refreshCompleted) {
            Logger.WLog('refresh failed');
            return -1;
        }
        return 1;

    } catch (error) {
        Logger.WLog('Error: ' + error.message);
        return -1;
    }

}

// Repeatedly try finding a show by shortening the path
function findSeries(filePath, sonarr) {
    let currentPath = filePath;
    let show = null;

    let allSeries = sonarr.getAllShows();
    let seriesFolders = {};

    // Map each folder back to its series
    for (let series of allSeries) {
        let folderName = System.IO.Path.GetFileName(series.path);
        seriesFolders[folderName] = series;
    }

    while (currentPath) {
        // Get childmost piece of path to work with different remote paths
        let currentFolder = System.IO.Path.GetFileName(currentPath);

        if (seriesFolders[currentFolder]) {
            show = seriesFolders[currentFolder];
            Logger.ILog('Show found: ' + show.id);
            return show;
        }

        // If no show is found, go up 1 dir
        Logger.ILog(`Show not found at ${currentPath}. Trying ${System.IO.Path.GetDirectoryName(currentPath)}`)
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (!currentPath) {
            Logger.WLog('Unable to find show file at path ' + filePath);
            return null;
        }
    }
    return null;
}

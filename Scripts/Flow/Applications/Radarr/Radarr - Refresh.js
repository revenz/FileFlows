import { Radarr } from 'Shared/Radarr';

/**
 * @description This script will send a refresh command to Radarr for processing libraries in place.
 * @author Shaun Agius, Anthony Clerici : Modified by Macnemarion
 * @uid 78578502-07eb-44f5-99a9-76387c90d7f4
 * @revision 1
 * @param {string} URI Radarr root URI and port (e.g. http://radarr:7878)
 * @param {string} ApiKey API Key
 * @output Item refreshed successfully
 * @output Item not found
 */
function Script(URI, ApiKey) {
    //Orignally authored by Shaun Agius and Anthony Clerici as the Radarr - Rename Script modifed to just refresh instead.
    // Remove trailing / from URI
    URI = URI.replace(/\/$/, '');
    let radarr = new Radarr(URI, ApiKey);
    let folderPath = Variables.folder.FullName;


    // Find movie name from radarr
    let movie = findMovie(folderPath, radarr);

    if (!movie) {
        Logger.WLog('Movie not found for path: ' + folderPath);
        return 2;
    }

    // Get Movie File info
    let movieFiles = radarr.findMovieFiles(movie.id);
    if (!movieFiles) {
        Logger.ILog(`No files found for movie ${movie.id}`);
        return -1
    }

    let fileList = [];
    movieFiles.forEach(file => {
        fileList.push(file.id);
    });

    try {
        // Ensure movie is refreshed
        let refreshBody = {
            movieIds: [movie.id],
            isNewMovie: false
        }
        let refreshData = radarr.sendCommand('RefreshMovie', refreshBody)
        Logger.ILog('Movie refreshed');

        // Wait for the completion of the refresh
        let refreshCompleted = radarr.waitForCompletion(refreshData.id);
        if (!refreshCompleted) {
            Logger.ILog('refresh not completed');
            return -1;
        }
        return 1;
        
    } catch (error) {
        Logger.WLog('Error: ' + error.message);
        return -1;
    }
}

// Repeatedly try finding a movie by shortening the path
function findMovie(filePath, radarr) {
    let currentPath = filePath;
    let movie = null;

    let allMovies = radarr.fetchJson('movie');
    let movieFolders = {};

    // Map each folder back to its movie
    for (let movie of allMovies) {
        let folderName = System.IO.Path.GetFileName(movie.path);
        movieFolders[folderName] = movie;
    }

    while (currentPath) {
        // Get the childmost piece of the path
        let currentFolder = System.IO.Path.GetFileName(currentPath);

        if (movieFolders[currentFolder]) {
            movie = movieFolders[currentFolder];
            Logger.ILog('Movie found: ' + movie.id);
            return movie;
        }

        // Log the path where the movie was not found and move up one directory
        Logger.ILog(`Movie not found at ${currentPath}. Trying ${System.IO.Path.GetDirectoryName(currentPath)}`);
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (!currentPath) {
            Logger.WLog('Unable to find movie file at path ' + filePath);
            return null;
        }
    }

    return null;
}

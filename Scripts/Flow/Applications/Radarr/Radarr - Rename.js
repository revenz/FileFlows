import { Radarr } from 'Shared/Radarr';

/**
 * @description This script will send a rename command to Radarr
 * @author Shaun Agius, Anthony Clerici
 * @revision 14
 * @uid d9b988c1-b219-4c1b-b98f-2eb99ead716a
 * @param {string} URI Radarr root URI and port (e.g. http://radarr:7878)
 * @param {string} ApiKey API Key
 * @output Item renamed successfully
 * @output Rename not required for item
 * @output Item not found
 */
function Script(URI, ApiKey) {
    // Remove trailing / from URI
    URI = URI.replace(/\/$/, '');
    let radarr = new Radarr(URI, ApiKey);
    let folderPath = Variables.folder.FullName;
    let currentFileName = Variables.file.Name;
    let newFilePath = null;

    // Find movie name from radarr
    let [movie, basePath] = findMovie(folderPath, radarr);

    if (!movie) {
        Logger.WLog('Movie not found for path: ' + folderPath);
        return 3;
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
        // Ensure movie is refreshed before renaming
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

        // Get what radarr wants to rename the movie
        let renamedMovies = radarr.fetchRenamedMovies(movie.id, radarr);
        let renamedMovie = null;
        if (!renamedMovies) {
            Logger.ILog('No movies need to be renamed');
            return 2;
        }

        renamedMovies.every(element => {
            if (element.existingPath.endsWith(currentFileName)) {
                renamedMovie = element;
                return false
            }
            return true;
        });

        // Ensure movie is found
        if (!renamedMovie) {
            Logger.ILog(`Current file does not need renaming ${movie.id}`)
            return 2;
        }
        
        let newFileName = renamedMovie.newPath;
        // Get differences in the path
        Logger.ILog(`Base path: ${basePath}`);
        // Construct new filepath
        Logger.ILog(`New path: ${newFileName}`);
        newFilePath = System.IO.Path.Combine(basePath, newFileName);
        Logger.ILog(`Found it, renaming file ${currentFileName} to ${newFileName}`);

        if (newFileName === null) {
            Logger.WLog('No matching movie found to rename.');
            return 3;
        }

        // Now rename the file to what Radarr specifies
        let renameBody = {
            movieId: movie.id,
            files: fileList
        }
        Logger.ILog(renameBody);
        let renameResponse = radarr.sendCommand('RenameFiles', renameBody);
        let renameCompleted = radarr.waitForCompletion(renameResponse.id);

        if (!renameCompleted) {
            Logger.ILog('Rename not completed');
            return -1;
        }
        Logger.ILog(`Movie ${movie.id} successfully renamed. Setting as working file.`)

        // Radarr has successfully renamed the file, set new filename as working directory
        Flow.SetWorkingFile(newFilePath);
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
            return [movie, currentPath];
        }

        // Log the path where the movie was not found and move up one directory
        Logger.ILog(`Movie not found at ${currentPath}. Trying ${System.IO.Path.GetDirectoryName(currentPath)}`);
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
        if (!currentPath) {
            Logger.WLog('Unable to find movie file at path ' + filePath);
            return [null, null];
        }
    }

    return [null, null];
}

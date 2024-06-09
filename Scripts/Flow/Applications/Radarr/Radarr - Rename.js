import { Radarr } from 'Shared/Radarr';
/**
 * @author Anthony Clerici
 * @author Shaun Agius
 * @uid bd6f02c8-e650-4916-bcae-46f382d20388
 * @description This script will send a rename command to Radarr
 * @revision 6
 * @param {string} URI Radarr root URI and port (e.g. http://radarr:1234)
 * @param {string} ApiKey API Key
 * @output Item renamed
 * @output Item not found
 */
function Script(URI, ApiKey) {
    let radarr = new Radarr(URI, ApiKey);
    let folderPath = Variables.folder.FullName;
    let filePath = Variables.file.FullName;
    let currentFileName = Variables.file.Name;
    let newFileName = null;

    // Find movie name from radarr
    let movie = findMovie(folderPath, radarr);

    if (!movie) {
        Logger.WLog('Movie not found for path: ' + folderPath);
        Logger.ILog('Returning 2');
        return 2;
    }

    // Get Movie File info
    let movieFiles = radarr.findMovieFiles(movie.id, radarr);
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
            movieId: movie.id
        }
        let refreshData = radarr.sendCommand('RescanMovie', refreshBody)
        Logger.ILog(`Movie refreshed: ${JSON.stringify(refreshData)}`);

        // Wait for the completion of the refresh scan
        let refreshCompleted = radarr.waitForCompletion(refreshData.id);
        if (!refreshCompleted) {
            Logger.ILog('Refresh not completed');
            return -1;
        }

        // Get what radarr wants to rename the movie
        let renamedMovies = radarr.fetchRenamedMovies(movie.id, radarr);
        let renamedMovie = null;
        if (!renamedMovies) {
            Logger.ILog('No movies need to be renamed');
            Logger.ILog('Returning 2');
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
            Logger.ILog(`Current file not found in list to be renamed for movie ${movie.id}`)
            Logger.ILog('Returning 2');
            return 2;
        }
        
        newFileName = System.IO.Path.GetFileName(renamedMovie.newPath);
        Logger.ILog(`Found it, renaming file to ${newFileName}`);

        if (newFileName === null) {
            Logger.WLog('No matching movie found to rename.');
            Logger.ILog('Returning 2');
            return 2;
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
        let newFilePath = System.IO.Path.Combine(Variables.folder.FullName, newFileName);
        Flow.SetWorkingFile(newFilePath);
        Logger.ILog('Returning 1');
        return 1;

    } catch (error) {
        Logger.WLog('Error: ' + error.message);
        Logger.ILog('Returning -1');
        return -1;
    }
}

// Function to repeatedly try finding a movie by shortening the path
function findMovie(filePath, radarr) {
    let currentPath = filePath;
    let movie = null;

    while (currentPath) {
        movie = radarr.getMovieByPath(currentPath);
        if (movie) {
            Logger.ILog('Movie found: ' + movie.id);
            return movie;
        }

        // If no movie is found go 1 dir up
        currentPath = System.IO.Path.GetDirectoryName(currentPath);
    }
    
    Logger.WLog('Unable to find movie file at path ' + filePath);
    return null;
}


/**
 * @minimumVersion 24.11.1.4000
 * @description Spins up a Docker container from the 'roop' image to perform a face swap on the current working file.
 * @author Reven
 * @uid ce02fa25-1b2e-4d43-bfb2-0d1b80b2d19b
 * @revision 2
 * @param {string} Face The path to the face image to be swapped onto the input.
 * @param {bool} ManyFaces If all faces should be swapped
 * @output File successfully swapped and updated working file with new file.
 * @output No face found to swap
 */
function Script(Face, ManyFaces)
{
    // Copy the file into the temporary directory (if not already in the temp directory)
    let wf = Flow.CopyToTemp();
    let wfShortName = wf.substring(wf.lastIndexOf('/') + 1);
    let dir = Flow.TempPath;
    System.IO.File.Move(wf,dir + '/' + wfShortName);
    wf = dir + '/' + wfShortName;

    let extension = wf.substring(wf.lastIndexOf('.') + 1);
    extension = extension ? '.' + extension : '.jpg';
 
    let output = System.Guid.NewGuid().ToString() + extension;

    Face = Flow.CopyToTemp(Face);
    Face = Face.substring(Flow.TempPath.length + 1);        

    const ROOP_IMAGE = 'roop';

    Logger.ILog('WorkingFile: ' + wf);
    Logger.ILog('Output: ' + output);

    // Check if image exists
    let checkImage = Flow.Execute({
        command: 'docker',
        argumentList: ['images', '-q', `${ROOP_IMAGE}`] // Check for image by name
    });

    let imageID = checkImage.standardOutput.trim();
    if (!imageID) {
        Logger.ELog(`No image named "${ROOP_IMAGE}" found.`);
        return -1;
    }

    Logger.ILog('Image found: ' + imageID);

    // we want to save some stuff into the parent tempParent directory so its not redownloaded again and again
    let tempParent = new System.IO.DirectoryInfo(Flow.TempPathHost).Parent.FullName;
    tempParent = System.IO.Path.Combine(tempParent, 'roop');
    // Use `docker run` with specified command and options
    let process = Flow.Execute({
        command: 'docker',
        argumentList: [
            'run',
            '--rm',
            '-v', `${Flow.TempPathHost}:/temp`,
            '-v', `${tempParent}/root:/root`,
            '-v', `${tempParent}/models:/roop/models`,
            ROOP_IMAGE,
            '--frame-processor', 'face_swapper',
            '-s', '/temp/' + Face,
            '-t', '/temp/' + wfShortName,
            '-o', '/temp/' + output,
            ManyFaces ? '--many-faces' : null
        ]
    });

    if (process.exitCode !== 0) 
    {
        if(process.standardOutput.indexOf('list index out of range') > 0 || 
           process.standardError.indexOf('list index out of range') > 0)
        {
            Logger.ILog('No face found in source image');
            return 2;
        }
        Logger.ELog('Failed processing: ' + process.exitCode);
        return -1;
    }

    output = dir + '/' + output;

    if(System.IO.File.Exists(output) == false)
    {
        Logger.ELog('Failed to create output file');
        Flow.FailureReason = 'Failed to create output file';
        return -1;
    }

    // Set the output file path as the working file
    Flow.SetWorkingFile(output);
    return 1;
}
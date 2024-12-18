import { Radarr } from 'Shared/Radarr';

/**
 * @name Radarr - Trigger Manual Import 2.0
 * @uid cd8926f3-a9ab-4fa7-a36e-cceb8ad58987
 * @description Trigger Radarr to manually import the media, run after last file in folder moved.
 * @author iBuSH
 * @revision 1
 * @param {string} URL Radarr root URL and port (e.g. http://radarr:7878)
 * @param {string} ApiKey Radarr API Key
 * @param {string} ImportPath The output path for import triggering (default Working File)
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows)
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy)
 * @output Command sent
 */
function Script(URL, ApiKey) {
  URL = URL || Variables['Radarr.Url'] || Variables["Radarr.URI"];
  ApiKey = ApiKey || Variables["Radarr.ApiKey"];
  ImportPath = ImportPath || Variables.file.FullName;
  const ImportMode = MoveMode ? "move" : "copy";
  const radarr = new Radarr(URL, ApiKey);

  if (UseUnmappedPath) ImportPath = Flow.UnMapPath(ImportPath);

  Logger.ILog(`Triggering URL: ${URL}`);
  Logger.ILog(`Triggering Path: ${ImportPath}`);
  Logger.ILog(`Import Mode: ${ImportMode}`);

  let commandBody = {
      path: ImportPath,
      importMode: ImportMode
    };
  
  if (radarr.sendCommand('downloadedMoviesScan', commandBody)) return 1;

  return -1;
}
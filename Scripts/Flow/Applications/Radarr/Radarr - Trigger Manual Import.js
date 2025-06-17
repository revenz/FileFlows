import { Radarr } from 'Shared/Radarr';

/**
 * @name Radarr - Trigger Manual Import
 * @uid 0e522a46-ed76-4b40-bd1f-b3baac64264c
 * @description Trigger Radarr to manually import the media, run after last file in folder moved.
 * @author iBuSH
 * @revision 4
 * @param {string} URL Radarr root URL and port (e.g. http://radarr:7878).
 * @param {string} ApiKey Radarr API Key.
 * @param {string} ImportPath The output path for import triggering (default Working File).
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows).
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy).
 * @param {int} TimeOut Set in seconds the timeout for waiting completion (default 60 seconds, max 600 seconds).
 * @output Command sent
 */
function Script(URL, ApiKey, ImportPath, UseUnmappedPath, MoveMode, TimeOut) {
  URL = URL || Variables['Radarr.Url'] || Variables['Radarr.URI'];
  URL = URL.replace(/\/+$/g, "");
  ApiKey = ApiKey || Variables['Radarr.ApiKey'];
  ImportPath = ImportPath || Variables.file.FullName;
  TimeOut = TimeOut ? Math.min(TimeOut, 600) * 1000 : 60000;
  const ImportMode = MoveMode ? 'move' : 'copy';
  const radarr = new Radarr(URL, ApiKey);

  if (UseUnmappedPath) ImportPath = Flow.UnMapPath(ImportPath);

  Logger.ILog(`Triggering URL: ${URL}`);
  Logger.ILog(`Triggering Path: ${ImportPath}`);
  Logger.ILog(`Import Mode: ${ImportMode}`);

  let commandBody = {
      path: ImportPath,
      importMode: ImportMode
    };

  let response = radarr.sendCommand('downloadedMoviesScan', commandBody)

  // Wait for the completion of the scan
  let commandId = response['id']
  let importCompleted = radarr.waitForCompletion(commandId, TimeOut);
  if (!importCompleted) {
      Logger.WLog('Import failed');
      return -1;
  }

  Logger.DLog(`Command ID: ${commandId}`);
  Logger.ILog('Import completed successfully');
  return 1;

}
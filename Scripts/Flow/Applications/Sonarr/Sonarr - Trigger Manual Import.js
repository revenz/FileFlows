import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - Trigger Manual Import
 * @uid 8fe63a47-4a97-4b08-99f4-368d29279ad2
 * @description Trigger Sonarr to manually import the media, run after last file in folder moved.
 * @author iBuSH
 * @revision 3
 * @param {string} URL Sonarr root URL and port (e.g. http://sonarr:8989)
 * @param {string} ApiKey Sonarr API Key
 * @param {string} ImportPath The output path for import triggering (default Working File)
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows)
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy)
 * @output Command sent
 */
function Script(URL, ApiKey, ImportPath, UseUnmappedPath, MoveMode) {
  URL = URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI'];
  URL = URL.replace(/\/+$/g, "");
  ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
  ImportPath = ImportPath || Variables.file.FullName;
  const ImportMode = MoveMode ? 'move' : 'copy';
  const sonarr = new Sonarr(URL, ApiKey);

  if (UseUnmappedPath) ImportPath = Flow.UnMapPath(ImportPath);

  Logger.ILog(`Triggering URL: ${URL}`);
  Logger.ILog(`Triggering Path: ${ImportPath}`);
  Logger.ILog(`Import Mode: ${ImportMode}`);


  let commandBody = {
      path: ImportPath,
      importMode: ImportMode
    };

  let response = sonarr.sendCommand('downloadedEpisodesScan', commandBody)

  // Wait for the completion of the scan
  let commandId = response['id']
  let importCompleted = sonarr.waitForCompletion(commandId);
  if (!importCompleted) {
      Logger.WLog('Import failed');
      return -1;
  }

  Logger.DLog(`Command ID: ${commandId}`);
  Logger.ILog('Import completed successfully');
  return 1;

}
import { Sonarr } from 'Shared/Sonarr';

/**
 * @name Sonarr - Trigger Manual Import 2.0
 * @uid 544e1d8f-c3b5-4d1a-8f2c-5fdf24126100
 * @description Trigger Sonarr to manually import the media, run after last file in folder moved.
 * @author iBuSH
 * @revision 1
 * @param {string} URL Sonarr root URL and port (e.g. http://sonarr:8989)
 * @param {string} ApiKey Sonarr API Key
 * @param {string} ImportPath The output path for import triggering (default Working File)
 * @param {bool} UseUnmappedPath Whether to Unmap the path to the original FileFlows Server path when using nodes in different platforms (e.g. Docker and Windows)
 * @param {bool} MoveMode Import mode 'copy' or 'move' (default copy)
 * @output Command sent
 */
function Script(URL, ApiKey) {
  URL = URL || Variables['Sonarr.Url'] || Variables['Sonarr.URI'];
  ApiKey = ApiKey || Variables['Sonarr.ApiKey'];
  ImportPath = ImportPath || Variables.file.FullName;
  const ImportMode = MoveMode ? "move" : "copy";
  const sonarr = new Sonarr(URL, ApiKey);

  if (UseUnmappedPath) ImportPath = Flow.UnMapPath(ImportPath);

  Logger.ILog(`Triggering URL: ${URL}`);
  Logger.ILog(`Triggering Path: ${ImportPath}`);
  Logger.ILog(`Import Mode: ${ImportMode}`);


  let commandBody = {
      path: ImportPath,
      importMode: ImportMode
    };

  if (sonarr.sendCommand('downloadedEpisodesScan', commandBody)) return 1;

  return -1;
}
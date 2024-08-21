/**
 * @name Lidarr - Manually trigger import
 * @description Manually tells Lidarr to rescan, run after move
 * @author Lawrence / DoughDoze
 * @revision 3
 * @param {string} URI Lidarr root URI and port (e.g. http://lidarr:8686)
 * @param {string} ApiKey API Key
 * @output Output 0
 */
function Script(URI, ApiKey) {
  const lidarr = new Lidarr(URI, ApiKey);
  if (lidarr.downloadedAlbumsScan(Variables.folder.FullName)) return 1;

  return -1;
}

class Lidarr {
  constructor(URI, ApiKey) {
    if (!URI || !ApiKey) {
      Logger.ELog("No credentials specified");
      return -1;
    }

    this.URI = URI;
    this.ApiKey = ApiKey;
  }

  downloadedAlbumsScan(path) {
    let endpoint = `${this.URI}/api/v1/command`;
    let commandBody = {
      path: path,
      name: "DownloadedAlbumsScan",
    };

    let jsonData = JSON.stringify(commandBody);
    http.DefaultRequestHeaders.Add("X-API-Key", this.ApiKey);
    let response = http.PostAsync(endpoint, JsonContent(jsonData)).Result;
    http.DefaultRequestHeaders.Remove("X-API-Key");

    if (response.IsSuccessStatusCode) {
      let responseData = JSON.parse(
        response.Content.ReadAsStringAsync().Result
      );
      return responseData;
    } else {
      let error = response.Content.ReadAsStringAsync().Result;
      Logger.WLog("API error: " + error);
      return null;
    }
  }
}

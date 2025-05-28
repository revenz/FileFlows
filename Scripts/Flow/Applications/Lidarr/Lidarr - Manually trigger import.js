/**
 * @name Lidarr - Manually trigger import
 * @uid 8963756e-7bf1-4170-8446-29278db542d6
 * @description Manually tells Lidarr to rescan, run after last file in folder moved
 * @author lawrence / DoughDoze
 * @revision 4
 * @param {string} URI Lidarr root URI and port (e.g. http://lidarr:8686)
 * @param {string} ApiKey API Key
 * @output Command sent
 */
function Script(URI, ApiKey) {
  URI = URI || Variables['Lidarr.URI']
  ApiKey = ApiKey || Variables['Lidarr.ApiKey']

  const lidarr = new Lidarr(URI, ApiKey);
  if (lidarr.downloadedAlbumsScan(Variables.folder.FullName)) return 1;

  return -1;
}

class Lidarr {
  constructor(URI, ApiKey) {
    if (!URI || !ApiKey) {
      return Flow.Fail("No credentials specified"); 
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
      Logger.ILog(responseData);
      return responseData;
    } else {
      let error = response.Content.ReadAsStringAsync().Result;
      Logger.WLog("API error: " + error);
      Flow.Fail("API error")
      return null;
    }
  }
}

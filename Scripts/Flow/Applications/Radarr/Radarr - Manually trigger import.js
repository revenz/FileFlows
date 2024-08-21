/**
 * @name Radarr - Manually trigger import
 * @description Manually tells Radarr to rescan, run after move
 * @author Lawrence / DoughDoze
 * @revision 1
 * @param {string} URI Radarr root URI and port (e.g. http://radarr:7878)
 * @param {string} ApiKey API Key
 * @output Output 0
 */
function Script(URI, ApiKey) {
  const radarr = new Radarr(URI, ApiKey);
  if (radarr.downloadedMoviesScan(Variables.folder.FullName)) return 1;

  return -1;
}

class Radarr {
  constructor(URI, ApiKey) {
    if (!URI || !ApiKey) {
      Logger.ELog("No credentials specified");
      return -1;
    }

    this.URI = URI;
    this.ApiKey = ApiKey;
  }

  downloadedMoviesScan(path) {
    let endpoint = `${this.URI}/api/v3/command`;
    let commandBody = {
      path: path,
      name: "downloadedMoviesScan",
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
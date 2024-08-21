/**
 * @name Sonarr - Manually trigger import
 * @description Manually tells Sonarr to rescan, run after move
 * @author Lawrence / DoughDoze
 * @revision 1
 * @param {string} URI Sonarr root URI and port (e.g. http://sonarr:8989)
 * @param {string} ApiKey API Key
 * @output Output 0
 */
function Script(URI, ApiKey) {
  const sonarr = new Sonarr(URI, ApiKey);
  if (sonarr.downloadedEpisodesScan(Variables.folder.FullName)) return 1;

  return -1;
}

class Sonarr {
  constructor(URI, ApiKey) {
    if (!URI || !ApiKey) {
      Logger.ELog("No credentials specified");
      return -1;
    }

    this.URI = URI;
    this.ApiKey = ApiKey;
  }

  downloadedEpisodesScan(path) {
    let endpoint = `${this.URI}/api/v3/command`;
    let commandBody = {
      path: path,
      name: "downloadedEpisodesScan",
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
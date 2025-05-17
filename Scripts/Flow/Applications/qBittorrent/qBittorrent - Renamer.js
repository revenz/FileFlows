/**
 * @name qBittorrent - Renamer
 * @description Renames the current file in qBittorrent, Run after move / delete original.
 * This requires you don't have authentication on qBittorrent or different path mappings
 * @author lawrence
 * @uid 4130f59f-3871-4c24-9a0d-15966da6eb7c
 * @revision 2
 * @param {string} URI qBittorent root URI and port (e.g. http://qbitorrent:8080)
 * @output Item Renamed
 * @output Item not found or not renamed
 */
function Script(URI) {
  URI = URI || Variables["QBittorrent.URI"];

  const qbittorrent = new QBittorrent(URI);
  return qbittorrent.check(
    Flow.UnMapPath(Variables.folder.Orig.Name),
    Variables.file.Orig.Name,
    Variables.file.Name
  );
}

class QBittorrent {
  constructor(uri) {
    if (!uri) {
      return Flow.Fail("qBittorrent: No URI specified");
    }

    this.uri = uri;
  }

  getUrl(endpoint) {
    let url = "" + this.uri;
    if (url.endsWith("/") === false) url += "/";
    return `${url}api/v2/${endpoint}`;
  }

  getJson(endpoint) {
    let url = this.getUrl(endpoint);
    let response = http.GetAsync(url).Result;

    let body = response.Content.ReadAsStringAsync().Result;
    if (!response.IsSuccessStatusCode) {
      Flow.Fail("Unable to fetch: " + endpoint + "\n" + body);
    }
    return JSON.parse(body);
  }

  postJson(endpoint, data) {
    let url = this.getUrl(endpoint);
    let response = http.PostAsync(url, data).Result;

    let body = response.Content.ReadAsStringAsync().Result;
    if (!response.IsSuccessStatusCode) {
      Flow.Fail("Unable to rename: " + endpoint + "\n" + body);
    }
    return body;
  }

  check(path, oldName, newName) {
    let queue = this.getJson("torrents/info");
    path = path.replace(/\W/gi, "");
    if (!queue) return 2;

    let renamed = false;

    queue.forEach((item) => {
      if (item.content_path.replace(/\W/gi, "").includes(path)) {
        Logger.ILog(`Identified item ${item.name} from ${item.content_path}`);

        if (oldName != newName) {
          let files = this.getJson(`torrents/files?hash=${item.hash}`);
          files.forEach((file) => {
            if (file.name.includes(oldName)) {
              newName = file.name.replace(oldName, newName);

              var Dick = System.Collections.Generic.Dictionary(
                System.String,
                System.String
              );
              var list = new Dick();
              list.Add("hash", item.hash);
              list.Add("oldPath", file.name);
              list.Add("newPath", newName);
              let data = FormUrlEncodedContent(list);

              this.postJson("torrents/renameFile", data);
              renamed = true;
            }
          });
        } else {
          Logger.DLog("Did not rename, item has the same name or is seeding");
        }
      }
    });

    if (renamed) return 1;

    return 2;
  }
}
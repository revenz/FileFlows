/**
 * @name qBittorrent - Seeding check
 * @description Checks if the current file is seeding in qBittorrent so you can 'reprocess' later
 * This requires you don't have authentication on qBittorrent or different path mappings
 * @author lawrence
 * @uid 48c1518d-83fc-416d-972d-8caf7defe424
 * @revision 2
 * @param {string} URI qBittorent root URI and port (e.g. http://qbitorrent:8080)
 * @output Item is seeding
 * @output Item not found or seeding
 */
function Script(URI) {
    URI = URI || Variables["QBittorrent.URI"];

    const qbittorrent = new QBittorrent(URI);
    return qbittorrent.check(Flow.UnMapPath(Variables.folder.Orig.Name));
}

class QBittorrent {
    constructor(uri) {
        if (!URI) {
            Flow.Fail("qBittorrent: No URI specified");
            return -1;
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

    check(path) {
        let seeding = false;
        let queue = this.getJson("torrents/info");
        path = path.replace(/\W/gi, "");
        if (!queue) return 2;

        queue.forEach((item) => {
            if (item.content_path.replace(/\W/gi, "").includes(path)) {
                if (item.state.includes("upload")) seeding = true;

                Logger.ILog(
                    `Identified item ${item.name} from ${item.content_path}`
                );
            }
        });

        if (seeding) return 1;

        return 2;
    }
}

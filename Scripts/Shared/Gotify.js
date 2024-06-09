/**
 * @name Gotify
 * @uid ef1e6fad-2313-4851-a11e-4156c489b04d
 * @revision 4
 * @description Class that interacts with Gotify
 * @minimumVersion 1.0.0.0
 */
export class Gotify
{
    URL;
    AccessToken;

    constructor(){
        this.URL = Variables['Gotify.Url'];
        if(!this.URL)
            MissingVariable('Gotify.Url');
        this.AccessToken = Variables['Gotify.AccessToken'];
        if(!this.AccessToken)
            MissingVariable('Gotify.AccessToken');
    }

    getUrl()
    {
        let url = '' + this.URL;
        if(url.endsWith('/') === false)
            url += '/';
        return url + 'message';
    }

    
    /**
     * Sends a message to a Gotify server
     * @param {string} title title of the message
     * @param {string} message the message to send
     * @param {number} priority the priority of the message
     * @returns {bool} if the message was sent successfully
     */
    sendMessage(title, message, priority)
    {
        let url = this.getUrl();
        
        http.DefaultRequestHeaders.Add("X-Gotify-Key", this.AccessToken);
        let json = JSON.stringify({
            title: title,
            message: message,
            priority: !priority ? 2 : priority
        });
        let response = http.PostAsync(url, JsonContent(json)).Result;
        http.DefaultRequestHeaders.Remove("X-Gotify-Key");
        if (response.IsSuccessStatusCode)
            return true;

        let error = response.Content.ReadAsStringAsync().Result;
        Logger?.WLog("Error from Gotify: " + error);
        return false;
    }
}
/**
 * @name Apprise
 * @uid 3aef04da-13b1-494e-8ea4-075432d86bd7
 * @description Class that interacts with Apprise
 * @revision 5
 */
export class Apprise
{
    URL;
    DefaultEndpoint;

    constructor(){
        this.URL = Variables['Apprise.Url'];
        if(!this.URL)
            MissingVariable('Apprise.Url');
        this.DefaultEndpoint = Variables['Apprise.DefaultEndpoint'];
        if(!this.DefaultEndpoint)
            MissingVariable('Apprise.DefaultEndpoint');
    }

    getUrl(endpoint)
    {
        let url = '' + this.URL;
        if(url.endsWith('/') === false)
            url += '/';
        return url + endpoint;
    }

    
    /**
     * Sends a message to the default Apprise endpoint
     * @param {string} type the type of message, info, success, warning, error
     * @param {string} message the message to send
     * @param {string[]} tags an array of tags to send the message with, if missing "all" will be used
     * @returns {bool} if the message was sent successfully
     */
    sendMessage(type, message, tags)
    {
        return this.sendMessageToEndpoint(this.DefaultEndpoint, type, message, tags);
    }

    /**
     * Sends a message to a specific Apprise endpoint
     * @param {string} endpoint the endpoint to send the message to
     * @param {string} type the type of message, info, success, warning, error
     * @param {string} message the message to send
     * @param {string[]} tags an array of tags to send the message with, if missing "all" will be used
     * @returns {bool} if the message was sent successfully
     */
    sendMessageToEndpoint(endpoint, type, message, tags)
    {
        let url = this.getUrl(endpoint);

        let tagArg = 'all';
        if(tags && typeof(tags) === 'string')
            tagArg = tags;
        else if(tags && tags.length)
            tagArg = tags.join(';');
        
        let json = JSON.stringify({
            body: message,
            type: type,
            tag: tagArg
        });
        let response = http.PostAsync(url, JsonContent(json)).Result;
        if (response.IsSuccessStatusCode)
            return true;

        let error = response.Content.ReadAsStringAsync().Result;
        Logger?.WLog("Error from Apprise: " + error);
        return false;
    }
}
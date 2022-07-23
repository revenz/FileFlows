/**
 * Class that interacts with the SABnzbd API 
 * @name SABnzbd
 * @revision 1
 */
export class SABnzbd
{
    API_KEY;
    URL;

    constructor()
    {
        this.URL = Variables['SABnzbd_Url'];
        if(!this.URL)
            MissingVariable('SABnzbd_Url');
        this.API_KEY = Variables['SABnzbd_ApiKey'];
        if(!this.API_KEY)
            MissingVariable('SABnzbd_ApiKey');
    }

    /**
     * Pauses SABnzbd from downloading
     */
    pause() 
    {
        Logger.ILog('Pausing SABnzbd');
        this.send('pause');
    }

    /**
     * Resumes SABnzbd downloading
     */
    resume(){
        Logger.ILog('Resuming SABnzbd');
        this.send('resume');
    }

    getUrl(mode)
    {
        let url = '' + this.URL;
        if(url.endsWith('/') === false)
            url += '/';
        return `${url}api?output=json&apikey=${this.API_KEY}&mode=${mode}`;    
    }
    
    send(mode)
    {        
        let url = this.getUrl(mode);
        let response = http.GetAsync(url).Result;
        let responseBody = response.Content.ReadAsStringAsync().Result;
        if(response.IsSuccessStatusCode === false)
            throw responseBody;
        Logger.ILog('Status Code: ', response.StatusCode);
    }
}
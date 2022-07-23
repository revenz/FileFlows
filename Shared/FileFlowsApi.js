/**
 * Class that interacts with the FileFlows API 
 * @name FileFlows API
 * @revision 1
 */
export class FileFlowsApi
{
    URL;

    constructor(){
        this.URL = Variables['FileFlowsUrl'];
        if(!this.URL)
            MissingVariable('FileFlowsUrl');
    }

    getUrl(endpoint)
    {
        let url = '' + this.URL;
        if(url.endsWith('/') === false)
            url += '/';
        return url + 'api/' + endpoint;
    }

    getStatus(status)
    {
        let url = this.getUrl('library-file/status');
        let response = http.GetAsync(url).Result;
        let responseBody = response.Content.ReadAsStringAsync().Result;
        if(response.IsSuccessStatusCode === false)
            throw responseBody;        
        let data = JSON.parse(responseBody);
        let item = data.find(x => x.Status === status);
        if(!item)
            return 0;
        return item.Count;
    }

    /**
     * Gets the number of unprocessed files
     * This does NOT included disabled or on hold files
     * @returns the number of unprocessed files
     */
    getUnprocessedCount() {
        return this.getStatus(0);
    }

    /**
     * Gets the number of processing files
     * @returns the number of processing files
     */
    getProcessingCount(){
        return this.getStatus(1);
    }

    /**
     * Gets the number of processed files
     * @returns the number of processed files
     */
    getProcessedCount(){
        return this.getStatus(2);
    }
}
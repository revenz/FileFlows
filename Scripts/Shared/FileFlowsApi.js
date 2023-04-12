/**
 * Class that interacts with the FileFlows API 
 * @name FileFlows API
 * @revision 5
 * @minimumVersion 1.0.0.0
 */
export class FileFlowsApi
{
    URL;

    constructor(){
        this.URL = Variables['FileFlows.Url'];
        if(!this.URL)
            MissingVariable('FileFlows.Url');
    }

    getUrl(endpoint)
    {
        let url = '' + this.URL;
        if(url.endsWith('/') === false)
            url += '/';
        return url + 'api/' + endpoint;
    }

    /**
     * Gets the total of each file status in the system
     * @returns the file status
     */
    getStatus()
    {
        let url = this.getUrl('library-file/status');
        let response = http.GetAsync(url).Result;
        let responseBody = response.Content.ReadAsStringAsync().Result;
        if(response.IsSuccessStatusCode === false)
            throw responseBody;        
        let data = JSON.parse(responseBody);
        let status = { 
            Unprocessed: 0, // 0
            Processed: 0, // 1
            Processing: 0, // 2
            FlowNotFound: 0, // 3
            Failed: 0, // 4
            Duplicate: 0, // 5
            MappingIssue: 0, // 6
            MissingLibrary: 0, // 7

            OutOfSchedule: 0, // -1
            Disabled: 0, // -2
            OnHold: 0, // -3
        };
        for(let item of data) {
            if(item.Status === -3)
                status.OnHold = item.Count;
            else if(item.Status === -2)
                status.Disabled = item.Count;
            else if(item.Status === -1)
                status.OutOfSchedule = item.Count;
            else if(item.Status === 0)
                status.Unprocessed = item.Count
            else if(item.Status === 1)
                status.Processed = item.Count
            else if(item.Status === 2)
                status.Processing = item.Count
            else if(item.Status === 3)
                status.FlowNotFound = item.Count
            else if(item.Status === 4)
                status.Failed = item.Count
            else if(item.Status === 5)
                status.Duplicate = item.Count
            else if(item.Status === 6)
                status.MappingIssue = item.Count
            else if(item.Status === 7)
                status.MissingLibrary = item.Count
        }
        return status;
    }

    /**
     * Gets the number of unprocessed files
     * This does NOT included disabled or on hold files
     * @returns the number of unprocessed files
     */
    getUnprocessedCount() {
        return this.getStatus().Unprocessed;
    }

    /**
     * Gets the number of processing files
     * @returns the number of processing files
     */
    getProcessingCount(){
        return this.getStatus().Processing;
    }

    /**
     * Gets the number of processed files
     * @returns the number of processed files
     */
    getProcessedCount(){
        return this.getStatus().Processed;
    }

    /**
     * Gets the number of failed files
     * @returns the number of failed files
     */
    getFailedCount(){
        return this.getStatus().Failed;
    }

    /**
     * Gets a variable from FileFlows
     * @param name the variables name
     */
    getVariable(name) {

        let url = this.getUrl('variable/name/' + encodeURIComponent(name));
        let response = http.GetAsync(url).Result;
        let responseBody = response.Content.ReadAsStringAsync().Result;
        if(response.IsSuccessStatusCode === false)
            throw responseBody;        
        let data = JSON.parse(responseBody);
        return data.Value;
    }

    /**
     * Processes in FileFlows, if the File has already been processed, this will make it reprocess
     * If the file has not been found, this will attempt to add it to the library for processing
     * @param {string} fileName 
     */
    processFile(fileName) {
        let url = this.getUrl('library-file/process-file?filename=' + encodeURIComponent(fileName));
        let response = http.PostAsync(url).Result;
        let responseBody = response.Content.ReadAsStringAsync().Result;
        if(response.IsSuccessStatusCode === false)
            throw responseBody;        
        return true;
    }
}
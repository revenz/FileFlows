/**
 * Creates the InputCode instance and returns it
 * @param dotNetObject The calling dotnet object
 * @returns {InputCode} the InputCode instance
 */
export function createInputCode(dotNetObject)
{
    return new InputCode(dotNetObject);
}

export class InputCode
{
    constructor(dotNetObject)
    {
        this.dotNetObject = dotNetObject;
    }
    
    shellEditor()
    {
        monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
            target: monaco.languages.shell,
            allowNonTsExtensions: true
        });
    }
    
    jsEditor(variables, sharedScripts)
    {
        monaco.languages.typescript.javascriptDefaults.setCompilerOptions({
            target: monaco.languages.typescript.ScriptTarget.ES6,
            allowNonTsExtensions: true
        });
        if(sharedScripts?.length)
        {
            for(let script of sharedScripts)
            {
                let path = script.path.replace('Scripts/', '');
                path = path.replace('.js', '');
                let genCode = `declare module '${path}' { ${script.code} }`;
                monaco.languages.typescript.javascriptDefaults.addExtraLib(
                    genCode,
                    path + '/index.d.ts');
            }
        }


        monaco.editor.createModel(
            `const Logger = { 
    /**
     * Logs the provided messages as information.
     * @param {...any} messages - The messages to log.
     */
    ILog: function (...messages) { },
    /**
     * Logs the provided messages as debug information.
     * @param {...any} messages - The messages to log.
     */
    DLog: function (...messages) { },
    /**
     * Logs the provided messages as warnings.
     * @param {...any} messages - The messages to log.
     */
    WLog: function (...messages) { },
    /**
     * Logs the provided messages as errors.
     * @param {...any} messages - The messages to log.
     */
    ELog: function (...messages) { }
  }`,
            "javascript"
        );


        monaco.editor.createModel(
            `const LanguageHelper = {
        /**
         * Converts the provided language to its English name.
         * @param {string} language - The language to convert.
         * @returns {string} The English name of the language.
         */
        GetEnglishFor: function(language) { },

        /**
         * Retrieves the ISO 639-1 code for the provided language.
         * @param {string} language - The language.
         * @returns {string} The ISO 639-1 code for the language.
         */
        GetIso1Code: function(language) { },

        /**
         * Retrieves the ISO 639-2 code for the provided language.
         * @param {string} language - The language.
         * @returns {string} The ISO 639-2 code for the language.
         */
        GetIso2Code: function(language) { },

        /**
         * Checks if two languages represent the same language.
         * @param {string} language1 - The first language code to compare.
         * @param {string} language2 - The second language code to compare.
         * @returns {boolean} True if the languages represent the same language, false otherwise.
         */
        AreSame: function(language1, language2) { }
    };
`, "javascript"
    );

        monaco.editor.createModel(`
/**
 * Sleeps for the given time
 * @param {number} milliseconds - The number of milliseconds to sleep for
 */
    function Sleep(milliseconds) {}`,
            "javascript"
        );

            monaco.editor.createModel(
                `const CacheStore = {
            /**
             * Gets an JSON string from the cache store.
             * @param {string} key - The name of the object to get.
             * @returns {string} - The value of the object as a JSON string.
             */
            GetJson: function(key:string) { },

            /**
             * Sets an object in the cache store.
             * @param {string} key - The name of the object to set.
             * @param {object} value - The value of the object.
             * @param {int} minutes - The number of minutes to keep this object in the cache for.
             */
            SetObject: function(key:string, value, minutes: number) { },

            /**
             * Sets an JSON string in the cache store.
             * @param {string} key - The name of the object to set.
             * @param {string} json - The json to store
             * @param {int} minutes - The number of minutes to keep this object in the cache for.
             */
            SetJson: function(key:string, json: string, value:number) { }
        }`,
                "javascript"
            );
        
    //     monaco.editor.createModel(
    //         `const CacheStore = {
    //     /**
    //      * Clears the cache store.
    //      */
    //     Clear: function() { },
    //    
    //     /**
    //      * Gets an object from the cache store.
    //      * @param {string} key - The name of the object to get.
    //      * @returns {object} - The value of the object.
    //      */
    //     Get: function(key:string) { },
    //    
    //     /**
    //      * Gets an integer from the cache store.
    //      * @param {string} key - The name of the object to get.
    //      * @returns {number} - The value of the integer.
    //      */
    //     GetInt: function(key:string) { },
    //    
    //     /**
    //      * Gets a boolean from the cache store.
    //      * @param {string} key - The name of the object to get.
    //      * @returns {boolean} - The value of the boolean.
    //      */
    //     GetBool: function(key:string) { },
    //    
    //     /**
    //      * Gets a string from the cache store.
    //      * @param {string} key - The name of the object to get.
    //      * @returns {string} - The value of the string.
    //      */
    //     GetString: function(key:string) { },
    //    
    //     /**
    //      * Sets an object in the cache store.
    //      * @param {string} key - The name of the object to set.
    //      * @param {object} value - The value of the object.
    //      */
    //     Set: function(key:string, value) { },
    //    
    //     /**
    //      * Sets an integer in the cache store.
    //      * @param {string} key - The name of the object to set.
    //      * @param {number} value - The value of the integer.
    //      */
    //     SetInt: function(key:string, value:number) { },
    //    
    //     /**
    //      * Sets a string in the cache store.
    //      * @param {string} key - The name of the object to set.
    //      * @param {string} value - The value of the string.
    //      */
    //     SetString: function(key:string, value:string) { },
    //    
    //     /**
    //      * Sets a boolean in the cache store.
    //      * @param {string} key - The name of the object to set.
    //      * @param {boolean} value - The value of the boolean.
    //      */
    //     SetBool: function(key:string, value:boolean) { }
    // }`,
    //         "javascript"
    //     );


        monaco.editor.createModel(
            `const Flow = { 
/**
 * Creates a directory if it does not already exist
 * @param {string} path - The path of the directory
 */
CreateDirectoryIfNotExists: function (path:string) { },
/**
 * Sets the thumbnail on the file, takes either a file path or URL
 * @param {string} path - The path of the file or URL
 */
SetThumbnail: function(path:string) {},
/**
 * Fails the flow with the given reason.
 *
 * Example usage: return Flow.Fail('File not found');
 * @param {string} reason - The reason to fail the flow
 * @returns {number} - The error return code, return this from the function/script
 */
Fail: function(reason:string):number {},
/**
 * Gets the size of a directory in bytes
 * @param {string} path - The path of the directory
 * @returns {number} - The size of the directory in bytes
 */
GetDirectorySize: function (path:string):number { },
/**
 * Gets a parameter from the collection, these are generally complex objects set by plugins
 * @param {string} key - The key of the parameter to get
 * @returns {object} - The parameter if found
 */
GetParameter: function (key:string):any { }, 
/**
 * Checks if a plugin is available
 * @param {string} name - The name of the plugin
 * @returns {boolean} - True if the plugin is available, otherwise false
 */
HasPlugin: function(name:string):boolean { },
/**
 * Maps a path to its real path
 * @param {string} path - The path to map
 * @returns {string} - The real path
 */
MapPath: function (path:string) { }, 
/**
 * Unmaps a path to the original FileFlows Server path.
 *
 * Note: It is safe to unmap a path multiple times as this should not effect its value
 * @param {string} path - The path to unmap
 * @returns {string} - The unmapped path as it appears on the server<
 */
UnMapPath: function (path:string) { }, 
/**
 * Moves a file to a destination path
 * @param {string} destination - The destination path
 */
MoveFile: function (destination:string) { }, 
/**
 * Resets the working file to the original input file
 */
ResetWorkingFile: function () { }, 
/**
 * Sets the working file with an optional delete flag
 * @param {string} filename - The filename of the working file
 * @param {boolean} dontDelete - True to prevent deletion of the file, false otherwise
 */
SetWorkingFile: function (filename:string, dontDelete:boolean) { }, 
/**
 * Sets the value of a parameter by its key
 * @param {string} key - The key of the parameter to set
 * @param {*} value - The value to set for the parameter
 */
SetParameter: function (key:string, value) { }, 
/**
 * Generates a new unique identifier (GUID)
 * @returns {string} - The new GUID
 */
NewGuid: function ():string { }, 
/**
 * Add a tags to the file by their tag name
 * @param {string[]} names - The names of the tags
 * @returns {bool} - The number of tags added
 */
AddTags: function (names:string[]):number { }, 
/**
 * Sets the tags on a file, clearing any existing tags
 * @param {string[]} names - The names of the tags
 * @returns {bool} - The number of tags added
 */
SetTags: function (names:string[]):number { }, 
/**
 * Copies a file to the temporary path
 * @param {string} filename - The filename to copy to the temporary path
 * @returns {string} - The temporary path
 */
CopyToTemp: function (filename:string):string { }
/**
 * Records additional information that will be shown on the processing runner
 * @param {string} name - The name or label to show
 * @param {any} value - The value to show next to the name
 * @param {any} steps - How many steps to keep this information visible for, each flow part change decreases the steps
 */
AdditionalInfoRecorder: function (name:string, value:any, steps:number):string { }
/**
 * The file name
 * @type {string}
 */
FileName: string, 
/**
 * The temporary path
 * @type {string}
 */
TempPath: string, 
/**
 * The temporary path name
 * @type {string}
 */
TempPathName: string, 
/**
 * The temporary path host
 * @type {string}
 */
TempPathHost: string, 
/**
 * The unique identifier of the runner
 * @type {string}
 */
RunnerUid: string, 
/**
 * The working file
 * @type {string}
 */
WorkingFile: string, 
/**
 * The working file name
 * @type {string}
 */
WorkingFileName: string, 
/**
 * The size of the working file
 * @type {number}
 */
WorkingFileSize: number, 
/**
 * The relative file path
 * @type {string}
 */
RelativeFile: string, 
/**
 * If this flow is running against a directory, if false then it's running against a file
 * @type {boolean}
 */
IsDirectory: boolean, 
/**
 * A flag indicating if it's running in a Docker environment
 * @type {boolean}
 */
IsDocker: boolean, 
/**
 * A flag indicating if it's running on a Windows OS
 * @type {boolean}
 */
IsWindows: boolean, 
/**
 * A flag indicating if it's running on a Linux OS
 * @type {boolean}
 */
IsLinux: boolean, 
/**
 * A flag indicating if it's running on a macOS
 * @type {boolean}
 */
IsMac: boolean, 
/**
 * A flag indicating if it's running on an ARM architecture
 * @type {boolean}
 */
IsArm: boolean, 
/**
 * The library path
 * @type {string}
 */
LibraryPath: string,
/**
 * Execute a process and capture the output
 * you can use arguments for a string argument list, or argumentList which is an string array and will escape the arguments for you correctly
 * timeout is optional, number of seconds to wait before killing the process
 * @param {{
 *   command: string,
 *   arguments: string,
 *   argumentList: string[],
 *   timeout: number,
 *   workingDirectory: string
 * }} args - The arguments and options for the command execution
 * @returns {{
 *   completed: boolean,
 *   exitCode: number,
 *   output: string,
 *   standardOutput: string,
 *   standardError: string
 * }} - The result of the command execution
 */
Execute: function (args:{{ command: string, arguments: string, argumentList: string[], timeout: number, workingDirectory: string}}):
    {{ completed: boolean, exitCode: number, output: string, standardOutput: string, standardError: string }} { }, 
}`,
            "javascript"
        );

        const funFileInfo = `declare function FileInfo(path: string): {
   Exists: bool,
   Length: number,
   Name: string, 
   DirectoryName: string, 
   IsReadOnly: bool,
   CreationTime: date,
   LastWriteTime: date, 
   LastAccessTime: date,
   Extension: string
};`;
        monaco.languages.typescript.javascriptDefaults.addExtraLib(funFileInfo, 'ff.funFileInfo');

        if (variables) {
            var actualVaraibles = {};
            for (let k in variables) {
                let tk = k;
                let av = actualVaraibles
                while (tk.indexOf('.') > 0) {
                    let nk = tk.substring(0, tk.indexOf('.'));
                    if(!av[nk])
                        av[nk] = {};
                    tk = tk.substring(tk.indexOf('.') + 1);
                    av = av[nk];
                }
                if(!av[tk])
                    av[tk] = variables[k]
            }
            let js = "const Variables = " + JSON.stringify(actualVaraibles);
            monaco.editor.createModel(js, "javascript");
        }
    }


    codeCaptureSave() {
        window.CodeCaptureListener = (e) => {
            if (e.ctrlKey === false || e.shiftKey || e.altKey || e.code != 'KeyS')
                return;
            e.preventDefault();
            e.stopPropagation();
            setTimeout(() => {
                this.dotNetObject.invokeMethodAsync("SaveCode");
            }, 1);
            return true;
        };
        document.addEventListener("keydown", window.CodeCaptureListener);
    }
    
    codeUncaptureSave() {
        document.removeEventListener("keydown", window.CodeCaptureListener);
    }
}
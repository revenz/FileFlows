window.dashboardElementResized = new Event('dashboardElementResized', {});
var ffcsharp;
var ACCESS_TOKEN;

window.ff = {
    setCSharp(csharp) {
        window.ff.ffcsharp = csharp;
    },
    getAccessToken:  function(){
        try{
            let value = localStorage.getItem('ACCESS_TOKEN');
            if(value)
                return JSON.parse(value);
        }catch(err){
            return ACCESS_TOKEN;
        }
    },
    setAccessToken: function(token) {
        try{
            if(token) localStorage.setItem('ACCESS_TOKEN', JSON.stringify(token));
            else localStorage.setItem('ACCESS_TOKEN', token);
        }catch(err){
            return ACCESS_TOKEN = token;
        }
    },
    doFetch: function(url) {
        let token = ff.getAccessToken();
        if(!token)
            return fetch(url);

        return fetch(url, {
            method: 'GET',
            headers: {
                'Authorization': 'Bearer ' + token
            }
        });
    },
    open: function(url, inBrowser){
        if(inBrowser) {
            open(url, '_blank', 'noopener,noreferrer');
            return;            
        }
        if(!window.ff.ffcsharp){
            console.log('ffcsharp not set, cannot open help');
            return;
        }
        window.ff.ffcsharp.invokeMethodAsync('OpenUrl', url).then(result => {
            if(!result)
                open(url, '_blank', 'noopener,noreferrer');
        })
    },
    openLink: function(event) {
        event.preventDefault();
        let url = event.target.getAttribute('href');
        if(url.startsWith('/'))
            window.ff.ffcsharp.invokeMethodAsync('NavigateTo', url)
        else
            ff.open(url);        
    },
    log: function (level, parameters) {

        if (!parameters || parameters.length === 0)
            return;
        let message = parameters[0]
        parameters.splice(0, 1);

        if (level === 0) parameters.length > 0 ? console.error('ERRR: ' + message, parameters) : console.error('ERRR: ' + message);
        else if (level === 1) parameters.length > 0 ? console.warn('WARN: ' + message, parameters) : console.warn('WARN: ' + message);
        else if (level === 2) parameters.length > 0 ? console.info('INFO: ' + message, parameters) : console.info('INFO: ' + message);
        else if (level === 3) parameters.length > 0 ? console.log('DBUG: ' + message, parameters) : console.log('DBUG: ' + message);
        else parameters.length > 0 ? console.error(message, parameters) : console.log(message);
    },
    deviceDimensions: function () {

        return { width: screen.width, height: screen.height };
    },
    disableMovementKeys: function (element) {
        if (typeof (element) === 'string')
            element = document.getElementById(element);
        if (!element)
            return;
        const blocked = ['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight', 'Enter', 'PageUp', 'PageDown', 'Home', 'End'];
        element.addEventListener('keydown', e => {
            if (e.target.getAttribute('data-disable-movement') != 1)
                return;

            if (blocked.indexOf(e.code) >= 0) {
                e.preventDefault();
                return false;
            }
        })
    },
    loadJS: function (url, callback) {
        if (!url)
            return;
        let tag = document.createElement('script');
        tag.src = url;
        tag.onload = () => {
            if (callback)
                callback();
        };
        document.body.appendChild(tag);
    },
    downloadFile: function (url, filename) {
        let authToken = ff.getAccessToken();
        if(authToken)
        {            
            // Make a fetch request to the server, including the Authorization header
            fetch(url, {
                method: 'GET',
                headers: {
                    'Authorization': `Bearer ${authToken}`, // Add your Authorization token here
                }
            })
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to fetch file');
                }
                return response.blob(); // Convert response to Blob (binary data)
            })
            .then(blob => {
                // Create a temporary URL for the Blob
                const url = window.URL.createObjectURL(blob);

                // Create a hidden <a> element to trigger the download
                const link = document.createElement('a');
                link.href = url;
                link.download = filename || 'download';  // Set the filename for the download
                document.body.appendChild(link); // Append the link to the body (required for Firefox)

                // Trigger a click event on the link to start the download
                link.click();

                // Cleanup: remove the link and revoke the object URL
                document.body.removeChild(link);
                window.URL.revokeObjectURL(url);
            })
            .catch(error => {
                console.error('Download failed:', error);
            });
        }
        else {
            const anchorElement = document.createElement('a');
            anchorElement.href = url;
            anchorElement.download = filename ? filename : 'File';
            anchorElement.click();
            anchorElement.remove();
        }
    },
    saveTextAsFile: function (fileName, textContent) {
        // Create a Blob with the file contents
        let blob = new Blob([textContent], { type: 'text/plain' });

        // Create a temporary anchor element
        let a = document.createElement("a");
        document.body.appendChild(a);

        // Set the object URL as the anchor's href
        a.href = window.URL.createObjectURL(blob);

        // Set the file name
        a.download = fileName;

        // Trigger a click event on the anchor to initiate download
        a.click();

        // Cleanup
        window.URL.revokeObjectURL(a.href);
        document.body.removeChild(a);
    },
    saveByteArrayAsFile: function (fileName, byteArray) {
        // Convert byte array to Blob
        let blob = new Blob([byteArray], { type: 'application/octet-stream' });

        // Create a temporary anchor element
        let link = document.createElement('a');

        // Set the href attribute of the anchor element to the Blob object URL
        link.href = window.URL.createObjectURL(blob);

        // Set the download attribute to specify the file name
        link.download = fileName;

        // Append the anchor element to the body
        document.body.appendChild(link);

        // Programmatically click the anchor element to trigger the download
        link.click();

        // Remove the anchor element from the DOM
        document.body.removeChild(link);
    },
    copyToClipboard: function (text) {
        if (window.clipboardData && window.clipboardData.setData) {
            // Internet Explorer-specific code path to prevent textarea being shown while dialog is visible.
            return window.clipboardData.setData("Text", text);

        }
        else if (document.queryCommandSupported && document.queryCommandSupported("copy")) {
            var textarea = document.createElement("textarea");
            textarea.textContent = text;
            textarea.style.position = "fixed";  // Prevent scrolling to bottom of page in Microsoft Edge.
            document.body.appendChild(textarea);
            textarea.select();
            try {
                return document.execCommand("copy");  // Security exception may be thrown by some browsers.
            }
            catch (ex) {
                console.warn("Copy to clipboard failed.", ex);
                return prompt("Copy to clipboard: Ctrl+C, Enter", text);
            }
            finally {
                document.body.removeChild(textarea);
            }
        }
    },
    scrollTableToTop: function(){
        document.querySelector('.flowtable-body').scrollTop = 0;
    },
    
    nearBottom: function(element){
        let ele = element;
        if(typeof(element) === 'string')
            ele = document.getElementById(element);
        if(!ele)
            ele = document.querySelector(element);
        if(!ele)
            return false;

        const threshold = 100;
        const position = ele.scrollTop + ele.offsetHeight;
        const height = ele.scrollHeight;
        return position > height - threshold;
    },
    scrollToBottom: function(element, notSmooth) {
        let ele = element;
        if(typeof(element) === 'string')
            ele = document.getElementById(element);
        if(!ele)
            ele = document.querySelector(element);
        if(!ele)
            return false;
        if(notSmooth)
            ele.scrollTo({ top: ele.scrollHeight })
        else
            ele.scrollTo({ top: ele.scrollHeight, behavior: 'smooth' })
    },
    codeCaptureSave: function(csharp) {
        window.CodeCaptureListener = (e) => {
            if(e.ctrlKey === false || e.shiftKey || e.altKey || e.code != 'KeyS')
                return;
            e.preventDefault();
            e.stopPropagation();
            setTimeout(() => {                
                csharp.invokeMethodAsync("SaveCode");
            },1);
            return true;
        };
        document.addEventListener("keydown", window.CodeCaptureListener);
    },
    codeUncaptureSave: function(){
        document.removeEventListener("keydown", window.CodeCaptureListener);        
    },
    onEscapeListener: function(csharp) {
        window.CodeCaptureListener = (e) => {
            if(e.ctrlKey || e.shiftKey || e.altKey || e.code != 'Escape')
                return;
            e.preventDefault();
            e.stopPropagation();
            
            hasModal = !!document.querySelector('.flow-modal');
            logPartialViewer = !!document.querySelector('.log-partial-viewer');
            
            setTimeout(() => {
                csharp.invokeMethodAsync("OnEscape", { hasModal: hasModal, hasLogPartialViewer: logPartialViewer });
            },1);
            return true;
        };
        document.addEventListener("keydown", window.CodeCaptureListener);
    },
    attachEventListeners: function(csharp) {
        window.addEventListener('blur', () => {csharp.invokeMethodAsync("EventListener", "WindowBlur")});
        document.addEventListener('click', () => {csharp.invokeMethodAsync("EventListener", "DocumentClick")});        
    },
    resizableEditor: function(uid) {
        let panel = document.getElementById(uid);
        if(!panel)
            return;
        
        if(panel.classList.contains('maximised'))
            return;

        const BORDER_SIZE = 4;

        function resize(e){
            let bWidth = document.body.clientWidth;
            let width = bWidth - e.x;
            panel.style.width = width + "px";
        }

        panel.addEventListener("mousedown", function(e){
            if(e.target !== panel)
                return;
            if (e.offsetX < BORDER_SIZE) {
                document.addEventListener("mousemove", resize, false);
            }
            e.preventDefault();
            e.stopPropagation();
            return false;
        }, false);

        document.addEventListener("mouseup", function(){
            document.removeEventListener("mousemove", resize, false);
        }, false);        
    },
    resetTable: function(uid, tableIdentifier) {
        console.log('reset table');
        localStorage.removeItem(tableIdentifier);        
        this.resizableTable(uid, tableIdentifier, true);
    },
    resizableTable: function(uid, tableIdentifier, reset){
        let div = document.getElementById(uid);
        if(!div)
            return;
        
        let existing = div.querySelectorAll('.resizer');
        for(let item of existing)
            item.remove();

        const getCssClass = function(name) {
            for(let ss of document.styleSheets)
            {
                for(let rule of ss.cssRules)
                {
                    if(rule.selectorText === '.' + name)
                        return rule;
                }
            }
        }
        
        const saveColumnInfo = function()
        {
            if(!tableIdentifier)
                return;
            const cols = div.querySelectorAll('.flowtable-header-row > span:not(.hidden)');
            let widths = [];
            for(let col of cols){
                const styles = window.getComputedStyle(col);
                w = parseInt(styles.width, 10);
                widths.push(w);
            }
            localStorage.setItem(tableIdentifier, JSON.stringify(widths));            
        }
        
        const getClassName = function(col){
            return col.className.match(/col\-[\d]+/)[0]
        } 
                
        const createResizableColumn = function (col, resizer, next) {
            let x = 0;
            let w = 0;
            let nW = 0;

            let colClassName = getClassName(col);
            let nextClassName = getClassName(next);
            let colClass = getCssClass(colClassName);
            let nextClass = getCssClass(nextClassName);
            
            const mouseDownHandler = function (e) {
                x = e.clientX;
                const styles = window.getComputedStyle(col);
                w = parseInt(styles.width, 10);
                const nextStyles = window.getComputedStyle(next);
                nW = parseInt(nextStyles.width, 10);
                document.addEventListener('mousemove', mouseMoveHandler);
                document.addEventListener('mouseup', mouseUpHandler);
                resizer.classList.add('resizing');
            };

            const mouseMoveHandler = function (e) {
                const dx = e.clientX - x;
                colClass.style.width = `${w + dx}px`;
                colClass.style.minWidth = `${w + dx}px`;
                nextClass.style.width = `${nW - dx}px`;
                nextClass.style.minWidth = `${nW - dx}px`;                
            };

            const mouseUpHandler = function () {
                resizer.classList.remove('resizing');
                document.removeEventListener('mousemove', mouseMoveHandler);
                document.removeEventListener('mouseup', mouseUpHandler);
                saveColumnInfo();
            };

            resizer.addEventListener('mousedown', mouseDownHandler);
        }
        
        const cols = div.querySelectorAll('.flowtable-header-row > span:not(.hidden)');
        let savedWidths = [];
        if(tableIdentifier != null){
            let saved = localStorage.getItem(tableIdentifier);
            if(saved){
                savedWidths = JSON.parse(saved);
            }
        }

        for(let i=0;i<cols.length;i++) {
            let col = cols[i];
            if (col.classList.contains('flowtable-select'))
                continue;

            let colClassName = getClassName(col);
            let colClass = getCssClass(colClassName);
            if (colClass) {
                if (reset) 
                {
                    let width = col.getAttribute('data-width');
                    if(width) {
                        colClass.style.width = width;
                        colClass.style.minWidth = width;
                    }
                    else
                    {
                        colClass.style.width = 'unset';
                        colClass.style.minWidth = 'unset';
                    }
                }
                else if (i < savedWidths.length && savedWidths[i] > 0) {
                    colClass.style.width = `${savedWidths[i]}px`;
                    colClass.style.minWidth = `${savedWidths[i]}px`;
                }
            }
            if(i < cols.length - 1) {
                const resizer = document.createElement('div');
                col.style.position = 'relative';
                resizer.classList.add('resizer');
                resizer.style.height = `${div.offsetHeight}px`;
                col.appendChild(resizer);
                createResizableColumn(col, resizer, cols[i + 1]);
            }
        }
    },
    stopSelectPropagation: function(event){
        if(event.ctrlKey === false && event.shiftKey === false)
        {
            event.stopPropagation();
            return false;
        }
    },
    toast: function(type, title, message, timeout){
        switch(type)
        {
            case 'info': Toast.info(title, message, timeout); break;
            case 'warn': Toast.warn(title, message, timeout); break;
            case 'success': Toast.success(title, message, timeout); break;
            case 'error': Toast.error(title, message, timeout); break;
        }
    },
    updateUrlWithNewUid: function(newUid) {
        // Get the current URL
        let currentUrl = window.location.href;
    
        // Replace the empty GUID with the new UID
        let updatedUrl = currentUrl.replace("00000000-0000-0000-0000-000000000000", newUid);
    
        // Update the URL
        window.history.replaceState({}, document.title, updatedUrl);
    },

    handleClickOutside: function(elementId, dotnetObjRef) {
        let clickHandler = function(event) {
            let element = document.getElementById(elementId);
            if (element && !element.contains(event.target)) {
                dotnetObjRef.invokeMethodAsync('OnOutsideClick');
            }
        };

        window.addEventListener('click', clickHandler);

        let elementClickHandlers = window.elementClickHandlers || {};
        elementClickHandlers[elementId] = clickHandler;
        window.elementClickHandlers = elementClickHandlers;
    },
    removeClickHandler: function(elementId) {
        let clickHandler = window.elementClickHandlers[elementId];
        if (clickHandler) {
            window.removeEventListener('click', clickHandler);
            delete window.elementClickHandlers[elementId];
        }
    }
};
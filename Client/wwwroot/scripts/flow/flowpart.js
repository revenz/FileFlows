class ffFlowPart
{
    constructor(ffFlow) {
        this.ffFlow = ffFlow;
        this.flowPartElements = [];
        this.fontSize = this.getFontSize();
        this.fpLineHeight = this.getFlowPartLineHeight();
    }
    
    getFontSize(){
        let value = getComputedStyle(document.body).getPropertyValue('--font-size');
        if(!value)
            value = 14;
        else
            value = parseFloat(value);
        return value;        
    }
    getFlowPartLineHeight(){
        let value = getComputedStyle(document.body).getPropertyValue('--flow-part-line-height');
        if(!value)
            value = this.fontSize * 1.2;
        else
            value = parseFloat(value);
        return value;
    }

    reset() {
        this.flowPartElements = [];
    }

    unselectAll() {
        for (let ele of this.flowPartElements) 
            ele.classList.remove('selected');
    }

    focusElement(uid) {
        let ele = document.getElementById(uid);
        if (ele)
            ele.focus();
    }
    
    initFlowPartColor(div, part){
        if(part.color)
            div.style.setProperty('--custom-color', part.color);
        else if(part.customColor)
            div.style.setProperty('--custom-color', part.customColor);
    }

    addFlowPart(part) {
        let div = document.createElement('div');
        this.flowPartElements.push(div);
        div.setAttribute('x-uid', part.uid);
        div.style.position = 'absolute';
        this.initFlowPartColor(div, part);
        let xPos = Math.floor(part.xPos / 10) * 10;
        let yPos = Math.floor(part.yPos / 10) * 10;
        div.style.left = xPos + 'px';
        div.style.top = yPos + 'px';
        div.classList.add('flow-part');
        if (typeof (part.type) === 'number') {
            if (part.type === 0)
                div.classList.add('Input');
            else if (part.type === 1)
                div.classList.add('Output');
            else if (part.type === 2)
                div.classList.add('Process');
            else if (part.type === 3)
                div.classList.add('Logic');
            else if (part.type === 4)
                div.classList.add('BuildStart');
            else if (part.type === 5)
                div.classList.add('BuildEnd');
            else if (part.type === 6)
                div.classList.add('BuildPart');
            else if (part.type === 7)
                div.classList.add('Failure');
            else if (part.type === 8)
                div.classList.add('Communication');
            else if (part.type === 9)
                div.classList.add('Script');
            else if (part.type === 10)
                div.classList.add('SubFlow');
        }

        // used for touch, currently disabled
        // let mc = new Hammer.Manager(div);
        // mc.add( new Hammer.Tap({ event: 'doubletap', taps: 2 }) );
        // mc.on("doubletap", (ev) => {
        //     // this is effect the double click/ctrl to open a sub flow instead of opening side editor
        //     this.editFlowPart(part.uid, false);
        // });
        div.classList.add('size-' + Math.max(part.inputs, part.outputs));
        
        div.setAttribute('tabIndex', -1);
        this.attachEventListeners({part: part, div: div});

        if (part.inputs > 0) {
            div.classList.add('has-input');
            let divInputs = document.createElement('div');
            divInputs.classList.add('inputs');
            div.appendChild(divInputs);
            for (let i = 1; i <= part.inputs; i++) {
                let divInput = document.createElement('div');
                let divInputInner = document.createElement('div');
                divInput.appendChild(divInputInner);
                divInput.setAttribute('x-uid', part.uid + '-input-' + i);
                divInput.setAttribute('x-input', i);
                divInput.classList.add('input');
                divInput.classList.add('input-' + i);
                divInput.addEventListener("onclick", function (event) {
                });
                divInput.addEventListener("contextmenu", function (event) {
                    event.preventDefault();
                    return false;
                });

                divInputs.appendChild(divInput);
            }
        }
        let divIconWrapper = document.createElement('div');
        divIconWrapper.classList.add('icon-wrapper');
        div.appendChild(divIconWrapper);
        if(/^svg:/.test(part.icon || ''))
        {
            let imgIcon = document.createElement('img');
            imgIcon.setAttribute('src', '/icons/' + part.icon.substring(4) + '.svg');
            divIconWrapper.appendChild(imgIcon);
        }
        else {
            let spanIcon = document.createElement('span');
            spanIcon.classList.add('icon');
            if (part.icon) {
                for (let picon of part.icon.split(' '))
                    spanIcon.classList.add(picon);
            }
            divIconWrapper.appendChild(spanIcon);
        }

        let divName = document.createElement('div');
        divName.classList.add('name');
        div.appendChild(divName);
        this.updateOutputNodes(part.uid, part, div);

        let divDraggable = document.createElement('div');
        divDraggable.classList.add('draggable');
        divDraggable.addEventListener("contextmenu", (e) => { this.ffFlow.contextMenu(e, part); return false; }, false);

        div.appendChild(divDraggable);

        this.ffFlow.eleFlowParts.appendChild(div);

        this.setPartName(part);
        this.ffFlow.initOutputHints(part);
        return div;
    }
    
    attachEventListeners(args){
        let part = args.part;
        let div = args.div;
        let allEvents = args.allEvents;
        if(!div)
            div = this.ffFlow.getFlowPart(part.uid);

        if(this.ffFlow.readOnly === false) {
            div.addEventListener("click", (event) => {
                event.stopImmediatePropagation();
                event.preventDefault();
                this.unselectAll();
                div.classList.add('selected');
                this.ffFlow.SelectedParts = [part];
                this.ffFlow.selectNode(part);
            });
            div.addEventListener("keydown", (event) => {
                if (event.code === 'Delete' || event.code === 'Backspace') {
                    this.deleteFlowPart(part.uid);
                    event.stopImmediatePropagation();
                    event.preventDefault();
                } else if (event.code === 'Enter') {
                    this.editFlowPart(part.uid);
                    event.stopImmediatePropagation();
                    event.preventDefault();
                }
            });
            div.addEventListener("contextmenu", (event) => {
                event.preventDefault();
                return false;
            });
            if (allEvents) {
                for (let output of div.querySelectorAll('.output div')) {
                    this.attachOutputNodeEvents(output);
                }
            }
        }

        div.addEventListener("dblclick", (event) => {
            event.stopImmediatePropagation();
            event.preventDefault();
            if(event.ctrlKey) {
                this.ffFlow.csharp.invokeMethodAsync("CtrlDblClick", part);
            } else if(this.ffFlow.readOnly === false){
                this.ffFlow.setInfo(part.Name, 'Node');
                this.editFlowPart(part.uid);
            }
        });
    }
    
    attachOutputNodeEvents(divOutput) {
        divOutput.addEventListener("click", (event) => {            
            event.stopImmediatePropagation();
            event.preventDefault();
        });
        divOutput.addEventListener("mousedown", (event) => {
            event.stopImmediatePropagation();
            event.preventDefault();
            this.ffFlow.FlowLines.ioDown(event);
        });
    }
    
    setPartName(part) {
        try {
            let div = this.ffFlow.getFlowPart(part.uid);
            let divName = div.querySelector('.name');
            if (!divName) 
                return;
            let name = part.name;
            if (!name)
                name = part.label;
            if (!name) 
                name = part.flowElementUid.substring(part.flowElementUid.lastIndexOf('.') + 1);
            
            part.displayName = name;
            divName.innerHTML = name;

            // Iterate over the classes in reverse order and remove those that start with "height-"
            for (let i = div.classList.length - 1; i >= 0; i--) {
                let className = div.classList[i];
                if (className.startsWith('height-')) {
                    div.classList.remove(className);
                }
            }
            
            let lines = this.getLineCount(name, part.outputs);
            div.classList.add('height-' + lines);
        } catch (err) {
            console.error(err);
        }
    }
    
    getNameWidth(outputs) {
        const unit = 10;
        const pad = 3.92 * unit;
        let oSpacing;
    
        if (outputs < 4) {
            oSpacing = 4 * unit;
        } else if (outputs < 6) {
            oSpacing = 3 * unit;
        } else {
            oSpacing = 4 * unit / 2;
        }
    
        let width = pad + (oSpacing * Math.max(2, outputs)) + pad;
        width -= 3 * unit; // remove the left icon    
        return width;
    }
    
    getLineCount(text, outputs) {
        let width = this.getNameWidth(outputs);
        let div = document.createElement('div');
        div.style.width = width + 'px';
        div.innerText = text;
        // get number of lines needed for div
        let fontSize = this.fontSize || 14;
        let lineHeight = this.fpLineHeight || fontSize * 1.2;
        div.style.fontSize = fontSize + 'px';
        div.style.lineHeight = lineHeight + 'px';

        // Append the temporary div to the body (this is necessary to measure its height accurately)
        document.body.appendChild(div);

        // Get the height of the temporary div
        let height = div.clientHeight;

        // Remove the temporary div from the body
        document.body.removeChild(div);

        // Calculate the number of lines based on the height and line height
        let numberOfLines = Math.round(height / lineHeight);
        return Math.min(3, numberOfLines); // 3 line limit
        
        
        // let lineLength = Math.max(20, div.querySelectorAll('.output').length * 3);
        // console.log('line length: ' + lineLength);
        // let lines = Math.round(name.length / lineLength);
        // lines = Math.min(lines, 3) + 1;
    }

    deleteFlowPart(uid) 
    {
        if(uid && this.ffFlow.getFlowPart(uid))
            this.ffFlow.History.perform(new FlowActionDelete(this.ffFlow, uid));
    }

    editFlowPart(uid, deleteOnCancel) {
        let part = this.ffFlow.parts.filter(x => x.uid === uid)[0];
        if (!part || part.readOnly)
            return;

        this.ffFlow.csharp.invokeMethodAsync("Edit", part, deleteOnCancel === true).then(result => {
            if (!result || !result.model) {
                if (deleteOnCancel === true) {
                    this.deleteFlowPart(uid);
                }
                return; // editor was canceled
            }
            if (result.model.Name) {
                part.name = result.model.Name;
                delete result.model.Name;
            } else if (result.model.Name === '') {
                part.name = '';
                let dn = this.getNodeName(part.flowElementUid);
                if (dn) 
                    part.label = dn;
                delete result.model.Name;
            }
            if (result.model.Color) {
                part.color = result.model.Color;
                delete result.model.Color;
            } else if (result.model.Color === '') {
                part.color = '';
                delete result.model.Color;
            }
            let div = this.ffFlow.getFlowPart(part.uid);
            this.initFlowPartColor(div, part);
            part.model = result.model;

            this.setPartName(part);

            if (result.outputs >= 0) {
                part.outputs = result.outputs;
                // have to update any connections in case they are no long available
                this.updateOutputNodes(part.uid, part);
                this.ffFlow.redrawLines();
            }
            this.ffFlow.initOutputHints(part);
            this.ffFlow.redrawLines();

        });
    }

    getNodeName(elementUid) {
        let node = this.ffFlow.elements.filter(x => x.uid === elementUid)[0];
        if (node)
            return node.displayName;
        return '';
    }


    updateOutputNodes(uid, part, div) {
        if (!part)
            part = this.ffFlow.parts.filter(x => x.uid === uid)[0];
        if (!part) {
            return;
        }
        if (!div) {
            div = this.ffFlow.getFlowPart(uid);
            if (!div) {
                return;
            }
        }
        for (let i = 1; i < 100; i++) {
            div.classList.remove('size-' + i);
        }
        div.classList.add('size-' + Math.max(part.outputs, 1));

        let divOutputs = div.querySelector('.outputs');

        //if (part.outputs > 0) {
            if (!divOutputs) {
                divOutputs = document.createElement('div');
                div.appendChild(divOutputs);
            }
            else {
                while (divOutputs.hasChildNodes()) {
                    divOutputs.removeChild(divOutputs.firstChild);
                }
            }
            divOutputs.className = 'outputs outputs-' + Math.max(part.outputs, 1);
            for (let i = 0; i <= part.outputs; i++) {
                if(i === 0 && !part.inputs)
                    continue;
                let divOutput = document.createElement('div');
                let divOutputInner = document.createElement('div');
                let index = i === 0 ? -1 : i;
                divOutput.appendChild(divOutputInner);
                divOutput.setAttribute('x-uid', part.uid + '-output-' + index);
                divOutput.setAttribute('x-output', i);
                divOutput.classList.add('output');
                divOutput.classList.add('output-' + index);
                this.attachOutputNodeEvents(divOutput);
                divOutputs.appendChild(divOutput);
            }
        // } else if (divOutputs) {
        //     divOutputs.remove();
        // }

        // delete any connections
        for (let i = part.outputs + 1; i < 100; i++) {
            this.ffFlow.FlowLines.ioOutputConnections.delete(part.uid + '-output-' + i);
        }
    }

}
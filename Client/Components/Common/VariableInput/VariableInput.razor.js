/**
 * Creates the VariableInput instance and returns it
 * @param dotNetObject The calling dotnet object
 * @param uid the UID of the input element
 * @param variables the Dictionary<string, object> from C# of available variables to show
 * @returns {VariableInput} the variable input javascript instance
 */
export function createVariableInput(dotNetObject, uid, variables)
{
    return new VariableInput(dotNetObject, uid, variables);
}

/**
 * VariableInput JavaScript file
 */
export class VariableInput {
    constructor(dotNetObject, uid, variables) {
        this.dotNetObject = dotNetObject;
        this.uid = uid;
        this.variables = variables;
        this.dropdownIndex = 0;
        this.currentVariable = "";
        this.dropdownShownAtLeastOnce = false;

        // Reference to the textarea element
        this.textInput = document.getElementById(uid);
        if(!this.textInput)
            return;

        // Create dropdown element
        this.dropdown = document.createElement("ul");
        this.dropdown.id = "ta-var-dropdown-" + uid;
        this.dropdown.className = "ta-var-dropdown";
        this.dropdown.style.position = "absolute";
        this.dropdown.style.display = "none"; // Initially hidden
        document.body.appendChild(this.dropdown);

        // Event listeners
        this.textInput.addEventListener("keydown", this.handleKeydown.bind(this));
        this.textInput.addEventListener('click', this.handleClick.bind(this));


        // CSS for the dropdown
        const dropdownStyle = document.createElement("style");
        dropdownStyle.textContent = `
            .ta-var-dropdown {
                background: var(--base-lighter);
                border: solid 1px var(--input-background);
                padding: 5px;
                list-style-type: none;
                z-index: 1000;
                max-height: 14rem;
                overflow: auto;
            }
            .ta-var-dropdown li {
                cursor: pointer;
                padding: 3px;
            }
            .ta-var-dropdown li:hover {
                background-color: var(--accent);
            }
            .ta-var-dropdown li.selected {
                background-color: rgba(var(--accent-rgb), 0.7);
            }
        `;
        document.head.appendChild(dropdownStyle);
    }

    updateValue(value) {
        this.dotNetObject.invokeMethodAsync("updateValue", value);
    }

    handleClick(event)
    {
        if (this.isDropdownVisible())
        {
            this.removeBrackets();
        }
    }

    handleKeydown(event) {
        const ctrlSpacePressed = event.ctrlKey && event.key === " ";
        if (ctrlSpacePressed) {
            const cursorPosition = this.textInput.selectionStart;
            const value = this.textInput.value;

            // Check if the cursor is inside a {} block
            const openBraceIndex = value.lastIndexOf("{", cursorPosition - 1);
            const closeBraceIndex = value.indexOf("}", openBraceIndex);

            if (openBraceIndex !== -1 && (closeBraceIndex === -1 || cursorPosition <= closeBraceIndex)) {
                // Inside a {} block, show the dropdown without inserting new {}
                const partialVariable = value.substring(openBraceIndex + 1, cursorPosition);
                this.showDropdown(partialVariable);
                this.updateDropdown(partialVariable); // Show dropdown with the partial variable
                event.preventDefault();
                return;
            }
        }
        if (event.key === "{" || ctrlSpacePressed) {            
            this.showDropdown();
            event.preventDefault();
        }  else if (this.isDropdownVisible()) {
            // Handle arrow keys, Enter, and Escape in the dropdown
            if (event.key === "ArrowUp") {
                event.preventDefault();
                this.selectPrevious();
            } else if (event.key === "ArrowDown") {
                event.preventDefault();
                this.selectNext();
            } else if (event.key === "Enter") {
                event.preventDefault();
                this.insertSelectedVariable();
                this.hideDropdown();
            } else if (event.key === "Escape") {
                event.preventDefault();
                this.removeBrackets(); // Remove brackets and hide dropdown
            } else if (event.key === "Backspace") {
                if (this.currentVariable === "") {
                    this.removeBrackets(); // Remove brackets if currentVariable is empty
                } else {
                    this.updateVariableText(this.currentVariable.slice(0, -1)); // Call updateVariableText method
                }
            }else if (event.key.length === 1) {
                // Check if adding the typed key forms a valid variable
                const potentialVariable = this.currentVariable + event.key;
                //if (this.isValidVariable(potentialVariable)) { 
                // ^^ allow them to write this character regardless, in case a variable is there just not in the list
                // Update currentVariable and filter variables based on the typed key
                this.updateVariableText(potentialVariable); // Call updateVariableText method
                this.selectItem(0);
                //}
            }
            event.preventDefault();
        }
        else if (event.key === "Escape" && this.dropdownShownAtLeastOnce) 
        {
            event.preventDefault();
            // a user may hit escape to clean up a drop down which could cause an error
        }
    }

    updateVariableText(newText) {
        // Update currentVariable
        this.currentVariable = newText;

        this.textInput.value = this.textBeforeCursor + '{' + newText + '}' + this.textAfterCursor;

        // Set cursor position after inserted variable
        const newCursorPosition = this.cursorPosition + newText.length;
        this.textInput.setSelectionRange(newCursorPosition, newCursorPosition);
        this.updateDropdown(newText);
    }

    removeBrackets() {
        this.textInput.value = this.textBeforeCursor + this.textAfterCursor;
        const position = Math.max(0, this.cursorPosition - 1);
        this.textInput.setSelectionRange(position, position);
        this.hideDropdown();
    }

    isDropdownVisible() {
        return this.dropdown.style.display === "block";
    }

    updateDropdown(partialVariable) {
        const filteredVariables = Object.keys(this.variables).filter(variable => variable.startsWith(partialVariable));
        if (filteredVariables.length > 0) {
            // Update dropdown options
            this.dropdown.innerHTML = ""; // Clear existing options
            filteredVariables.forEach((variable, index) => {
                const listItem = document.createElement("li");
                listItem.textContent = variable;
                listItem.addEventListener("click", () => {
                    this.insertSelectedVariable(variable);
                    this.hideDropdown();
                });
                if (index === this.dropdownIndex) {
                    listItem.classList.add("selected");
                }
                this.dropdown.appendChild(listItem);
            });
        } else {
            this.hideDropdown();
        }
    }
    showDropdown(initialText) {
        this.dropdownShownAtLeastOnce = true;
        let cursor = this.textInput.selectionStart;
        let text = this.textInput.value;
        let startBracketIndex = this.getStartBracketLocation(cursor, text);
        let endBracketIndex = startBracketIndex === -1 ? -1 : this.getEndBracketLocation(startBracketIndex, text);
        if (!initialText) {
            this.currentVariable = '';
            this.updateDropdown(""); // Show all variables initially

            if (startBracketIndex < 0 && endBracketIndex < 0) {
                // No existing {} around the cursor, insert {}
                this.textInput.setRangeText("{", cursor, cursor, "end");
                this.textInput.setRangeText("}", cursor + 1, cursor + 1, "end");
                startBracketIndex = cursor;
                endBracketIndex = cursor + 2;
                cursor = cursor + 1;
            } else if (endBracketIndex < 0) {
                // Open { exists, but no closing } yet, add the closing brace
                this.textInput.setRangeText("}", cursor + 1, cursor, "end");
                endBracketIndex = cursor + 1;
            }
        } else {
            this.currentVariable = initialText;
            this.updateDropdown(initialText);
        }

        // Cache the text before and after the cursor
        this.cursorPosition = cursor;
        this.textBeforeCursor = this.textInput.value.substring(0, startBracketIndex);
        this.textAfterCursor = this.textInput.value.substring(endBracketIndex);
        if(this.textAfterCursor.startsWith('}'))
            this.textAfterCursor = this.textAfterCursor.substring(1);

        this.textInput.setSelectionRange(startBracketIndex + 1, endBracketIndex);

        const rect = this.textInput.getBoundingClientRect();
        const lineHeight = parseInt(getComputedStyle(this.textInput).lineHeight);
        const scrollTop = this.textInput.scrollTop; // Get the scrollTop of the textarea

        // Calculate the top position relative to the viewport, accounting for the scroll position
        const linesAboveCursor = this.textInput.value.substring(0, this.cursorPosition).split('\n').length;
        const topPosition = rect.bottom + window.pageYOffset + lineHeight * (linesAboveCursor - 1) - scrollTop;

        // Set the dropdown position
        this.dropdown.style.top = topPosition + "px";
        this.dropdown.style.left = rect.left + "px";

        // Set the dropdown width to match the textarea width
        this.dropdown.style.width = rect.width + "px";

        this.dropdown.style.display = "block";
        this.selectItem(0);
    }

    /**
     * Gets the position of the last start {, or -1 if no valid start { is found
     * @param cursorIndex the index of the current cursor
     * @param text the text in to look for
     */
    getStartBracketLocation(cursorIndex, text) {
        let index = text.lastIndexOf('{', cursorIndex);
        if (index < 0)
            return -1;
        let textAfter = text.substring(index + 1, cursorIndex);
        if (/}/.test(textAfter))
            return -1;
        return index;
    }

    /**
     * Gets the position of the end }, or -1 if no valid end } is found
     * @param startBracketIndex the index of the start bracket
     * @param text the text in to look for
     */
    getEndBracketLocation(startBracketIndex, text) {
        text = text.substring(startBracketIndex + 1);
        let index = text.indexOf('}');
        if (index < 0)
            return -1;
        if (/{/.test(text.substring(0, index)))
            return -1;
        return startBracketIndex + index + 1;
    }



    hideDropdown() {
        this.dropdown.style.display = "none";
    }

    selectNext() {
        this.selectItem('next')
    }

    selectPrevious() {
        this.selectItem('previous');
    }
    selectItem(direction) {
        const listItems = this.dropdown.getElementsByTagName("li");
        let newIndex;

        if(direction === 0){
            newIndex = 0;
        } else if (direction === "next") {
            newIndex = this.dropdownIndex < listItems.length - 1 ? this.dropdownIndex + 1 : 0;
        } else {
            newIndex = this.dropdownIndex > 0 ? this.dropdownIndex - 1 : listItems.length - 1;
        }

        if(listItems.length > this.dropdownIndex)
            listItems[this.dropdownIndex].classList.remove("selected");
        listItems[newIndex].classList.add("selected");

        // Scroll into view only if the selected item is not fully visible
        const itemRect = listItems[newIndex].getBoundingClientRect();
        if (itemRect.bottom > this.dropdown.clientHeight || itemRect.top < 0) {
            listItems[newIndex].scrollIntoView({ behavior: "smooth", block: "nearest", inline: "nearest" });
        }

        this.dropdownIndex = newIndex;
    }

    insertSelectedVariable() {
        const listItems = this.dropdown.getElementsByTagName("li");
        const selectedVariable = listItems[this.dropdownIndex].textContent;
        const newText = this.textBeforeCursor + '{' + selectedVariable + '}' + this.textAfterCursor;
        this.textInput.value = newText;

        // Set cursor position after inserted variable
        const newCursorPosition = this.textBeforeCursor.length + selectedVariable.length + 2;
        this.textInput.setSelectionRange(newCursorPosition, newCursorPosition);
        this.updateValue(newText);
    }

    isValidVariable(partialVariable) {
        // Check if any variable starts with the given partialVariable
        return Object.keys(this.variables).some(variable => variable.startsWith(partialVariable));
    }
}

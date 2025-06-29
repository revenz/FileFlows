window.createTimeSpanInput = function(eleUid, jsInvokeRef) {
    let updateFunction = (value) => jsInvokeRef.invokeMethodAsync("OnTimeSpanChange", value);
    
    return new TimeSpanInput(eleUid, updateFunction);
}

class TimeSpanInput {
    constructor(elementUid, updateValueCallback) {
        this.element = document.getElementById(elementUid);
        this.updateValueCallback = updateValueCallback;

        // Check if the input value is in the format 00:00:00
        const regex = /^([01]?[0-9]|2[0-3]):[0-5][0-9]:[0-5][0-9]$/;
        const inputValue = this.element.value.trim();
        if (regex.test(inputValue)) {
            const segments = inputValue.split(':').map(segment => parseInt(segment, 10));
            this.value = segments;
        } else {
            this.value = [0, 0, 0]; // Default to 00:00:00
        }

        this.currentSegment = 0; // 0 for Hours, 1 for Minutes, 2 for Seconds
        this.element.addEventListener('keydown', this.handleKeyDown.bind(this));
        this.render();
    }

    handleKeyDown(event) {
        const key = event.key;
        const cursorPosition = this.element.selectionStart;
        this.currentSegment = Math.min(Math.floor(cursorPosition / 3), 2); // Limit segment index to 0, 1, 2

        if (/^\d$/.test(key)) {
            this.updateSegment(parseInt(key));
            event.preventDefault();
        } else if (key === 'ArrowUp') {
            this.incrementSegment();
            event.preventDefault();
        } else if (key === 'ArrowDown') {
            this.decrementSegment();
            event.preventDefault();
        } else if (event.key === 'Backspace' || event.key === 'Delete') {
            this.handleBackspace();
            event.preventDefault();
        } else if (key === ':') {
            this.moveToNextSegment();
            event.preventDefault();        
        } else if (event.key.length === 1) {
            event.preventDefault(); // Prevent non-digit characters from being entered
        }
    }

    updateSegment(value) {
        const currentValue = this.value[this.currentSegment];
        
        let segmentMax = this.currentSegment === 0 ? 23 : 59;
        if(currentValue === segmentMax)
        {
            this.value[this.currentSegment] = value;            
        }
        else 
        {
            let newValue = (currentValue % 10 * 10 + value) % 100;
            if (newValue > segmentMax)
                newValue = segmentMax; 
            this.value[this.currentSegment] = newValue;
        }

        this.render();
        this.updateValueCallback(this.value.join(':'));
    }


    handleBackspace() {
        const currentValue = this.value[this.currentSegment];
        this.value[this.currentSegment] = Math.floor(currentValue / 10);

        this.render();
        this.updateValueCallback(this.value.join(':'));
    }

    incrementSegment() {
        const maxValues = [24, 60, 60];
        if (this.value[this.currentSegment] < maxValues[this.currentSegment] - 1) {
            this.value[this.currentSegment]++;
        } else {
            this.value[this.currentSegment] = 0;
        }
        this.render();
        this.updateValueCallback(this.value.join(':'));
    }

    decrementSegment() {
        if (this.value[this.currentSegment] > 0) {
            this.value[this.currentSegment]--;
        } else {
            this.value[this.currentSegment] = this.currentSegment === 0 ? 24 : 59;
        }
        this.render();
        this.updateValueCallback(this.value.join(':'));
    }

    moveToNextSegment() {
        this.currentSegment = (this.currentSegment + 1) % 3;
    }

    render() {
        this.element.value = this.value.map(num => num.toString().padStart(2, '0')).join(':');
        this.element.setSelectionRange(this.currentSegment * 3, this.currentSegment * 3 + 2);
    }
}

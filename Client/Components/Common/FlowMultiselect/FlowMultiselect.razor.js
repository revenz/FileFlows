/**
 * Creates and initializes an `Uploader` instance.
 *
 * @param {object} dotNetObject - The .NET object
 * @param {string} uid - The unique identifier for the input element.
 * @param {array} options - The options to show
 * @returns {Multiselect} - The created `Multiselect` instance.
 */
export function createMultiselect(dotNetObject, uid, options) {
    return new Multiselect(dotNetObject, uid, options);
}

/**
 * Class representing a multiselect input
 */
export class Multiselect {
    constructor(dotNetObject, uid, options) {
        this.container = document.getElementById(uid);
        this.multiselect__tags = this.container.querySelector('.multiselect__tags');
        this.multiselect__options = this.container.querySelector('.multiselect__options');
        this.input = this.container.querySelector(".multiselect__input");
        this.options = options || [];
        this.filteredOptions = [...this.options]; // Copy of options for filtering
        this.selected = [];
        this.dotNetObject = dotNetObject;
        this.init();
    }

    init() {
        this.render();
        this.setupInitialEvents();
    }

    render() {
        // Filter out already selected items from the options
        const availableOptions = this.filteredOptions.filter(
            (option) => !this.selected.includes(option.value)
        );

        // Render selected tags
        this.multiselect__tags.innerHTML = `${this.selected
            .map(
                (value) =>
                    `<span class="multiselect__tag">${this.getOptionLabel(
                        value
                    )}<span class="multiselect__remove" data-value="${value}">&times;</span></span>`
            )
            .join("")}`;

        // Render available options
        this.multiselect__options.innerHTML = `
          ${availableOptions
            .map(
                (option) =>
                    `<li class="multiselect__option" data-value="${option.value}">${option.label}</li>`
            )
            .join("")}`;
    }

    setupInitialEvents() {
        const dropdown = this.container.querySelector(".multiselect__dropdown");

        // Open/close dropdown on input focus/blur
        this.input.addEventListener("focus", () => dropdown.classList.add("open"));
        this.input.addEventListener("blur", () =>
            setTimeout(() => dropdown.classList.remove("open"), 200)
        );

        // Filter options on input with debouncing
        let debounceTimeout;
        this.input.addEventListener("input", () => {
            clearTimeout(debounceTimeout);
            debounceTimeout = setTimeout(() => {
                const filter = this.input.value.toLowerCase();
                this.filteredOptions = this.options
                    .filter(
                        (option) =>
                            option.label.toLowerCase().includes(filter) &&
                            !this.selected.includes(option.value) // Exclude selected items
                    )
                    .sort((a, b) => {
                        const startsWithA = a.label.toLowerCase().startsWith(filter);
                        const startsWithB = b.label.toLowerCase().startsWith(filter);

                        if (startsWithA && !startsWithB) return -1;
                        if (!startsWithA && startsWithB) return 1;
                        return a.label.localeCompare(b.label);
                    });

                this.render();
            }, 300); // 300ms debounce
        });

        // Handle Enter key to select an option
        this.input.addEventListener("keydown", (e) => {
            if (e.key === "Enter") {
                if (this.filteredOptions.length > 0 && this.input.value.trim().length > 0) {
                    const value = this.filteredOptions[0].value;
                    if (!this.selected.includes(value)) {
                        this.addSelected(value);
                        this.input.value = ""; // Clear the input
                        this.filteredOptions = [...this.options]; // Reset filter
                        this.render();
                    }
                }
            }
        });

        // Handle tag removal using event delegation
        this.multiselect__tags.addEventListener("click", (e) => {
            if (e.target.classList.contains("multiselect__remove")) {
                const value = e.target.dataset.value;
                this.removeSelected(value);
            }
        });

        // Handle option selection using event delegation
        this.multiselect__options.addEventListener("click", (e) => {
            if (e.target.classList.contains("multiselect__option")) {
                const value = e.target.dataset.value;
                if (!this.selected.includes(value)) {
                    this.addSelected(value);
                    this.input.value = ""; // Clear the input
                    this.filteredOptions = [...this.options]; // Reset filter
                    this.render();
                }
            }
        });

        // Open dropdown and focus input when clicking on the container
        this.container.addEventListener("click", (e) => {
            if (!e.target.classList.contains("multiselect__remove")) {
                dropdown.classList.add("open");
                this.input.focus();
            }
        });
    }

    addSelected(value) {
        this.selected.push(value);
        this.notifyBlazor();
        this.render();
    }

    removeSelected(value) {
        this.selected = this.selected.filter((item) => item !== value);
        this.notifyBlazor();
        this.render();
    }

    getOptionLabel(value) {
        const option = this.options.find((o) => o.value === value);
        return option ? option.label : value;
    }

    // Update selected values from Blazor
    update(selectedValues) {
        this.selected = selectedValues.map((value) => value.toString());
        this.render();
    }

    // Notify Blazor of changes
    notifyBlazor() {
        if (this.dotNetObject) {
            this.dotNetObject.invokeMethodAsync("UpdateSelectedValues", this.selected);
        }
    }
}

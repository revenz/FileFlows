export function addClickOutsideListener(elementRef, dotnetHelper) {
    const element = elementRef instanceof HTMLElement ? elementRef : elementRef instanceof Object && elementRef instanceof Element ? elementRef : elementRef && elementRef.getBoundingClientRect ? elementRef : elementRef; // defensive fallback

    const handler = (event) => {
        // This resolves the element if passed from Blazor
        const domElement = element instanceof HTMLElement ? element : element?.[0] || element;

        if (!domElement || typeof domElement.contains !== 'function') {
            console.warn("clickOutside: Invalid element passed", domElement);
            return;
        }

        if (!domElement.contains(event.target)) {
            dotnetHelper.invokeMethodAsync('NotifyClickOutside');
        }
    };

    element._clickOutsideHandler = handler;
    document.addEventListener('click', handler);
}

export function removeClickOutsideListener(elementRef) {
    const element = elementRef instanceof HTMLElement ? elementRef : elementRef?.[0] || elementRef;
    const handler = element._clickOutsideHandler;
    if (handler) {
        document.removeEventListener('click', handler);
        delete element._clickOutsideHandler;
    }
}

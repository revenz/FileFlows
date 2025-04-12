export function addClickOutsideListener(selector, dotnetHelper) {
    const element = document.querySelector(selector);

    const handler = (event) => {
        let targetElement = event.target;
        if (!document.body.contains(targetElement)) {
            return;
        }
        if (!element.contains(event.target)) {
            dotnetHelper.invokeMethodAsync('NotifyClickOutside');
        }
    };

    element._clickOutsideHandler = handler;
    document.addEventListener('click', handler);
}

export function removeClickOutsideListener(elementRef) {
    const element = document.querySelector(selector);
    const handler = element._clickOutsideHandler;
    if (handler) {
        document.removeEventListener('click', handler);
        delete element._clickOutsideHandler;
    }
}

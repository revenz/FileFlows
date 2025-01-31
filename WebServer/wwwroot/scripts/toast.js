class Toast {
    static toastContainer = null;

    static info(title, message, timeout = 5000, svg = false) {
        Toast.showToast('info', title, message, timeout, svg);
    }

    static error(title, message, timeout = 5000, svg = false) {
        Toast.showToast('error',  title, message, timeout, svg);
    }

    static warn(title, message, timeout = 5000, svg = false) {
        Toast.showToast('warn', title, message, timeout, svg);
    }

    static success(title, message, timeout = 5000, svg = false) {
        Toast.showToast('success', title, message, timeout, svg);
    }
    static showToast(type, title, message, timeout, svg) {
        if (!Toast.toastContainer) {
            Toast.createToastContainer();
        }

        if(title && !message){
            message = title;
            title = '';
        }
        if(!timeout)
            timeout = 5000;
        svg = !!svg;

        const toast = document.createElement('div');
        toast.classList.add('ff-toast', type);
        toast.innerHTML = `
          <div class="toast-content">
            <span class="toast-icon">${svg ? Toast.getSvg(type) : `<i class="fas fa-${Toast.getIcon(type)}"></i>`}</span>
            <span class="toast-message">
                ${title ? `<span class="title">${title}</span>` : ''}
                <span class="message">${message}</message>
            </span>
            <span class="toast-close"><i class="fas fa-times"></i></span>
          </div>
        `;

        const toastClose = toast.querySelector('.toast-close');
        toastClose.addEventListener('click', () => {
            Toast.removeToast(toast);
        });

        Toast.toastContainer.appendChild(toast);
        setTimeout(() => {
            toast.classList.add('show');
        }, 100);

        setTimeout(() => {
            Toast.removeToast(toast);
        }, timeout);

        toast.addEventListener('mouseenter', () => {
            clearTimeout(toast.dismissTimeout);
        });

        toast.addEventListener('mouseleave', () => {
            toast.dismissTimeout = setTimeout(() => {
                Toast.removeToast(toast);
            }, timeout);
        });
    }


    static createToastContainer() {
        Toast.toastContainer = document.createElement('div');
        Toast.toastContainer.classList.add('toast-container');
        document.body.appendChild(Toast.toastContainer);
    }

    static removeToast(toast) {
        toast.classList.add('hide');
        setTimeout(() => {
            if(!Toast.toastContainer)
                return;
            if (toast.parentNode === Toast.toastContainer) {
                Toast.toastContainer.removeChild(toast);
            }
            if (Toast.toastContainer.childElementCount === 0) {
                document.body.removeChild(Toast.toastContainer);
                Toast.toastContainer = null;
            }
        }, 500);
    }

    static getIcon(type) {
        switch (type) {
            case 'success':
                return 'check-circle';
            case 'warn':
            case 'warning':
                return 'exclamation-triangle';
            case 'info':
                return 'info-circle';
            case 'error':
                return 'times-circle';
            default:
                return '';
        }
    }

    static getSvg(type){
        switch(type)
        {
            case 'success':
                return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path fill="var(--success)" d="M504 256c0 137-111 248-248 248S8 393 8 256 119 8 256 8s248 111 248 248zM227.3 387.3l184-184c6.2-6.2 6.2-16.4 0-22.6l-22.6-22.6c-6.2-6.2-16.4-6.2-22.6 0L216 308.1l-70.1-70.1c-6.2-6.2-16.4-6.2-22.6 0l-22.6 22.6c-6.2 6.2-6.2 16.4 0 22.6l104 104c6.2 6.2 16.4 6.2 22.6 0z"/></svg>'
            case 'error':
                return '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 512 512"><path fill="var(--error)" d="M256 8C119 8 8 119 8 256s111 248 248 248 248-111 248-248S393 8 256 8zm121.6 313.1c4.7 4.7 4.7 12.3 0 17L338 377.6c-4.7 4.7-12.3 4.7-17 0L256 312l-65.1 65.6c-4.7 4.7-12.3 4.7-17 0L134.4 338c-4.7-4.7-4.7-12.3 0-17l65.6-65-65.6-65.1c-4.7-4.7-4.7-12.3 0-17l39.6-39.6c4.7-4.7 12.3-4.7 17 0l65 65.7 65.1-65.6c4.7-4.7 12.3-4.7 17 0l39.6 39.6c4.7 4.7 4.7 12.3 0 17L312 256l65.6 65.1z"/></svg>';
        }
        return '';
    }
}
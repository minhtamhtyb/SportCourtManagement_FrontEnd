<<<<<<< HEAD
(function () {
    const TOAST_DURATION = 4500;

    function getContainer() {
        let el = document.getElementById('toastContainer');
        if (!el) {
            el = document.createElement('div');
            el.id = 'toastContainer';
            el.className = 'toast-container';
            el.setAttribute('aria-live', 'polite');
            el.setAttribute('aria-atomic', 'true');
            document.body.appendChild(el);
        }
        return el;
    }

    function iconFor(type) {
        if (type === 'success') return 'fa-circle-check';
        if (type === 'warning') return 'fa-triangle-exclamation';
        return 'fa-circle-exclamation';
    }

    window.showToast = function (message, type) {
        if (!message) return;

        type = type || 'error';
        const container = getContainer();
        const toast = document.createElement('div');
        toast.className = 'admin-toast admin-toast-' + type;
        toast.innerHTML =
            '<i class="fa-solid ' + iconFor(type) + ' admin-toast-icon"></i>' +
            '<span class="admin-toast-message">' + escapeHtml(message) + '</span>' +
            '<button type="button" class="admin-toast-close" aria-label="Đóng">&times;</button>';

        toast.querySelector('.admin-toast-close').addEventListener('click', function () {
            dismissToast(toast);
        });

        container.appendChild(toast);
        requestAnimationFrame(function () {
            toast.classList.add('show');
        });

        setTimeout(function () {
            dismissToast(toast);
        }, TOAST_DURATION);
    };

    function dismissToast(toast) {
        if (!toast || toast.classList.contains('hide')) return;
        toast.classList.remove('show');
        toast.classList.add('hide');
        toast.addEventListener('transitionend', function () {
            toast.remove();
        }, { once: true });
        setTimeout(function () {
            toast.remove();
        }, 400);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    document.getElementById('sidebarToggle')?.addEventListener('click', function () {
        document.getElementById('adminSidebar')?.classList.toggle('collapsed');
    });

    document.addEventListener('DOMContentLoaded', function () {
        const flash = document.getElementById('adminFlashData');
        if (!flash) return;

        const success = flash.dataset.success;
        const error = flash.dataset.error;
        if (success) showToast(success, 'success');
        if (error) showToast(error, 'error');
    });
})();
=======
document.getElementById('sidebarToggle')?.addEventListener('click', function () {
    document.getElementById('adminSidebar')?.classList.toggle('collapsed');
});
>>>>>>> 27f494423e01f6551489a0125ff0c2254db9326e

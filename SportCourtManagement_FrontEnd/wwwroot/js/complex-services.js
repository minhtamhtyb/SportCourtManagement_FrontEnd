/* complex-services.js
 * Handles AJAX CRUD (Add / Edit / Delete) for ComplexCourtTypeService offerings.
 * Works with Services.cshtml:
 *   - #serviceModal          → Add / Edit form (loaded via partial)
 *   - #deleteConfirmModal    → Styled confirm before delete
 *   - #toastContainer        → Toast notifications
 */
(function () {
    'use strict';

    // ── DOM refs ─────────────────────────────────────────────────────────────
    const modalEl        = document.getElementById('serviceModal');
    const modalBody      = document.getElementById('serviceModalBody');
    const modalTitle     = document.getElementById('serviceModalLabel');
    const deleteForm     = document.getElementById('serviceDeleteForm');
    const delConfirmEl   = document.getElementById('deleteConfirmModal');
    const delNameEl      = document.getElementById('deleteServiceName');
    const confirmDelBtn  = document.getElementById('confirmDeleteBtn');

    if (!modalEl || !modalBody) return;

    const modal       = bootstrap.Modal.getOrCreateInstance(modalEl);
    const delModal    = delConfirmEl ? bootstrap.Modal.getOrCreateInstance(delConfirmEl) : null;
    const baseUrl     = '/Admin/Complexes';

    // ── Wire "Add" buttons ───────────────────────────────────────────────────
    document.querySelectorAll('[data-service-modal="create"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const complexId   = btn.getAttribute('data-complex-id');
            const courtTypeId = btn.getAttribute('data-court-type-id');
            if (complexId) openFormModal('create', complexId, null, courtTypeId);
        });
    });

    // ── Wire "Edit" buttons ──────────────────────────────────────────────────
    document.querySelectorAll('[data-service-modal="edit"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const offeringId = btn.getAttribute('data-offering-id');
            const complexId  = btn.getAttribute('data-complex-id');
            if (offeringId && complexId) openFormModal('edit', complexId, offeringId);
        });
    });

    // ── Wire "Delete" buttons → show confirm modal ───────────────────────────
    let pendingDelete = null; // { offeringId, complexId, courtTypeId }

    document.querySelectorAll('[data-service-delete]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const offeringId  = btn.getAttribute('data-service-delete');
            const complexId   = btn.getAttribute('data-complex-id');
            const courtTypeId = btn.getAttribute('data-court-type-id');
            const name        = btn.getAttribute('data-service-name') || 'dịch vụ này';

            if (!offeringId || !complexId) return;

            pendingDelete = { offeringId, complexId, courtTypeId };
            if (delNameEl) delNameEl.textContent = name;
            if (delModal) delModal.show();
        });
    });

    // ── Confirm delete button ────────────────────────────────────────────────
    if (confirmDelBtn) {
        confirmDelBtn.addEventListener('click', async function () {
            if (!pendingDelete) return;
            const { offeringId, complexId, courtTypeId } = pendingDelete;
            pendingDelete = null;

            if (delModal) delModal.hide();

            confirmDelBtn.disabled = true;
            confirmDelBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang xóa...';

            await deleteService(offeringId, complexId, courtTypeId);

            confirmDelBtn.disabled = false;
            confirmDelBtn.innerHTML = '<i class="fa-solid fa-trash me-1"></i> Xóa';
        });
    }

    // ── Reset modal body on close ────────────────────────────────────────────
    modalEl.addEventListener('hidden.bs.modal', function () {
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
    });

    // ── Open Add / Edit modal ────────────────────────────────────────────────
    async function openFormModal(mode, complexId, offeringId, courtTypeId) {
        modalTitle.textContent = mode === 'edit' ? 'Sửa dịch vụ loại sân' : 'Thêm dịch vụ loại sân';
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
        modal.show();

        let url;
        if (mode === 'edit') {
            url = baseUrl + '/EditService?offeringId=' + offeringId + '&complexId=' + complexId;
        } else {
            url = baseUrl + '/AddService?complexId=' + complexId;
            if (courtTypeId) url += '&courtTypeId=' + courtTypeId;
        }

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (response.status === 401) throw new Error('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
            if (!response.ok) throw new Error('Không thể tải form. Vui lòng thử lại.');
            const html = await response.text();
            // Detect error JSON
            if (html.trim().startsWith('{')) {
                const json = JSON.parse(html);
                modal.hide();
                showToast(json.message || 'Có lỗi xảy ra.', 'error');
                return;
            }
            modalBody.innerHTML = html;
            bindFormSubmit();
            bindServiceFormUi();
        } catch (err) {
            modal.hide();
            showToast(err.message || 'Không thể tải form.', 'error');
        }
    }

    // ── Bind form UX (price/mode sync, catalog auto-fill) ────────────────────
    function bindServiceFormUi() {
        const modeSelect   = document.getElementById('serviceModeSelect');
        const priceInput   = document.getElementById('priceInput');
        const priceRow     = document.getElementById('priceRow');
        const catalogSelect = document.getElementById('catalogServiceSelect');

        if (!modeSelect || !priceInput) return;

        function syncMode() {
            const isIncluded = modeSelect.value === 'Included';
            priceInput.readOnly = isIncluded;
            if (isIncluded) priceInput.value = 0;
            if (priceRow) priceRow.style.opacity = isIncluded ? '0.55' : '1';
        }

        catalogSelect?.addEventListener('change', function () {
            const opt = this.selectedOptions[0];
            if (!opt || !opt.dataset.price) return;
            if (modeSelect.value !== 'Included')
                priceInput.value = opt.dataset.price;
        });

        modeSelect.addEventListener('change', syncMode);
        syncMode();
    }

    // ── Bind AJAX form submit ────────────────────────────────────────────────
    function bindFormSubmit() {
        const form = document.getElementById('serviceOfferingForm');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = document.getElementById('serviceFormSubmit');
            const isEdit    = !!form.querySelector('[name="OfferingId"]')?.value;

            if (submitBtn) {
                submitBtn.disabled = true;
                submitBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...';
            }

            clearFieldErrors(form);

            try {
                const formData = new FormData(form);
                const response = await fetch(form.action, {
                    method: 'POST',
                    headers: { 'X-Requested-With': 'XMLHttpRequest' },
                    body: formData
                });

                const result = await parseJsonResponse(response);

                if (result.success) {
                    modal.hide();
                    showToast(result.message || 'Lưu thành công!', 'success');
                    setTimeout(function () { window.location.reload(); }, 700);
                    return;
                }

                // Validation errors
                showToast(result.message || 'Có lỗi xảy ra.', 'error');
                if (result.errors) applyFieldErrors(form, result.errors);

            } catch (err) {
                showToast(err.message || 'Không thể lưu dữ liệu. Vui lòng thử lại.', 'error');
            } finally {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    submitBtn.innerHTML =
                        '<i class="fa-solid fa-floppy-disk"></i> ' +
                        (isEdit ? 'Lưu thay đổi' : 'Thêm mới');
                }
            }
        });
    }

    // ── AJAX Delete ──────────────────────────────────────────────────────────
    async function deleteService(offeringId, complexId, courtTypeId) {
        if (!deleteForm) return;

        const token = deleteForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token)      formData.append('__RequestVerificationToken', token);
        formData.append('offeringId', offeringId);
        formData.append('complexId',  complexId);
        if (courtTypeId) formData.append('courtTypeId', courtTypeId);

        try {
            const response = await fetch(baseUrl + '/DeleteService', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            const result = await parseJsonResponse(response);

            if (result.success) {
                showToast(result.message || 'Đã xóa dịch vụ.', 'success');
                // Remove the row from DOM without full reload for snappier UX
                const row = document.getElementById('offering-row-' + offeringId);
                if (row) {
                    row.style.transition = 'opacity 0.3s';
                    row.style.opacity = '0';
                    setTimeout(function () { window.location.reload(); }, 400);
                } else {
                    setTimeout(function () { window.location.reload(); }, 600);
                }
            } else {
                showToast(result.message || 'Không thể xóa dịch vụ.', 'error');
            }
        } catch (err) {
            showToast(err.message || 'Không thể xóa dịch vụ. Vui lòng thử lại.', 'error');
        }
    }

    // ── Parse response safely ────────────────────────────────────────────────
    async function parseJsonResponse(response) {
        const text = await response.text();
        if (!text) {
            if (response.status === 401)
                throw new Error('Phiên đăng nhập hết hạn. Vui lòng đăng xuất và đăng nhập lại.');
            throw new Error('Máy chủ trả về phản hồi rỗng (HTTP ' + response.status + ').');
        }
        try {
            return JSON.parse(text);
        } catch {
            throw new Error(
                response.ok
                    ? 'Phản hồi không hợp lệ từ máy chủ.'
                    : 'Lỗi máy chủ (HTTP ' + response.status + ').'
            );
        }
    }

    // ── Field-level validation helpers ───────────────────────────────────────
    function clearFieldErrors(form) {
        form.querySelectorAll('.field-error').forEach(function (el) { el.textContent = ''; });
        form.querySelectorAll('.is-invalid').forEach(function (el) { el.classList.remove('is-invalid'); });
    }

    function applyFieldErrors(form, errors) {
        Object.keys(errors).forEach(function (key) {
            const messages = errors[key];
            if (!messages || !messages.length) return;
            const input = form.querySelector('[name="' + key + '"]');
            if (input) input.classList.add('is-invalid');
            const fieldError = input?.closest('.mb-3, .col-md-6')?.querySelector('.field-error');
            if (fieldError) fieldError.textContent = messages[0];
        });
    }

    // ── Toast notification ───────────────────────────────────────────────────
    window.showToast = function (message, type) {
        const container = document.getElementById('toastContainer');
        if (!container) return;

        const colors = {
            success: { bg: 'rgba(16,185,129,.95)', icon: 'fa-circle-check' },
            error:   { bg: 'rgba(239,68,68,.95)',  icon: 'fa-circle-xmark' },
            info:    { bg: 'rgba(59,130,246,.95)',  icon: 'fa-circle-info'  },
            warning: { bg: 'rgba(245,158,11,.95)',  icon: 'fa-triangle-exclamation' }
        };
        const cfg = colors[type] || colors.info;

        const toast = document.createElement('div');
        toast.style.cssText = [
            'background:' + cfg.bg,
            'color:#fff',
            'padding:12px 18px',
            'border-radius:10px',
            'font-size:.9rem',
            'display:flex',
            'align-items:center',
            'gap:10px',
            'box-shadow:0 4px 20px rgba(0,0,0,.4)',
            'pointer-events:auto',
            'opacity:0',
            'transform:translateX(40px)',
            'transition:all .3s ease',
            'min-width:280px',
            'max-width:400px',
            'word-break:break-word'
        ].join(';');
        toast.innerHTML = '<i class="fa-solid ' + cfg.icon + '" style="flex-shrink:0;font-size:1rem"></i>' +
                          '<span>' + message + '</span>';
        container.appendChild(toast);

        // Animate in
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                toast.style.opacity = '1';
                toast.style.transform = 'translateX(0)';
            });
        });

        // Auto-dismiss
        setTimeout(function () {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(40px)';
            setTimeout(function () { toast.remove(); }, 350);
        }, 4000);
    };

})();

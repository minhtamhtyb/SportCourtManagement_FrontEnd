(function () {
    const modalEl = document.getElementById('serviceModal');
    const modalBody = document.getElementById('serviceModalBody');
    const modalTitle = document.getElementById('serviceModalLabel');
    const deleteForm = document.getElementById('serviceDeleteForm');

    if (!modalEl || !modalBody) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const baseUrl = '/Admin/Complexes';

    document.querySelectorAll('[data-service-modal="create"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const complexId = btn.getAttribute('data-complex-id');
            const courtTypeId = btn.getAttribute('data-court-type-id');
            if (complexId) openFormModal('create', complexId, null, courtTypeId);
        });
    });

    document.querySelectorAll('[data-service-modal="edit"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const offeringId = btn.getAttribute('data-offering-id');
            const complexId = btn.getAttribute('data-complex-id');
            if (offeringId && complexId) openFormModal('edit', complexId, offeringId);
        });
    });

    document.querySelectorAll('[data-service-delete]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const offeringId = btn.getAttribute('data-service-delete');
            const complexId = btn.getAttribute('data-complex-id');
            const courtTypeId = btn.getAttribute('data-court-type-id');
            const name = btn.getAttribute('data-service-name') || 'dịch vụ này';
            if (!offeringId || !complexId) return;
            if (!confirm('Xóa "' + name + '" khỏi loại sân?')) return;
            deleteService(offeringId, complexId, courtTypeId);
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
    });

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
            if (!response.ok) throw new Error('Không thể tải form. Vui lòng thử lại.');
            modalBody.innerHTML = await response.text();
            bindFormSubmit();
            bindServiceFormUi();
        } catch (err) {
            modal.hide();
            showToast(err.message || 'Không thể tải form.', 'error');
        }
    }

    function bindServiceFormUi() {
        const modeSelect = document.getElementById('serviceModeSelect');
        const priceInput = document.getElementById('priceInput');
        const priceRow = document.getElementById('priceRow');
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

    function bindFormSubmit() {
        const form = document.getElementById('serviceOfferingForm');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = document.getElementById('serviceFormSubmit');

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
                    showToast(result.message, 'success');
                    setTimeout(function () {
                        window.location.reload();
                    }, 600);
                    return;
                }

                showToast(result.message || 'Có lỗi xảy ra.', 'error');
                if (result.errors) applyFieldErrors(form, result.errors);
            } catch (err) {
                showToast(err.message || 'Không thể lưu dữ liệu. Vui lòng thử lại.', 'error');
            } finally {
                if (submitBtn) {
                    submitBtn.disabled = false;
                    const isEdit = form.querySelector('[name="OfferingId"]')?.value;
                    submitBtn.innerHTML =
                        '<i class="fa-solid fa-floppy-disk"></i> ' +
                        (isEdit ? 'Lưu thay đổi' : 'Thêm mới');
                }
            }
        });
    }

    async function deleteService(offeringId, complexId, courtTypeId) {
        if (!deleteForm) return;

        const token = deleteForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token) formData.append('__RequestVerificationToken', token);
        formData.append('offeringId', offeringId);
        formData.append('complexId', complexId);
        if (courtTypeId) formData.append('courtTypeId', courtTypeId);

        try {
            const response = await fetch(baseUrl + '/DeleteService', {
                method: 'POST',
                headers: { 'X-Requested-With': 'XMLHttpRequest' },
                body: formData
            });

            const result = await parseJsonResponse(response);

            if (result.success) {
                showToast(result.message, 'success');
                setTimeout(function () {
                    window.location.reload();
                }, 600);
            } else {
                showToast(result.message || 'Không thể xóa dịch vụ.', 'error');
            }
        } catch (err) {
            showToast(err.message || 'Không thể xóa dịch vụ. Vui lòng thử lại.', 'error');
        }
    }

    async function parseJsonResponse(response) {
        const text = await response.text();
        if (!text) {
            if (response.status === 401)
                throw new Error('Phiên đăng nhập hết hạn. Vui lòng đăng xuất và đăng nhập lại.');
            throw new Error('Máy chủ trả về phản hồi rỗng (HTTP ' + response.status + ').');
        }

        try {
            return JSON.parse(text);
        } catch (err) {
            throw new Error(
                response.ok
                    ? 'Phản hồi không hợp lệ từ máy chủ.'
                    : 'Lỗi máy chủ (HTTP ' + response.status + ').'
            );
        }
    }

    function clearFieldErrors(form) {
        form.querySelectorAll('.field-error').forEach(function (el) {
            el.textContent = '';
        });
        form.querySelectorAll('.is-invalid').forEach(function (el) {
            el.classList.remove('is-invalid');
        });
    }

    function applyFieldErrors(form, errors) {
        Object.keys(errors).forEach(function (key) {
            const messages = errors[key];
            if (!messages || !messages.length) return;

            const input = form.querySelector('[name="' + key + '"]');
            if (input) input.classList.add('is-invalid');

            const fieldError = input?.closest('.mb-3')?.querySelector('.field-error');
            if (fieldError) fieldError.textContent = messages[0];
        });
    }
})();

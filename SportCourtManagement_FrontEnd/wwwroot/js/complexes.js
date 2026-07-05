(function () {
    const modalEl = document.getElementById('complexModal');
    const modalBody = document.getElementById('complexModalBody');
    const modalTitle = document.getElementById('complexModalLabel');
    const deleteForm = document.getElementById('complexDeleteForm');

    if (!modalEl || !modalBody) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const baseUrl = '/Admin/Complexes';
    const maxImageSize = 5 * 1024 * 1024;

    document.querySelectorAll('[data-complex-modal="create"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openFormModal('create');
        });
    });

    document.querySelectorAll('[data-complex-modal="edit"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-complex-id');
            if (id) openFormModal('edit', id);
        });
    });

    document.querySelectorAll('[data-complex-delete]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-complex-delete');
            const name = btn.getAttribute('data-complex-name') || 'tổ hợp này';
            if (!id) return;
            if (!confirm('Bạn có chắc muốn xóa "' + name + '"?')) return;
            deleteComplex(id);
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
    });

    async function openFormModal(mode, id) {
        modalTitle.textContent = mode === 'edit' ? 'Sửa tổ hợp sân' : 'Thêm tổ hợp sân';
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
        modal.show();

        const url = mode === 'edit' ? baseUrl + '/Edit/' + id : baseUrl + '/Create';

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!response.ok) throw new Error('Không thể tải form. Vui lòng thử lại.');
            modalBody.innerHTML = await response.text();
            bindFormSubmit();
            bindImagePreview();
        } catch (err) {
            modal.hide();
            showToast(err.message || 'Không thể tải form.', 'error');
        }
    }

    function bindImagePreview() {
        const fileInput = document.getElementById('complexImageFile');
        const preview = document.getElementById('complexImagePreview');
        if (!fileInput || !preview) return;

        fileInput.addEventListener('change', function () {
            const file = fileInput.files && fileInput.files[0];
            if (!file) return;

            if (file.size > maxImageSize) {
                showToast('Ảnh không được vượt quá 5MB.', 'error');
                fileInput.value = '';
                return;
            }

            const reader = new FileReader();
            reader.onload = function (e) {
                preview.src = e.target.result;
                preview.classList.remove('d-none');
            };
            reader.readAsDataURL(file);
        });
    }

    function bindFormSubmit() {
        const form = document.getElementById('complexForm');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = document.getElementById('complexFormSubmit');
            const fileInput = document.getElementById('complexImageFile');

            if (fileInput && fileInput.files && fileInput.files[0] && fileInput.files[0].size > maxImageSize) {
                showToast('Ảnh không được vượt quá 5MB.', 'error');
                return;
            }

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
                    const isEdit = form.querySelector('[name="ComplexId"]');
                    submitBtn.innerHTML =
                        '<i class="fa-solid fa-floppy-disk"></i> ' +
                        (isEdit ? 'Lưu thay đổi' : 'Thêm mới');
                }
            }
        });
    }

    async function deleteComplex(id) {
        if (!deleteForm) return;

        const token = deleteForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token) formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch(baseUrl + '/Delete/' + id, {
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
                showToast(result.message || 'Không thể xóa tổ hợp sân.', 'error');
            }
        } catch (err) {
            showToast(err.message || 'Không thể xóa tổ hợp sân. Vui lòng thử lại.', 'error');
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
                    : 'Lỗi máy chủ (HTTP ' + response.status + '). Kiểm tra Backend đang chạy và đã đăng nhập.'
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

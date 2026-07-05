(function () {
    const modalEl = document.getElementById('courtModal');
    const modalBody = document.getElementById('courtModalBody');
    const modalTitle = document.getElementById('courtModalLabel');

    if (!modalEl || !modalBody) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const baseUrl = '/Admin/Courts';

    document.querySelectorAll('[data-court-modal="create"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const complexId = btn.getAttribute('data-complex-id');
            if (complexId) openFormModal('create', complexId);
        });
    });

    document.querySelectorAll('[data-court-modal="edit"]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-court-id');
            if (id) openFormModal('edit', id);
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
    });

    async function openFormModal(mode, idOrComplexId) {
        modalTitle.textContent = mode === 'edit' ? 'Sửa sân thể thao' : 'Thêm sân thể thao';
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
        modal.show();

        const url = mode === 'edit' ? baseUrl + '/Edit/' + idOrComplexId : baseUrl + '/Create?complexId=' + idOrComplexId;

        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!response.ok) throw new Error('Không thể tải form. Vui lòng thử lại.');
            modalBody.innerHTML = await response.text();
            bindFormSubmit();
        } catch (err) {
            modal.hide();
            showToast(err.message || 'Không thể tải form.', 'error');
        }
    }

    function bindFormSubmit() {
        const form = document.getElementById('courtForm');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = document.getElementById('courtFormSubmit');

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
                    const isEdit = form.querySelector('[name="CourtId"]');
                    submitBtn.innerHTML =
                        '<i class="fa-solid fa-floppy-disk"></i> ' +
                        (isEdit ? 'Lưu thay đổi' : 'Thêm mới');
                }
            }
        });
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

            const fieldError = input?.closest('.mb-3')?.querySelector('.field-error') || input?.nextElementSibling;
            if (fieldError && fieldError.classList.contains('text-danger')) {
                fieldError.textContent = messages[0];
            }
        });
    }
})();

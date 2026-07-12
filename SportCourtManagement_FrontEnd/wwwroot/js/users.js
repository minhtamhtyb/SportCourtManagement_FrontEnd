(function () {
    const modalEl = document.getElementById('userRolesModal');
    const modalBody = document.getElementById('userRolesModalBody');
    const modalTitle = document.getElementById('userRolesModalLabel');
    const toggleForm = document.getElementById('userToggleStatusForm');

    if (!modalEl || !modalBody) return;

    const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
    const baseUrl = '/Admin/Users';

    document.querySelectorAll('[data-user-create]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            openCreateModal();
        });
    });

    document.querySelectorAll('[data-user-edit]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-user-id');
            if (id) openEditModal(id);
        });
    });

    document.querySelectorAll('[data-user-delete]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-user-id');
            const name = btn.getAttribute('data-user-name') || 'người dùng này';
            if (!id) return;
            if (!confirm('Bạn có chắc chắn muốn xóa vĩnh viễn người dùng "' + name + '"? Hợp đồng đặt sân và giải đấu của họ có thể bị ảnh hưởng.')) return;
            deleteUser(id, name);
        });
    });

    document.querySelectorAll('[data-user-toggle-status]').forEach(function (btn) {
        btn.addEventListener('click', function () {
            const id = btn.getAttribute('data-user-id');
            const name = btn.getAttribute('data-user-name') || 'người dùng này';
            const isActive = btn.getAttribute('data-is-active') === 'true';
            const nextActive = !isActive;
            const action = nextActive ? 'kích hoạt' : 'vô hiệu hóa';
            if (!id) return;
            if (!confirm('Bạn có chắc muốn ' + action + ' "' + name + '"?')) return;
            toggleStatus(id, nextActive);
        });
    });

    modalEl.addEventListener('hidden.bs.modal', function () {
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
    });

    async function openCreateModal() {
        modalTitle.textContent = 'Thêm người dùng mới';
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
        modal.show();

        try {
            const response = await fetch(baseUrl + '/Create', {
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

    async function openEditModal(userId) {
        modalTitle.textContent = 'Cập nhật thông tin người dùng';
        modalBody.innerHTML =
            '<div class="text-center py-4 text-secondary">' +
            '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
            '<span class="ms-2">Đang tải...</span></div>';
        modal.show();

        try {
            const response = await fetch(baseUrl + '/Edit/' + userId, {
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

    async function deleteUser(userId, name) {
        const deleteForm = document.getElementById('userDeleteForm');
        if (!deleteForm) return;

        const token = deleteForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token) formData.append('__RequestVerificationToken', token);
        formData.append('id', userId);

        try {
            const response = await fetch(baseUrl + '/Delete/' + userId, {
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
                showToast(result.message || 'Không thể xóa người dùng.', 'error');
            }
        } catch (err) {
            showToast(err.message || 'Không thể xóa người dùng. Vui lòng thử lại.', 'error');
        }
    }

    function bindFormSubmit() {
        const form = document.getElementById('userRolesForm');
        if (!form) return;

        form.addEventListener('submit', async function (e) {
            e.preventDefault();
            const submitBtn = document.getElementById('userRolesFormSubmit');

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
                    submitBtn.innerHTML = '<i class="fa-solid fa-floppy-disk"></i> Lưu thay đổi';
                }
            }
        });
    }

    async function toggleStatus(userId, isActive) {
        if (!toggleForm) return;

        const token = toggleForm.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const formData = new FormData();
        if (token) formData.append('__RequestVerificationToken', token);
        formData.append('id', userId);
        formData.append('isActive', isActive.toString());

        try {
            const response = await fetch(baseUrl + '/ToggleStatus', {
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
                showToast(result.message || 'Không thể cập nhật trạng thái.', 'error');
            }
        } catch (err) {
            showToast(err.message || 'Không thể cập nhật trạng thái. Vui lòng thử lại.', 'error');
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

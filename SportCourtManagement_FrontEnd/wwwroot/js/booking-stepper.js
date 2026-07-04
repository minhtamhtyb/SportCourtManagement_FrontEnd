$(document).ready(function () {
    const slotPrice = parseInt($('#slot-price').val()) || 120000;
    const courtId = parseInt($('input[name="courtId"]').val());
    const slotId = parseInt($('input[name="slotId"]').val());
    const originalDate = $('#original-date').val();

    let racketPrice = 30000;
    let drinkPrice = 15000;
    let discountPercent = 0;
    let computedDates = [];
    let checkedDatesAvailability = []; // { date: 'YYYY-MM-DD', status: 'Available'|'Booked' }

    // === 1. Quantity Buttons ===
    window.changeQty = function (id, delta) {
        const input = document.getElementById(id);
        if (!input) return;
        let val = parseInt(input.value) + delta;
        if (val < 0) val = 0;
        if (id === 'racketQty' && val > 10) val = 10;
        if (id === 'drinkQty' && val > 20) val = 20;
        input.value = val;
        updateSummary();
    };

    // === 2. Promo Code Application ===
    window.applyPromo = function () {
        const promo = $('#promo-input').val().trim();
        const msg = $('#promo-msg');
        const row = $('#discount-row');
        
        if (promo.toUpperCase() === 'DISCOUNT10') {
            discountPercent = 0.10;
            msg.show().removeClass('text-danger').addClass('text-sc-green').text('Áp dụng mã ưu đãi DISCOUNT10 thành công (Giảm 10%)!');
            row.css('display', 'flex');
        } else if (promo === '') {
            discountPercent = 0;
            msg.hide();
            row.css('display', 'none');
        } else {
            discountPercent = 0;
            msg.show().removeClass('text-sc-green').addClass('text-danger').text('Mã ưu đãi không hợp lệ.');
            row.css('display', 'none');
        }
        updateSummary();
    };

    // === 3. Toggle Single / Recurring ===
    $('input[name="isRecurring"]').on('change', function () {
        const isRec = $('input[name="isRecurring"]:checked').val() === 'true';
        if (isRec) {
            $('#recurring-fields').slideDown(300);
            $('#single-date-info').slideUp(300);
            $('#services-section').slideUp(300); // Services are usually for single sessions
            resetQuantities();
        } else {
            $('#recurring-fields').slideUp(300);
            $('#single-date-info').slideDown(300);
            $('#services-section').slideDown(300);
        }
        updateSummary();
    });

    function resetQuantities() {
        $('#racketQty').val(0);
        $('#drinkQty').val(0);
    }

    // === 4. Quick Day Selection Presets ===
    $('#btn-246').on('click', function () {
        $('input.day-checkbox').prop('checked', false);
        $('#day-1, #day-3, #day-5').prop('checked', true); // T2, T4, T6
        calculateRecurringDates();
    });

    $('#btn-357').on('click', function () {
        $('input.day-checkbox').prop('checked', false);
        $('#day-2, #day-4, #day-6').prop('checked', true); // T3, T5, T7
        calculateRecurringDates();
    });

    // Recalculate dates whenever date range or days check changes
    $('#start-date, #end-date, input.day-checkbox').on('change', function () {
        calculateRecurringDates();
    });

    function calculateRecurringDates() {
        const startVal = $('#start-date').val();
        const endVal = $('#end-date').val();
        
        if (!startVal || !endVal) return;

        const start = new Date(startVal);
        const end = new Date(endVal);
        
        if (end <= start) {
            $('#recurring-dates-preview').html('<div class="text-danger small mt-2"><i class="bi bi-exclamation-triangle-fill"></i> Ngày kết thúc phải sau ngày bắt đầu.</div>');
            return;
        }

        const selectedDays = [];
        $('input.day-checkbox:checked').each(function () {
            selectedDays.push(parseInt($(this).val()));
        });

        if (selectedDays.length === 0) {
            $('#recurring-dates-preview').html('<div class="text-sc-muted small mt-2">Vui lòng chọn ít nhất một thứ trong tuần.</div>');
            return;
        }

        computedDates = [];
        let curr = new Date(start);
        while (curr <= end) {
            if (selectedDays.includes(curr.getDay())) {
                computedDates.push(new Date(curr));
            }
            curr.setDate(curr.getDate() + 1);
        }

        if (computedDates.length === 0) {
            $('#recurring-dates-preview').html('<div class="text-warning small mt-2"><i class="bi bi-exclamation-triangle-fill"></i> Không tìm thấy ngày phù hợp trong khoảng đã chọn.</div>');
            return;
        }

        // Render simple preview list
        let previewHtml = `<div class="text-sc-muted mb-2 small">Tìm thấy ${computedDates.length} buổi chơi:</div><div class="d-flex flex-wrap gap-2" style="max-height: 120px; overflow-y: auto;">`;
        computedDates.forEach(d => {
            const dateStr = d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' });
            previewHtml += `<span class="badge bg-secondary font-monospace" style="font-size: 11px;">${dateStr}</span>`;
        });
        previewHtml += '</div>';
        $('#recurring-dates-preview').html(previewHtml);

        updateSummary();
    }

    // === 5. Calculate pricing breakdown ===
    function updateSummary() {
        const isRec = $('input[name="isRecurring"]:checked').val() === 'true';
        let sessionsCount = 1;
        let servicesTotal = 0;

        if (isRec) {
            sessionsCount = computedDates.length || 0;
        } else {
            const racketQty = parseInt($('#racketQty').val()) || 0;
            const drinkQty = parseInt($('#drinkQty').val()) || 0;
            servicesTotal = (racketQty * racketPrice) + (drinkQty * drinkPrice);
        }

        const baseCourtPrice = slotPrice * sessionsCount;
        const subtotal = baseCourtPrice + servicesTotal;
        const discountTotal = subtotal * discountPercent;
        const grandTotal = subtotal - discountTotal;

        // Update sidebar labels
        $('#sessions-count-lbl').text(isRec ? ` (${sessionsCount} buổi)` : '');
        $('#court-total-lbl').text(baseCourtPrice.toLocaleString('vi-VN') + "đ");
        $('#services-total-lbl').text(servicesTotal.toLocaleString('vi-VN') + "đ");
        $('#discount-total-lbl').text("-" + discountTotal.toLocaleString('vi-VN') + "đ");
        $('#grand-total-lbl').text(grandTotal.toLocaleString('vi-VN') + "đ");

        // Keep labels inside form summary updated
        $('#summary-grand-total').text(grandTotal.toLocaleString('vi-VN') + "đ");
    }

    // === 6. Stepper Navigation ===
    $('#btn-next-step').on('click', function () {
        const isRec = $('input[name="isRecurring"]:checked').val() === 'true';
        
        if (isRec && computedDates.length === 0) {
            alert("Vui lòng thiết lập cấu hình đặt lịch định kỳ hợp lệ trước khi tiếp tục!");
            return;
        }

        // Show loading status in Step 3 preview
        $('#step3-dates-preview').html('<div class="py-3 text-center text-sc-muted"><span class="spinner-border spinner-border-sm me-2"></span> Đang kiểm tra trạng thái sân trống các ngày...</div>');

        // Transition views
        $('#step2-container').fadeOut(200, function () {
            $('#step3-container').fadeIn(200);
            
            // Update Stepper indicators
            $('#indicator-step2').removeClass('sc-indicator-active').addClass('sc-indicator-completed');
            $('#indicator-step2 i').removeClass('bi-2-circle-fill').addClass('bi-check-circle-fill text-success');
            $('#indicator-step3').addClass('sc-indicator-active');
        });

        // Populate summary reviews
        $('#confirm-type').text(isRec ? "Đặt định kỳ (Theo tuần)" : "Đặt đơn lẻ");
        $('#confirm-date-range').text(isRec 
            ? `${formatDateISO($('#start-date').val())} đến ${formatDateISO($('#end-date').val())}` 
            : formatDateISO(originalDate)
        );

        if (isRec) {
            let daysDisplay = [];
            $('input.day-checkbox:checked').each(function () {
                const label = $(this).parent().text().trim();
                daysDisplay.push(label);
            });
            $('#confirm-days-row').show();
            $('#confirm-days').text(daysDisplay.join(', '));
            
            // Check conflicts for each recurring date via AJAX
            checkRecurringAvailability();
        } else {
            $('#confirm-days-row').hide();
            // Single booking has 100% valid since they selected it
            const formattedDate = formatDateISO(originalDate);
            $('#step3-dates-preview').html(`
                <div class="p-3 rounded border mb-2 d-flex justify-content-between align-items-center" style="background: rgba(16, 185, 129, 0.05); border-color: rgba(16, 185, 129, 0.2);">
                    <span class="text-white fw-bold"><i class="bi bi-calendar-check me-2 text-success"></i> ${formattedDate}</span>
                    <span class="badge bg-success">Hợp lệ - Sẽ đặt</span>
                </div>
            `);
        }
    });

    $('#btn-prev-step').on('click', function () {
        $('#step3-container').fadeOut(200, function () {
            $('#step2-container').fadeIn(200);
            
            $('#indicator-step3').removeClass('sc-indicator-active');
            $('#indicator-step2').removeClass('sc-indicator-completed').addClass('sc-indicator-active');
            $('#indicator-step2 i').removeClass('bi-check-circle-fill text-success').addClass('bi-2-circle-fill');
        });
    });

    function formatDateISO(isoStr) {
        if (!isoStr) return "";
        const parts = isoStr.split('-');
        if (parts.length === 3) return `${parts[2]}/${parts[1]}/${parts[0]}`;
        return isoStr;
    }

    // === 7. AJAX Check Conflicts for Recurring Dates ===
    function checkRecurringAvailability() {
        checkedDatesAvailability = [];
        const promises = [];

        computedDates.forEach(date => {
            const dateStr = date.toISOString().split('T')[0];
            const promise = $.get('/Courts/CheckAvailabilityJson', { id: courtId, date: dateStr })
                .done(function (response) {
                    // Check if targeted slot is available or booked
                    const slot = response.slots ? response.slots.find(s => s.slotId === slotId || s.SlotId === slotId || s.timeSlotId === slotId) : null;
                    const status = slot ? slot.status : 'Booked';
                    checkedDatesAvailability.push({ date: date, dateStr: dateStr, status: status, slotName: slot ? (slot.startTime || slot.StartTime) : "" });
                })
                .fail(function () {
                    checkedDatesAvailability.push({ date: date, dateStr: dateStr, status: 'Booked' });
                });
            promises.push(promise);
        });

        // After all availability queries return
        $.when.apply($, promises).always(function () {
            renderRecurringConfirmationList();
        });
    }

    function renderRecurringConfirmationList() {
        let listHtml = '<div class="space-y-2" style="max-height: 250px; overflow-y: auto; padding-right: 5px;">';
        let validCount = 0;
        let conflictCount = 0;

        // Sort by date ascending
        checkedDatesAvailability.sort((a, b) => a.date - b.date);

        checkedDatesAvailability.forEach(item => {
            const formatted = formatDateISO(item.dateStr);
            const isValid = item.status.toLowerCase() === 'available';
            
            if (isValid) {
                validCount++;
                listHtml += `
                    <div class="p-2.5 rounded border mb-2 d-flex justify-content-between align-items-center" style="background: rgba(16, 185, 129, 0.04); border-color: rgba(16, 185, 129, 0.15); font-size: 13px;">
                        <span class="text-white fw-semibold"><i class="bi bi-calendar-check text-success me-2"></i> Thứ ${item.date.getDay() === 0 ? "CN" : item.date.getDay() + 1} (${formatted})</span>
                        <span class="badge bg-success-subtle text-success border border-success border-opacity-25" style="background: rgba(16, 185, 129, 0.15); font-size: 11px;">Hợp lệ - Sẽ đặt</span>
                    </div>
                `;
            } else {
                conflictCount++;
                listHtml += `
                    <div class="p-2.5 rounded border mb-2 d-flex justify-content-between align-items-center" style="background: rgba(239, 68, 68, 0.04); border-color: rgba(239, 68, 68, 0.15); font-size: 13px;">
                        <span class="text-white-50"><i class="bi bi-calendar-x text-danger me-2"></i> Thứ ${item.date.getDay() === 0 ? "CN" : item.date.getDay() + 1} (${formatted})</span>
                        <span class="badge bg-danger-subtle text-danger border border-danger border-opacity-25" style="background: rgba(239, 68, 68, 0.15); font-size: 11px;">Trùng lịch - Bỏ qua</span>
                    </div>
                `;
            }
        });
        listHtml += '</div>';

        if (conflictCount > 0) {
            listHtml = `
                <div class="alert alert-warning py-2 px-3 border border-warning border-opacity-25 rounded-3 mb-3 d-flex align-items-center gap-2" style="background: rgba(245, 158, 11, 0.05); font-size: 12.5px; color: var(--sc-amber);">
                    <i class="bi bi-exclamation-triangle-fill fs-5"></i>
                    <div>
                        Phát hiện <strong>${conflictCount} ngày bị trùng lịch</strong>. Theo quy tắc hệ thống, các ngày bị trùng sẽ được bỏ qua, hệ thống chỉ đặt và tính tiền các ngày hợp lệ còn lại.
                    </div>
                </div>
            ` + listHtml;
        }

        $('#step3-dates-preview').html(listHtml);

        // Update totals based on actual valid bookings
        const actualBookedCount = validCount;
        const totalEstimated = actualBookedCount * slotPrice;
        const discountTotal = totalEstimated * discountPercent;
        const grandTotal = totalEstimated - discountTotal;

        // Recalculate sidebar for recurring booking taking conflicts into account
        $('#sessions-count-lbl').text(` (${actualBookedCount} buổi được đặt)`);
        $('#court-total-lbl').text(totalEstimated.toLocaleString('vi-VN') + "đ");
        $('#discount-total-lbl').text("-" + discountTotal.toLocaleString('vi-VN') + "đ");
        $('#grand-total-lbl').text(grandTotal.toLocaleString('vi-VN') + "đ");
        $('#summary-grand-total').text(grandTotal.toLocaleString('vi-VN') + "đ");

        // Set conflict hidden fields to be sent during post if needed, or simply let the backend skip it natively.
    }
});

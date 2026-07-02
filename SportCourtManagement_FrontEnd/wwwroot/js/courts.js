$(document).ready(function () {
    // === 1. Tab Switching (Chuyển đổi Tab thông tin) ===
    $('.sc-tab-btn').on('click', function () {
        var targetTab = $(this).data('tab');

        // Gỡ bỏ active ở các tab cũ và ẩn nội dung cũ
        $('.sc-tab-btn').removeClass('active');
        $('.sc-tab-content').hide();

        // Kích hoạt tab mới và hiển thị nội dung
        $(this).addClass('active');
        $('#tab-' + targetTab).show();
    });

    // === 2. Date Picker & AJAX Availability (Chọn ngày và tải ô slot giờ trống) ===
    $('#booking-date').on('change', function () {
        var date = $(this).val();
        var courtId = $('#court-id').val();
        var $slotsContainer = $('#slots-container');

        if (date && courtId) {
            // Hiển thị trạng thái loading
            $slotsContainer.html('<div class="text-center py-4 text-sc-muted"> Đang tải lịch trống...</div>');

            // Gọi AJAX lấy danh sách slot
            $.get('/Courts/GetAvailability', { id: courtId, date: date })
                .done(function (htmlResult) {
                    $slotsContainer.html(htmlResult);
                    bindSlotSelection(); // Gán lại sự kiện click cho các ô giờ mới tải
                })
                .fail(function () {
                    $slotsContainer.html('<div class="text-center py-4 text-sc-red"> Lỗi tải lịch trống. Vui lòng thử lại.</div>');
                });
        }
    });

    // === 3. Slot Selection (Chọn ô giờ đặt sân) ===
    function bindSlotSelection() {
        var $selectableSlots = $('.sc-slot-btn').not('.sc-slot-booked, .sc-slot-maintenance');

        $selectableSlots.on('click', function () {
            // Gỡ bỏ chọn ở các ô khác
            $selectableSlots.removeClass('sc-slot-selected');

            // Chọn ô hiện tại
            $(this).addClass('sc-slot-selected');

            // Lấy thông tin từ data attributes
            var slotId = $(this).data('slot-id');
            var slotName = $(this).data('slot-name');
            var price = $(this).data('price');

            // Cập nhật lên giao diện và thẻ ẩn input
            $('#selected-slot-id').val(slotId);
            $('#selected-slot-label').text(slotName);
            
            if (price) {
                $('#booking-price').text(parseFloat(price).toLocaleString('vi-VN') + "đ");
            }
        });
    }
    bindSlotSelection(); // Gọi lần đầu khi vừa load trang xong

    // === 4. Interactive Review Star Rating (Tương tác chọn số sao đánh giá) ===
    var $ratingStars = $('.sc-star-interactive');
    var $ratingInput = $('#review-rating');

    if ($ratingStars.length > 0 && $ratingInput.length > 0) {
        $ratingStars.on('mouseover', function () {
            var val = parseInt($(this).data('value'));
            highlightStars(val);
        });

        $ratingStars.on('mouseout', function () {
            var currentVal = parseInt($ratingInput.val()) || 0;
            highlightStars(currentVal);
        });

        $ratingStars.on('click', function () {
            var val = parseInt($(this).data('value'));
            $ratingInput.val(val);
            highlightStars(val);
        });

        function highlightStars(count) {
            $ratingStars.each(function () {
                var val = parseInt($(this).data('value'));
                if (val <= count) {
                    $(this).addClass('sc-star-filled').text('★');
                } else {
                    $(this).removeClass('sc-star-filled').text('☆');
                }
            });
        }
    }

    // === 5. AJAX Review Pagination (Phân trang đánh giá bằng AJAX) ===
    window.loadReviewsPage = function (courtId, pageNumber) {
        var $reviewsContainer = $('#reviews-list-container');
        if ($reviewsContainer.length === 0) return;

        $reviewsContainer.html('<div class="text-center py-4 text-sc-muted">Đang tải đánh giá...</div>');

        $.get('/Courts/GetReviews', { courtId: courtId, pageNumber: pageNumber })
            .done(function (htmlResult) {
                $reviewsContainer.html(htmlResult);
            })
            .fail(function () {
                $reviewsContainer.html('<div class="text-center py-4 text-sc-red">Lỗi tải đánh giá.</div>');
            });
    };

    // === 6. Book Now Button Click Handler (Xử lý khi click Đặt sân ngay) ===
    $('#btn-book-now').on('click', function () {
        var isLoggedIn = $(this).data('logged-in') === true;
        if (!isLoggedIn) {
            alert("Bạn cần đăng nhập trước khi thực hiện đặt sân!");
            return;
        }

        var slotId = $('#selected-slot-id').val();
        var date = $('#booking-date').val();
        var courtId = $('#court-id').val();

        if (!slotId) {
            alert("Vui lòng chọn một khung giờ còn trống trước khi đặt sân!");
            return;
        }

        // Navigate to booking creation page
        window.location.href = '/Bookings/Create?courtId=' + courtId + '&date=' + date + '&slotId=' + slotId;
    });
});

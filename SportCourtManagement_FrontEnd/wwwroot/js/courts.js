// ======================================================
// courts.js - Court Detail Page (User Booking)
// ======================================================
$(document).ready(function () {
  // === 1. Tab Switching (Chuyển đổi Tab thông tin) ===
  $(".sc-tab-btn").on("click", function () {
    var targetTab = $(this).data("tab");

    // Gỡ bỏ active ở các tab cũ và ẩn nội dung cũ
    $(".sc-tab-btn").removeClass("active");
    $(".sc-tab-content").hide();

    // Kích hoạt tab mới và hiển thị nội dung
    $(this).addClass("active");
    $("#tab-" + targetTab).show();
  });

  // === 2. Date Picker & AJAX Availability (Chọn ngày và tải ô slot giờ trống) ===
  $("#booking-date").on("change", function () {
    var date = $(this).val();
    var courtId = $("#court-id").val();
    var $slotsContainer = $("#slots-container");

    if (date && courtId) {
      // Hiển thị trạng thái loading
      $slotsContainer.html(
        '<div class="text-center py-4 text-sc-muted"> Đang tải lịch trống...</div>',
      );

      // Gọi AJAX lấy danh sách slot
      $.get("/Courts/GetAvailability", { id: courtId, date: date })
        .done(function (htmlResult) {
          $slotsContainer.html(htmlResult);
          bindSlotSelection(); // Gán lại sự kiện click cho các ô giờ mới tải
        })
        .fail(function () {
          $slotsContainer.html(
            '<div class="text-center py-4 text-sc-red"> Lỗi tải lịch trống. Vui lòng thử lại.</div>',
          );
        });
    }
  });

  // === 3. Slot Selection (Chọn ô giờ đặt sân) ===
  function bindSlotSelection() {
    // Disable slot selection on detail page
  }
  bindSlotSelection(); // Gọi lần đầu khi vừa load trang xong

  // === 4. Interactive Review Star Rating (Tương tác chọn số sao đánh giá) ===
  var $ratingStars = $(".sc-star-interactive");
  var $ratingInput = $("#review-rating");

  if ($ratingStars.length > 0 && $ratingInput.length > 0) {
    $ratingStars.on("mouseover", function () {
      var val = parseInt($(this).data("value"));
      highlightStars(val);
    });

    $ratingStars.on("mouseout", function () {
      var currentVal = parseInt($ratingInput.val()) || 0;
      highlightStars(currentVal);
    });

    $ratingStars.on("click", function () {
      var val = parseInt($(this).data("value"));
      $ratingInput.val(val);
      highlightStars(val);
    });

    function highlightStars(count) {
      $ratingStars.each(function () {
        var val = parseInt($(this).data("value"));
        if (val <= count) {
          $(this).addClass("sc-star-filled").text("★");
        } else {
          $(this).removeClass("sc-star-filled").text("☆");
        }
      });
    }
  }

  // === 5. AJAX Review Pagination (Phân trang đánh giá bằng AJAX) ===
  window.loadReviewsPage = function (courtId, pageNumber) {
    var $reviewsContainer = $("#reviews-list-container");
    if ($reviewsContainer.length === 0) return;

    $reviewsContainer.html(
      '<div class="text-center py-4 text-sc-muted">Đang tải đánh giá...</div>',
    );

    $.get("/Courts/GetReviews", { courtId: courtId, pageNumber: pageNumber })
      .done(function (htmlResult) {
        $reviewsContainer.html(htmlResult);
      })
      .fail(function () {
        $reviewsContainer.html(
          '<div class="text-center py-4 text-sc-red">Lỗi tải đánh giá.</div>',
        );
      });
  };

  // === 6. Book Now Button Click Handler (Xử lý khi click Đặt sân ngay) ===
  $("#btn-book-now").on("click", function () {
    var date = $("#booking-date").val();
    var courtId = $("#court-id").val();

    if (!date) {
      alert("Vui lòng chọn ngày chơi!");
      return;
    }

    var $slotsContainer = $("#slots-container");
    if ($slotsContainer.text().indexOf("Đang tải") !== -1) {
      alert("Vui lòng đợi tải lịch trống!");
      return;
    }

    if ($slotsContainer.text().indexOf("Lỗi tải lịch trống") !== -1) {
      alert("Không thể tải lịch trống. Vui lòng làm mới trang hoặc thử lại!");
      return;
    }

    var hasAvailableSlot = $slotsContainer.find(".sc-slot-available").length > 0;
    if (!hasAvailableSlot) {
      alert("Không có khung giờ trống nào trong ngày đã chọn để đặt sân!");
      return;
    }

    // Navigate to booking creation page (singular Booking flow)
    window.location.href =
      "/Booking?courtId=" + courtId + "&date=" + date;
  });
});

// ======================================================
// Admin Court Modal (Create / Edit - Admin Panel)
// ======================================================
(function () {
  const modalEl = document.getElementById("courtModal");
  const modalBody = document.getElementById("courtModalBody");
  const modalTitle = document.getElementById("courtModalLabel");

  if (!modalEl || !modalBody) return;

  const modal = bootstrap.Modal.getOrCreateInstance(modalEl);
  const baseUrl = "/Admin/Courts";

  document
    .querySelectorAll('[data-court-modal="create"]')
    .forEach(function (btn) {
      btn.addEventListener("click", function () {
        const complexId = btn.getAttribute("data-complex-id");
        if (complexId) openFormModal("create", complexId);
      });
    });

  document
    .querySelectorAll('[data-court-modal="edit"]')
    .forEach(function (btn) {
      btn.addEventListener("click", function () {
        const id = btn.getAttribute("data-court-id");
        if (id) openFormModal("edit", id);
      });
    });

  modalEl.addEventListener("hidden.bs.modal", function () {
    modalBody.innerHTML =
      '<div class="text-center py-4 text-secondary">' +
      '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
      '<span class="ms-2">Đang tải...</span></div>';
  });

  async function openFormModal(mode, idOrComplexId) {
    modalTitle.textContent =
      mode === "edit" ? "Sửa sân thể thao" : "Thêm sân thể thao";
    modalBody.innerHTML =
      '<div class="text-center py-4 text-secondary">' +
      '<div class="spinner-border spinner-border-sm text-success" role="status"></div>' +
      '<span class="ms-2">Đang tải...</span></div>';
    modal.show();

    const url =
      mode === "edit"
        ? baseUrl + "/Edit/" + idOrComplexId
        : baseUrl + "/Create?complexId=" + idOrComplexId;

    try {
      const response = await fetch(url, {
        headers: { "X-Requested-With": "XMLHttpRequest" },
      });
      if (!response.ok)
        throw new Error("Không thể tải form. Vui lòng thử lại.");
      modalBody.innerHTML = await response.text();
      bindFormSubmit();
      bindImagePreview();
    } catch (err) {
      modal.hide();
      showToast(err.message || "Không thể tải form.", "error");
    }
  }

  function bindImagePreview() {
    const fileInput = document.getElementById("courtImageFile");
    const galleryPreview = document.getElementById("courtImageGalleryPreview");
    if (!galleryPreview) return;

    // Handle selecting primary image by clicking thumbnail
    $(galleryPreview).on("click", ".img-thumb-item", function (e) {
      if ($(e.target).hasClass("btn-remove-existing-img")) return;

      galleryPreview.querySelectorAll(".img-thumb-item").forEach(item => {
        item.classList.remove("is-primary-thumb");
        const img = item.querySelector("img");
        if (img) {
          img.classList.remove("border-success", "border-2");
          img.classList.add("border-secondary");
        }
        const badge = item.querySelector(".primary-badge");
        if (badge) badge.classList.add("d-none");
      });

      this.classList.add("is-primary-thumb");
      const clickedImg = this.querySelector("img");
      if (clickedImg) {
        clickedImg.classList.remove("border-secondary");
        clickedImg.classList.add("border", "border-2", "border-success");
      }
      const clickedBadge = this.querySelector(".primary-badge");
      if (clickedBadge) clickedBadge.classList.remove("d-none");

      const url = $(this).data("url");
      const courtImageUrlInput = document.getElementById("courtImageUrl");
      if (courtImageUrlInput && url) {
        courtImageUrlInput.value = url;
      }
    });

    // Handle removing existing image thumbnail
    $(galleryPreview).on("click", ".btn-remove-existing-img", function (e) {
      e.stopPropagation();
      const urlToRemove = $(this).data("url");
      const thumbItem = $(this).closest(".img-thumb-item");
      const wasPrimary = thumbItem.hasClass("is-primary-thumb");
      thumbItem.remove();
      // Remove matching hidden input
      $(`.existing-image-url-input[value="${urlToRemove}"]`).remove();

      // If removed image was primary, set first remaining image as primary
      if (wasPrimary) {
        const firstRemaining = galleryPreview.querySelector(".img-thumb-item");
        if (firstRemaining) {
          firstRemaining.click();
        } else {
          const courtImageUrlInput = document.getElementById("courtImageUrl");
          if (courtImageUrlInput) courtImageUrlInput.value = "";
        }
      }
    });

    if (fileInput) {
      fileInput.addEventListener("change", function () {
        const files = Array.from(fileInput.files || []);
        if (files.length === 0) return;

        const existingCount = galleryPreview.querySelectorAll(".existing-img-thumb").length;
        if (existingCount + files.length > 3) {
          showToast(`Mỗi sân chỉ được chọn tối đa 3 ảnh mô tả. Đã có ${existingCount} ảnh, bạn chọn thêm ${files.length} ảnh.`, "error");
          fileInput.value = "";
          return;
        }

        // Clear previous new image preview thumbnails
        galleryPreview.querySelectorAll(".new-img-thumb").forEach(el => el.remove());

        for (let file of files) {
          if (file.size > 5 * 1024 * 1024) {
            showToast(`Ảnh ${file.name} vượt quá 5MB.`, "error");
            fileInput.value = "";
            return;
          }
        }

        const hasExisting = galleryPreview.querySelectorAll(".img-thumb-item").length > 0;

        files.forEach((file, index) => {
          const reader = new FileReader();
          reader.onload = function (e) {
            const isFirst = !hasExisting && index === 0;
            const thumb = document.createElement("div");
            thumb.className = `position-relative img-thumb-item new-img-thumb ${isFirst ? "is-primary-thumb" : ""}`;
            thumb.style.cssText = "width: 110px; height: 80px; cursor: pointer;";
            thumb.setAttribute("title", "Bấm để chọn làm ảnh chính");
            thumb.innerHTML = `
              <img src="${e.target.result}" class="rounded w-100 h-100 object-fit-cover ${isFirst ? "border border-2 border-success" : "border border-secondary"}" />
              <span class="badge bg-success position-absolute bottom-0 start-0 m-1 primary-badge ${isFirst ? "" : "d-none"}" style="font-size: 10px;">Ảnh chính</span>
            `;
            galleryPreview.appendChild(thumb);
          };
          reader.readAsDataURL(file);
        });
      });
    }
  }

  function bindFormSubmit() {
    const form = document.getElementById("courtForm");
    if (!form) return;

    form.addEventListener("submit", async function (e) {
      e.preventDefault();
      const submitBtn = document.getElementById("courtFormSubmit");
      const fileInput = document.getElementById("courtImageFile");
      const galleryPreview = document.getElementById("courtImageGalleryPreview");

      const existingCount = galleryPreview ? galleryPreview.querySelectorAll(".existing-img-thumb").length : 0;
      const newFilesCount = fileInput && fileInput.files ? fileInput.files.length : 0;
      if (existingCount + newFilesCount > 3) {
        showToast("Mỗi sân chỉ được có tối đa 3 ảnh mô tả.", "error");
        return;
      }

      if (submitBtn) {
        submitBtn.disabled = true;
        submitBtn.innerHTML =
          '<span class="spinner-border spinner-border-sm me-1"></span> Đang lưu...';
      }

      clearFieldErrors(form);

      try {
        const formData = new FormData(form);
        const response = await fetch(form.action, {
          method: "POST",
          headers: { "X-Requested-With": "XMLHttpRequest" },
          body: formData,
        });

        const result = await parseJsonResponse(response);

        if (result.success) {
          modal.hide();
          showToast(result.message, "success");
          setTimeout(function () {
            window.location.reload();
          }, 600);
          return;
        }

        showToast(result.message || "Có lỗi xảy ra.", "error");
        if (result.errors) applyFieldErrors(form, result.errors);
      } catch (err) {
        showToast(
          err.message || "Không thể lưu dữ liệu. Vui lòng thử lại.",
          "error",
        );
      } finally {
        if (submitBtn) {
          submitBtn.disabled = false;
          const isEdit = form.querySelector('[name="CourtId"]');
          submitBtn.innerHTML =
            '<i class="fa-solid fa-floppy-disk"></i> ' +
            (isEdit ? "Lưu thay đổi" : "Thêm mới");
        }
      }
    });
  }

  async function parseJsonResponse(response) {
    const text = await response.text();
    if (!text) {
      if (response.status === 401)
        throw new Error(
          "Phiên đăng nhập hết hạn. Vui lòng đăng xuất và đăng nhập lại.",
        );
      throw new Error(
        "Máy chủ trả về phản hồi rỗng (HTTP " + response.status + ").",
      );
    }

    try {
      return JSON.parse(text);
    } catch (err) {
      throw new Error(
        response.ok
          ? "Phản hồi không hợp lệ từ máy chủ."
          : "Lỗi máy chủ (HTTP " +
              response.status +
              "). Kiểm tra Backend đang chạy và đã đăng nhập.",
      );
    }
  }

  function clearFieldErrors(form) {
    form.querySelectorAll(".field-error").forEach(function (el) {
      el.textContent = "";
    });
    form.querySelectorAll(".is-invalid").forEach(function (el) {
      el.classList.remove("is-invalid");
    });
  }

  function applyFieldErrors(form, errors) {
    Object.keys(errors).forEach(function (key) {
      const messages = errors[key];
      if (!messages || !messages.length) return;

      const input = form.querySelector('[name="' + key + '"]');
      if (input) input.classList.add("is-invalid");

      const fieldError =
        input?.closest(".mb-3")?.querySelector(".field-error") ||
        input?.nextElementSibling;
      if (fieldError && fieldError.classList.contains("text-danger")) {
        fieldError.textContent = messages[0];
      }
    });
  }
})();

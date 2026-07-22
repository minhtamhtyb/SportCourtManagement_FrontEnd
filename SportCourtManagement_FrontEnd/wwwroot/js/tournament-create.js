(function () {
    "use strict";

    const config = Object.assign({
        apiBaseUrl: "http://localhost:5000",
        availabilityUrl: "/Booking/GetSlotAvailability",
        createUrl: "/Tournaments/CreateJson",
        pollIntervalMs: 15000
    }, window.TOURNAMENT_CREATE_CONFIG || {});

    const state = {
        schedules: [],
        availability: new Map(),
        activeFilter: "all",
        refreshing: false,
        submitting: false,
        connection: null,
        toastTimer: null
    };

    const statusMeta = {
        Available: { label: "Còn trống", className: "status-available" },
        Held: { label: "Đang được giữ", className: "status-held" },
        Booked: { label: "Đã đặt", className: "status-booked" },
        Maintenance: { label: "Đang bảo trì", className: "status-maintenance" },
        Inactive: { label: "Tạm đóng", className: "status-inactive" },
        Loading: { label: "Đang kiểm tra", className: "status-loading" },
        Error: { label: "Không tải được", className: "status-inactive" },
        Scheduled: { label: "Đã thêm vào lịch", className: "status-scheduled" }
    };

    let elements = {};
    document.addEventListener("DOMContentLoaded", initialize);

    function initialize() {
        elements = {
            form: document.getElementById("tournamentForm"),
            date: document.getElementById("builderDate"),
            name: document.getElementById("inputTournamentName"),
            addButton: document.getElementById("btnAddSchedule"),
            submitButton: document.getElementById("btnSubmitTournament"),
            refreshButton: document.getElementById("refreshAvailability"),
            selectionCount: document.getElementById("selectionCount"),
            availableCounter: document.getElementById("availableCounter"),
            lastSynced: document.getElementById("lastSyncedText"),
            summaryName: document.getElementById("summaryTourName"),
            summaryList: document.getElementById("summaryCourtsList"),
            summaryCount: document.getElementById("summaryCountBadge"),
            summaryTotal: document.getElementById("summaryTotalCost"),
            conflictBanner: document.getElementById("conflictAlertBanner"),
            conflictMessage: document.getElementById("conflictAlertMessage"),
            realtime: document.getElementById("realtimeStatus"),
            realtimeText: document.getElementById("realtimeStatusText"),
            toast: document.getElementById("tournamentToast")
        };

        bindEvents();
        updateSummary();
        refreshAvailability();
        initializeRealtime();
        window.setInterval(function () {
            if (!document.hidden) refreshAvailability({ silent: true });
        }, Math.max(10000, Number(config.pollIntervalMs) || 15000));
    }

    function bindEvents() {
        elements.date?.addEventListener("change", onDateChanged);
        elements.name?.addEventListener("input", updateSummary);
        elements.addButton?.addEventListener("click", addSelectedSchedules);
        elements.refreshButton?.addEventListener("click", function () { refreshAvailability(); });
        elements.form?.addEventListener("submit", submitTournament);
        document.getElementById("dismissConflict")?.addEventListener("click", hideConflict);

        document.querySelectorAll(".tc-filter-btn").forEach(function (button) {
            button.addEventListener("click", function () {
                document.querySelectorAll(".tc-filter-btn").forEach(function (item) { item.classList.remove("is-active"); });
                button.classList.add("is-active");
                state.activeFilter = button.dataset.filter || "all";
                applyCourtFilter();
            });
        });
        document.querySelectorAll(".slot-checkbox").forEach(function (input) {
            input.addEventListener("change", updateSelectionState);
        });
        document.querySelectorAll("[data-action='select-all']").forEach(function (button) {
            button.addEventListener("click", function () { toggleAllAvailableSlots(button.dataset.courtId); });
        });
    }

    function onDateChanged() {
        clearCurrentSelection();
        hideConflict();
        markAllSlotsLoading();
        refreshAvailability();
    }

    function applyCourtFilter() {
        document.querySelectorAll(".court-item").forEach(function (card) {
            const visible = state.activeFilter === "all" || card.dataset.courtType === state.activeFilter;
            card.hidden = !visible;
        });
        updateAvailabilityCounter();
    }

    function toggleAllAvailableSlots(courtId) {
        const inputs = Array.from(document.querySelectorAll(`.slot-checkbox[data-court-id="${cssEscape(courtId)}"]`))
            .filter(function (input) { return !input.disabled; });
        const shouldSelect = inputs.some(function (input) { return !input.checked; });
        inputs.forEach(function (input) { input.checked = shouldSelect; });
        updateSelectionState();
    }

    async function refreshAvailability(options) {
        options = options || {};
        if (state.refreshing || !elements.date?.value) return;
        state.refreshing = true;
        const requestedDate = elements.date.value;
        if (!options.silent) {
            elements.refreshButton?.classList.add("disabled");
            if (elements.lastSynced) elements.lastSynced.textContent = "Đang đồng bộ trạng thái...";
        }

        const cards = Array.from(document.querySelectorAll(".court-item"));
        const queue = cards.slice();
        const workerCount = Math.min(6, queue.length);
        let failures = 0;

        async function worker() {
            while (queue.length > 0) {
                const card = queue.shift();
                if (!card) return;
                try {
                    const slots = await fetchAvailability(Number(card.dataset.courtId), requestedDate);
                    if (elements.date.value === requestedDate) renderCourtAvailability(card, slots, requestedDate);
                } catch (error) {
                    failures += 1;
                    renderCourtError(card);
                    console.warn(`Không tải được lịch sân ${card.dataset.courtId}:`, error);
                }
            }
        }

        await Promise.all(Array.from({ length: workerCount }, worker));
        state.refreshing = false;
        elements.refreshButton?.classList.remove("disabled");
        if (elements.date?.value !== requestedDate) {
            refreshAvailability();
            return;
        }
        updateAvailabilityCounter();
        updateSelectionState();
        if (elements.lastSynced) {
            elements.lastSynced.textContent = failures === cards.length && cards.length > 0
                ? "Không thể đồng bộ, sẽ tự thử lại"
                : `Cập nhật lúc ${new Date().toLocaleTimeString("vi-VN")}`;
        }
    }

    async function fetchAvailability(courtId, date) {
        const url = new URL(config.availabilityUrl, window.location.origin);
        url.searchParams.set("courtId", courtId);
        url.searchParams.set("date", date);
        const response = await fetch(url.toString(), { headers: { Accept: "application/json" }, cache: "no-store" });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const payload = await response.json();
        if (!payload.success || !Array.isArray(payload.slots)) throw new Error(payload.message || "Dữ liệu lịch không hợp lệ");
        return payload.slots;
    }

    function renderCourtAvailability(card, slots, date) {
        const slotsById = new Map(slots.map(function (slot) { return [Number(slot.slotId), slot]; }));
        card.querySelectorAll(".slot-checkbox").forEach(function (input) {
            const slot = slotsById.get(Number(input.dataset.slotId));
            applySlotStatus(input, slot ? normalizeStatus(slot.status) : "Inactive", Number(slot?.price) || 0, date);
        });
        updateCourtSummary(card);
    }

    function renderCourtError(card) {
        card.querySelectorAll(".slot-checkbox").forEach(function (input) {
            const key = availabilityKey(input.dataset.courtId, elements.date.value, input.dataset.slotId);
            if (!state.availability.has(key)) applySlotStatus(input, "Error", 0, elements.date.value);
        });
        const copy = card.querySelector(".court-availability-copy");
        if (copy) copy.textContent = " · Chưa tải được lịch";
    }

    function applySlotStatus(input, rawStatus, price, date) {
        const courtId = Number(input.dataset.courtId);
        const slotId = Number(input.dataset.slotId);
        const status = normalizeStatus(rawStatus);
        state.availability.set(availabilityKey(courtId, date, slotId), { status: status, price: price });
        input.dataset.price = String(price || 0);

        const scheduled = date === elements.date?.value && hasScheduledSlot(courtId, date, slotId);
        const displayStatus = scheduled && status === "Available" ? "Scheduled" : status;
        const meta = statusMeta[displayStatus] || statusMeta.Inactive;
        const label = input.nextElementSibling;
        if (!label) return;

        label.className = `tc-slot-label ${meta.className}`;
        label.dataset.status = status;
        label.title = meta.label;
        const stateText = label.querySelector(".tc-slot-state");
        const priceText = label.querySelector(".tc-slot-price");
        if (stateText) stateText.textContent = meta.label;
        if (priceText) priceText.textContent = price > 0 && status === "Available" ? formatMoney(price) : statusPriceLabel(status);

        const selectable = status === "Available" && !scheduled;
        if (!selectable && input.checked) input.checked = false;
        input.disabled = !selectable;
    }

    function updateCourtSummary(card) {
        const inputs = Array.from(card.querySelectorAll(".slot-checkbox"));
        const available = inputs.filter(function (input) {
            return input.nextElementSibling?.dataset.status === "Available" && !input.disabled;
        }).length;
        const held = inputs.filter(function (input) { return input.nextElementSibling?.dataset.status === "Held"; }).length;
        const maintenance = inputs.filter(function (input) {
            return ["Maintenance", "Inactive"].includes(input.nextElementSibling?.dataset.status || "");
        }).length;
        const copy = card.querySelector(".court-availability-copy");
        if (copy) {
            copy.textContent = maintenance === inputs.length && inputs.length > 0
                ? " · Sân đang đóng / bảo trì"
                : ` · ${available} ca trống${held ? ` · ${held} đang giữ` : ""}`;
        }
        const selectAll = card.querySelector("[data-action='select-all']");
        if (selectAll) selectAll.disabled = available === 0;
    }

    function updateAvailabilityCounter() {
        const visibleCards = Array.from(document.querySelectorAll(".court-item:not([hidden])"));
        let available = 0;
        let held = 0;
        visibleCards.forEach(function (card) {
            card.querySelectorAll(".slot-checkbox").forEach(function (input) {
                const status = input.nextElementSibling?.dataset.status;
                if (status === "Available" && !input.disabled) available += 1;
                if (status === "Held") held += 1;
            });
        });
        if (elements.availableCounter) {
            elements.availableCounter.textContent = `${available} ca trống${held ? ` · ${held} đang giữ` : ""}`;
        }
    }

    function updateSelectionState() {
        let count = 0;
        document.querySelectorAll(".court-item").forEach(function (card) {
            const cardCount = card.querySelectorAll(".slot-checkbox:checked").length;
            count += cardCount;
            card.classList.toggle("has-selection", cardCount > 0);
        });
        if (elements.selectionCount) {
            elements.selectionCount.textContent = count > 0 ? `Đang chọn ${count} ca cho ${formatDate(elements.date?.value)}` : "Chưa chọn ca nào cho ngày này";
        }
        if (elements.addButton) elements.addButton.disabled = count === 0;
    }

    function addSelectedSchedules() {
        const date = elements.date?.value;
        if (!date) {
            showToast("Vui lòng chọn ngày thi đấu.", true);
            elements.date?.focus();
            return;
        }

        let addedSlots = 0;
        document.querySelectorAll(".court-item").forEach(function (card) {
            const checked = Array.from(card.querySelectorAll(".slot-checkbox:checked"));
            if (checked.length === 0) return;
            const courtId = Number(card.dataset.courtId);
            const courtName = card.dataset.courtName || `Sân ${courtId}`;
            let schedule = state.schedules.find(function (item) { return item.courtId === courtId && item.date === date; });
            if (!schedule) {
                schedule = { id: createId(), courtId: courtId, courtName: courtName, date: date, slots: [], services: [] };
                state.schedules.push(schedule);
            }

            checked.forEach(function (input) {
                const slotId = Number(input.dataset.slotId);
                if (schedule.slots.some(function (slot) { return slot.id === slotId; })) return;
                schedule.slots.push({ id: slotId, name: input.dataset.slotName || `Ca ${slotId}`, price: Number(input.dataset.price) || 0 });
                addedSlots += 1;
                input.checked = false;
            });
            mergeServices(schedule.services, readServices(courtId));
            resetServices(courtId);
        });

        state.schedules = state.schedules.filter(function (schedule) { return schedule.slots.length > 0; });
        if (addedSlots === 0) {
            showToast("Các ca đã chọn đều có trong lịch. Vui lòng chọn ca khác.", true);
            return;
        }
        refreshRenderedSlotStates();
        updateSelectionState();
        updateSummary();
        hideConflict();
        showToast(`Đã thêm ${addedSlots} ca vào lịch thi đấu.`);
    }

    function readServices(courtId) {
        return Array.from(document.querySelectorAll(`.service-input[data-court-id="${cssEscape(courtId)}"]`))
            .map(function (input) {
                return {
                    serviceId: Number(input.dataset.serviceId),
                    serviceName: input.dataset.serviceName || "Dịch vụ",
                    quantity: Math.max(0, Number(input.value) || 0),
                    price: Number(input.dataset.servicePrice) || 0
                };
            })
            .filter(function (service) { return service.quantity > 0; });
    }

    function mergeServices(target, incoming) {
        incoming.forEach(function (service) {
            const existing = target.find(function (item) { return item.serviceId === service.serviceId; });
            if (existing) existing.quantity += service.quantity;
            else target.push(service);
        });
    }

    function resetServices(courtId) {
        document.querySelectorAll(`.service-input[data-court-id="${cssEscape(courtId)}"]`).forEach(function (input) { input.value = "0"; });
    }

    function removeSchedule(id) {
        state.schedules = state.schedules.filter(function (schedule) { return schedule.id !== id; });
        refreshRenderedSlotStates();
        updateSummary();
    }

    function refreshRenderedSlotStates() {
        const date = elements.date?.value;
        document.querySelectorAll(".slot-checkbox").forEach(function (input) {
            const item = state.availability.get(availabilityKey(input.dataset.courtId, date, input.dataset.slotId));
            if (item) applySlotStatus(input, item.status, item.price, date);
        });
        document.querySelectorAll(".court-item").forEach(updateCourtSummary);
        updateAvailabilityCounter();
    }

    function updateSummary() {
        if (elements.summaryName) elements.summaryName.textContent = elements.name?.value.trim() || "Chưa nhập tên giải";
        const sorted = state.schedules.slice().sort(function (a, b) { return a.date.localeCompare(b.date) || a.courtId - b.courtId; });
        let slotCount = 0;
        let total = 0;
        const courtCount = new Set();

        if (elements.summaryList) {
            if (sorted.length === 0) {
                elements.summaryList.innerHTML = '<div class="tc-empty">Chọn sân và ca, sau đó bấm “Thêm vào lịch thi đấu”.</div>';
            } else {
                elements.summaryList.innerHTML = sorted.map(function (schedule) {
                    courtCount.add(schedule.courtId);
                    slotCount += schedule.slots.length;
                    total += schedule.slots.reduce(function (sum, slot) { return sum + slot.price; }, 0);
                    total += schedule.services.reduce(function (sum, service) { return sum + service.price * service.quantity; }, 0);
                    const servicesText = schedule.services.length > 0
                        ? `<div class="tc-schedule-service"><i class="bi bi-box-seam me-1"></i>${schedule.services.map(function (service) { return `${escapeHtml(service.serviceName)} × ${service.quantity}`; }).join(", ")}</div>`
                        : "";
                    return `<div class="tc-schedule-item">
                        <div class="tc-schedule-title">${escapeHtml(schedule.courtName)} · ${formatDate(schedule.date)}</div>
                        <div class="tc-schedule-meta"><i class="bi bi-clock me-1"></i>${schedule.slots.map(function (slot) { return escapeHtml(slot.name); }).join(", ")}</div>
                        ${servicesText}
                        <button type="button" class="tc-remove-schedule" data-remove-schedule="${schedule.id}" aria-label="Xóa lịch ${escapeHtml(schedule.courtName)}"><i class="bi bi-x-lg"></i></button>
                    </div>`;
                }).join("");
                elements.summaryList.querySelectorAll("[data-remove-schedule]").forEach(function (button) {
                    button.addEventListener("click", function () { removeSchedule(button.dataset.removeSchedule); });
                });
            }
        }

        if (elements.summaryCount) elements.summaryCount.textContent = `${courtCount.size} sân · ${slotCount} ca`;
        if (elements.summaryTotal) elements.summaryTotal.textContent = formatMoney(total);
        if (elements.submitButton && !state.submitting) elements.submitButton.disabled = state.schedules.length === 0;
    }

    async function submitTournament(event) {
        event.preventDefault();
        if (state.submitting) return;
        hideConflict();
        if (!elements.form?.checkValidity()) {
            elements.form?.reportValidity();
            return;
        }
        if (state.schedules.length === 0) {
            showToast("Vui lòng thêm ít nhất một ca vào lịch thi đấu.", true);
            return;
        }

        setSubmitting(true, "Đang kiểm tra lại lịch...");
        try {
            const valid = await preflightSchedules();
            if (!valid) return;
            setSubmitting(true, "Đang khóa ca và giữ chỗ...");
            const response = await fetch(config.createUrl, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                    "RequestVerificationToken": getAntiForgeryToken()
                },
                body: JSON.stringify(buildPayload())
            });
            const result = await readJson(response);
            if (response.ok && result.success) {
                window.location.assign(result.redirectUrl);
                return;
            }
            showConflict(result.message || "Một hoặc nhiều ca vừa được người khác chọn. Lịch đã được làm mới.");
            await refreshAvailability();
        } catch (error) {
            console.error("Tournament submit error:", error);
            showToast("Không kết nối được máy chủ. Lịch của bạn vẫn được giữ trên trang để thử lại.", true);
        } finally {
            setSubmitting(false);
        }
    }

    async function preflightSchedules() {
        const unique = Array.from(new Map(state.schedules.map(function (schedule) {
            return [`${schedule.courtId}|${schedule.date}`, schedule];
        })).values());
        const results = await Promise.all(unique.map(async function (schedule) {
            return { schedule: schedule, slots: await fetchAvailability(schedule.courtId, schedule.date) };
        }));
        const conflicts = [];

        results.forEach(function (result) {
            const remote = new Map(result.slots.map(function (slot) { return [Number(slot.slotId), normalizeStatus(slot.status)]; }));
            result.schedule.slots.slice().forEach(function (slot) {
                const status = remote.get(slot.id) || "Inactive";
                if (status !== "Available") {
                    conflicts.push(`${result.schedule.courtName} · ${slot.name} (${formatStatus(status)})`);
                    removeSlotFromSchedules(result.schedule.courtId, result.schedule.date, slot.id);
                }
            });
        });

        if (conflicts.length > 0) {
            refreshRenderedSlotStates();
            updateSummary();
            showConflict(`Đã bỏ ${conflicts.length} ca không còn khả dụng: ${conflicts.slice(0, 3).join("; ")}${conflicts.length > 3 ? "..." : ""}`);
            return false;
        }
        return true;
    }

    function buildPayload() {
        return {
            tournamentName: elements.name?.value.trim() || "",
            description: document.getElementById("inputDescription")?.value.trim() || "",
            promotionCode: document.getElementById("inputPromotionCode")?.value.trim() || null,
            note: document.getElementById("inputNote")?.value.trim() || null,
            courtSelections: state.schedules.map(function (schedule) {
                return {
                    courtId: schedule.courtId,
                    bookingDate: schedule.date,
                    slotIds: schedule.slots.map(function (slot) { return slot.id; }),
                    services: schedule.services.map(function (service) {
                        return { serviceId: service.serviceId, quantity: service.quantity };
                    })
                };
            })
        };
    }

    async function initializeRealtime() {
        if (!window.signalR || document.querySelectorAll(".court-item").length === 0) {
            setRealtimeMode("polling");
            return;
        }
        try {
            const connection = new window.signalR.HubConnectionBuilder()
                .withUrl(resolveHubUrl())
                .withAutomaticReconnect([0, 2000, 5000, 10000])
                .build();
            state.connection = connection;
            connection.on("SlotStatusChanged", onRealtimeSlotChanged);
            connection.onreconnecting(function () { setRealtimeMode("polling", "Mất kết nối trực tiếp · vẫn tự làm mới"); });
            connection.onreconnected(async function () {
                await joinCourtGroups(connection);
                setRealtimeMode("live");
                refreshAvailability({ silent: true });
            });
            connection.onclose(function () { setRealtimeMode("polling"); });
            await connection.start();
            await joinCourtGroups(connection);
            setRealtimeMode("live");
        } catch (error) {
            console.warn("SignalR unavailable, polling remains active:", error);
            setRealtimeMode("polling");
        }
    }

    async function joinCourtGroups(connection) {
        const ids = Array.from(document.querySelectorAll(".court-item")).map(function (card) { return Number(card.dataset.courtId); });
        await Promise.all(ids.map(function (courtId) { return connection.invoke("JoinCourtGroup", courtId); }));
    }

    function onRealtimeSlotChanged(courtId, slotId, date, newStatus) {
        courtId = Number(courtId);
        slotId = Number(slotId);
        const status = normalizeStatus(newStatus);
        const scheduled = findScheduledSlot(courtId, date, slotId);
        if (scheduled && status !== "Available") {
            const description = `${scheduled.schedule.courtName} · ${scheduled.slot.name}`;
            removeSlotFromSchedules(courtId, date, slotId);
            updateSummary();
            showConflict(`${description} vừa chuyển sang “${formatStatus(status)}” và đã được bỏ khỏi lịch của bạn.`);
        }

        if (elements.date?.value === date) {
            const input = document.querySelector(`.slot-checkbox[data-court-id="${cssEscape(courtId)}"][data-slot-id="${cssEscape(slotId)}"]`);
            const known = state.availability.get(availabilityKey(courtId, date, slotId));
            if (input) applySlotStatus(input, status, known?.price || Number(input.dataset.price) || 0, date);
            const card = input?.closest(".court-item");
            if (card) updateCourtSummary(card);
            updateAvailabilityCounter();
            updateSelectionState();
        }
    }

    function removeSlotFromSchedules(courtId, date, slotId) {
        state.schedules.forEach(function (schedule) {
            if (schedule.courtId === courtId && schedule.date === date) {
                schedule.slots = schedule.slots.filter(function (slot) { return slot.id !== slotId; });
            }
        });
        state.schedules = state.schedules.filter(function (schedule) { return schedule.slots.length > 0; });
    }

    function findScheduledSlot(courtId, date, slotId) {
        const schedule = state.schedules.find(function (item) { return item.courtId === courtId && item.date === date; });
        const slot = schedule?.slots.find(function (item) { return item.id === slotId; });
        return schedule && slot ? { schedule: schedule, slot: slot } : null;
    }

    function hasScheduledSlot(courtId, date, slotId) {
        return Boolean(findScheduledSlot(Number(courtId), date, Number(slotId)));
    }

    function resolveHubUrl() {
        const base = new URL(config.apiBaseUrl || "http://localhost:5000", window.location.origin);
        if (window.location.protocol === "https:" && base.protocol === "http:" && ["localhost", "127.0.0.1"].includes(base.hostname)) {
            base.protocol = "https:";
            if (base.port === "5000") base.port = "7075";
        }
        return `${base.toString().replace(/\/$/, "")}/hubs/slot-status`;
    }

    function setRealtimeMode(mode, customText) {
        if (!elements.realtime || !elements.realtimeText) return;
        elements.realtime.classList.toggle("is-live", mode === "live");
        elements.realtime.classList.toggle("is-polling", mode !== "live");
        elements.realtimeText.textContent = customText || (mode === "live"
            ? "Đang cập nhật trực tiếp"
            : `Tự làm mới mỗi ${Math.round((Number(config.pollIntervalMs) || 15000) / 1000)} giây`);
    }

    function markAllSlotsLoading() {
        document.querySelectorAll(".slot-checkbox").forEach(function (input) {
            input.checked = false;
            input.disabled = true;
            const label = input.nextElementSibling;
            if (!label) return;
            label.className = "tc-slot-label status-loading";
            label.dataset.status = "Loading";
            const copy = label.querySelector(".tc-slot-state");
            const price = label.querySelector(".tc-slot-price");
            if (copy) copy.textContent = "Đang kiểm tra";
            if (price) price.textContent = "--";
        });
        updateSelectionState();
    }

    function clearCurrentSelection() {
        document.querySelectorAll(".slot-checkbox:checked").forEach(function (input) { input.checked = false; });
        updateSelectionState();
    }

    function setSubmitting(active, text) {
        state.submitting = active;
        if (!elements.submitButton) return;
        elements.submitButton.disabled = active || state.schedules.length === 0;
        elements.submitButton.innerHTML = active
            ? `<span class="spinner-border spinner-border-sm me-2" aria-hidden="true"></span>${escapeHtml(text || "Đang xử lý...")}`
            : '<i class="bi bi-shield-check me-2"></i>Xác nhận và giữ chỗ';
    }

    function showConflict(message) {
        if (!elements.conflictBanner || !elements.conflictMessage) return;
        elements.conflictMessage.textContent = message;
        elements.conflictBanner.hidden = false;
        elements.conflictBanner.scrollIntoView({ behavior: "smooth", block: "center" });
    }

    function hideConflict() {
        if (elements.conflictBanner) elements.conflictBanner.hidden = true;
        document.querySelectorAll(".has-conflict").forEach(function (card) { card.classList.remove("has-conflict"); });
    }

    function showToast(message, error) {
        if (!elements.toast) return;
        window.clearTimeout(state.toastTimer);
        elements.toast.textContent = message;
        elements.toast.classList.toggle("is-error", Boolean(error));
        elements.toast.classList.add("is-visible");
        state.toastTimer = window.setTimeout(function () { elements.toast.classList.remove("is-visible"); }, 3500);
    }

    function normalizeStatus(status) {
        const value = String(status || "").toLowerCase();
        if (value === "available") return "Available";
        if (["held", "pending", "hold"].includes(value)) return "Held";
        if (["booked", "confirmed", "inuse"].includes(value)) return "Booked";
        if (value === "maintenance") return "Maintenance";
        if (["inactive", "closed"].includes(value)) return "Inactive";
        if (value === "loading") return "Loading";
        if (value === "error") return "Error";
        return "Inactive";
    }

    function formatStatus(status) {
        return (statusMeta[normalizeStatus(status)] || statusMeta.Inactive).label;
    }

    function statusPriceLabel(status) {
        if (status === "Held") return "Đang giữ";
        if (status === "Booked") return "Đã đặt";
        if (status === "Maintenance") return "Bảo trì";
        if (status === "Inactive") return "Tạm đóng";
        return "--";
    }

    function availabilityKey(courtId, date, slotId) {
        return `${Number(courtId)}|${date}|${Number(slotId)}`;
    }

    function formatMoney(value) {
        return `${Math.max(0, Number(value) || 0).toLocaleString("vi-VN")}đ`;
    }

    function formatDate(value) {
        if (!value) return "--/--/----";
        const parts = value.split("-");
        return parts.length === 3 ? `${parts[2]}/${parts[1]}/${parts[0]}` : value;
    }

    function createId() {
        return `schedule_${Date.now()}_${Math.random().toString(36).slice(2, 8)}`;
    }

    function getAntiForgeryToken() {
        return document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";
    }

    async function readJson(response) {
        try { return await response.json(); }
        catch (_) { return { success: false, message: `Máy chủ trả về lỗi ${response.status}.` }; }
    }

    function escapeHtml(value) {
        const element = document.createElement("div");
        element.textContent = String(value ?? "");
        return element.innerHTML;
    }

    function cssEscape(value) {
        return window.CSS?.escape ? window.CSS.escape(String(value)) : String(value).replace(/[^a-zA-Z0-9_-]/g, "\\$&");
    }
})();

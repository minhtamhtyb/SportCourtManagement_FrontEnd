$(document).ready(function () {
    const token = window.accessToken;
    const baseUrl = (window.API_BASE_URL || "https://localhost:7075").replace(/\/$/, "");

    if (!token) {
        console.log("Notifications: No authenticated token found. Real-time connection skipped.");
        return;
    }

    // Connect to backend SignalR Hub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(baseUrl + "/hubs/notifications", {
            accessTokenFactory: () => token
        })
        .withAutomaticReconnect()
        .build();

    // Start connection
    connection.start()
        .then(function () {
            console.log("SignalR: Connected to NotificationHub.");
            loadNotifications();
        })
        .catch(function (err) {
            console.error("SignalR Connection Error: " + err.toString());
        });

    // Listen for real-time notification
    connection.on("ReceiveNotification", function (notification) {
        console.log("Real-time notification received: ", notification);

        // Show Toast
        showToastNotification(notification.title || notification.Title, "info");

        // Reload notifications dropdown
        loadNotifications();
    });
});

async function loadNotifications() {
    try {
        // 1. Get Unread Count
        const countRes = await fetch("/Notification/GetUnreadCount");
        if (countRes.ok) {
            const countData = await countRes.json();
            const count = countData.count;

            // Update admin & customer badges
            const badge = document.getElementById("notificationBadge");
            const custBadge = document.getElementById("customerNotificationBadge");

            if (badge) {
                if (count > 0) {
                    badge.classList.remove("d-none");
                    badge.innerText = count > 9 ? "9+" : count;
                } else {
                    badge.classList.add("d-none");
                }
            }
            if (custBadge) {
                if (count > 0) {
                    custBadge.classList.remove("d-none");
                    custBadge.innerText = count > 9 ? "9+" : count;
                } else {
                    custBadge.classList.add("d-none");
                }
            }
        }

        // 2. Get Notifications List
        const listRes = await fetch("/Notification/GetMyNotifications?limit=10");
        if (listRes.ok) {
            const notifications = await listRes.json();
            renderNotificationsList(notifications);
        }
    } catch (err) {
        console.error("Failed to load notifications: ", err);
    }
}

function renderNotificationsList(notifications) {
    const container = document.getElementById("notificationItemsContainer");
    const custContainer = document.getElementById("customerNotificationItemsContainer");

    const noNotifHtml = `<li class="text-center py-3 text-muted" style="font-style: italic; font-size: 13px;">Không có thông báo mới</li>`;
    const custNoNotifHtml = `<li class="text-center py-3 text-white-50" style="font-style: italic; font-size: 13px;">Không có thông báo mới</li>`;

    // Process for Admin side
    if (container) {
        if (!notifications || notifications.length === 0) {
            container.innerHTML = noNotifHtml;
        } else {
            let html = "";
            notifications.forEach(n => {
                const icon = getNotificationIcon(n.type);
                const unreadClass = n.isRead ? "" : "unread";
                html += `
                    <li class="notification-item ${unreadClass}" style="border-bottom: 1px solid rgba(255,255,255,0.03);">
                        <a class="dropdown-item py-2 px-3 rounded d-flex align-items-start gap-2" href="javascript:void(0)" onclick="markNotificationAsRead(${n.notificationId})" style="white-space: normal; color: inherit;">
                            <span class="material-symbols-outlined mt-0.5 text-primary" style="font-size: 18px;">${icon}</span>
                            <div style="flex: 1;">
                                <p style="margin: 0; font-size: 13px; line-height: 1.4; color: var(--on-surface);">${n.title}</p>
                                <small style="color: var(--on-surface-variant); font-size: 11px; margin-top: 3px; display: block;">${formatDateString(n.createdAt)}</small>
                            </div>
                            ${n.isRead ? '' : '<span class="bg-primary rounded-circle mt-2" style="width: 6px; height: 6px; display: inline-block; flex-shrink: 0;"></span>'}
                        </a>
                    </li>
                `;
            });
            container.innerHTML = html;
        }
    }

    // Process for Customer side
    if (custContainer) {
        if (!notifications || notifications.length === 0) {
            custContainer.innerHTML = custNoNotifHtml;
        } else {
            let html = "";
            notifications.forEach(n => {
                const icon = getCustomerNotificationIcon(n.type);
                const unreadClass = n.isRead ? "" : "unread";
                html += `
                    <li class="notification-item ${unreadClass}" style="border-bottom: 1px solid rgba(255,255,255,0.05);">
                        <a class="dropdown-item py-2 px-3 rounded d-flex align-items-start gap-2" href="javascript:void(0)" onclick="markNotificationAsRead(${n.notificationId})" style="white-space: normal; color: #fff;">
                            <i class="bi ${icon} text-info mt-0.5" style="font-size: 16px;"></i>
                            <div style="flex: 1;">
                                <p style="margin: 0; font-size: 13px; line-height: 1.4; color: #fff;">${n.title}</p>
                                <small style="color: rgba(255,255,255,0.5); font-size: 11px; margin-top: 3px; display: block;">${formatDateString(n.createdAt)}</small>
                            </div>
                            ${n.isRead ? '' : '<span class="bg-info rounded-circle mt-2" style="width: 6px; height: 6px; display: inline-block; flex-shrink: 0;"></span>'}
                        </a>
                    </li>
                `;
            });
            custContainer.innerHTML = html;
        }
    }
}

async function markNotificationAsRead(id) {
    try {
        const res = await fetch(`/Notification/MarkAsRead?id=${id}`, { method: 'POST' });
        if (res.ok) {
            loadNotifications();
        }
    } catch (err) {
        console.error("Failed to mark read: ", err);
    }
}

async function markAllNotificationsAsRead() {
    try {
        const res = await fetch('/Notification/MarkAllAsRead', { method: 'POST' });
        if (res.ok) {
            loadNotifications();
        }
    } catch (err) {
        console.error("Failed to mark all read: ", err);
    }
}

function getNotificationIcon(type) {
    switch (type) {
        case "BookingConfirm": return "check_circle";
        case "BookingCancel": return "cancel";
        case "PaymentSuccess": return "payments";
        case "PaymentFail": return "money_off";
        case "Reminder": return "schedule";
        case "Promotion": return "campaign";
        case "Waitlist": return "hourglass_empty";
        default: return "notifications";
    }
}

function getCustomerNotificationIcon(type) {
    switch (type) {
        case "BookingConfirm": return "bi-check-circle-fill";
        case "BookingCancel": return "bi-x-circle-fill";
        case "PaymentSuccess": return "bi-credit-card-2-front-fill";
        case "PaymentFail": return "bi-exclamation-triangle-fill";
        case "Reminder": return "bi-alarm-fill";
        case "Promotion": return "bi-megaphone-fill";
        case "Waitlist": return "bi-hourglass-split";
        default: return "bi-bell-fill";
    }
}

function formatDateString(dateStr) {
    if (!dateStr) return "";
    try {
        const d = new Date(dateStr);
        if (isNaN(d.getTime())) return dateStr;
        return d.toLocaleDateString("vi-VN", {
            day: "2-digit",
            month: "2-digit",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit"
        });
    } catch (e) {
        return dateStr;
    }
}

function showToastNotification(message, type = "info") {
    let container = document.getElementById("toastContainer");
    if (!container) {
        container = document.createElement("div");
        container.id = "toastContainer";
        container.style = "position: fixed; top: 20px; right: 20px; z-index: 9999; display: flex; flex-direction: column; gap: 10px;";
        document.body.appendChild(container);
    }

    const toast = document.createElement("div");
    toast.className = `toast-notification ${type}`;
    toast.style = `
        background: rgba(20, 27, 45, 0.95);
        border: 1px solid rgba(56, 189, 248, 0.3);
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.45);
        backdrop-filter: blur(10px);
        border-radius: 8px;
        color: #fff;
        padding: 12px 18px;
        font-size: 13px;
        display: flex;
        align-items: center;
        gap: 10px;
        min-width: 280px;
        max-width: 380px;
        transition: all 0.3s ease;
        animation: toastFadeIn 0.3s cubic-bezier(0.4, 0, 0.2, 1) forwards;
    `;

    // inject slide-in animation stylesheet if not exists
    if (!document.getElementById("toastStyleSheet")) {
        const style = document.createElement("style");
        style.id = "toastStyleSheet";
        style.innerHTML = `
            @keyframes toastFadeIn {
                from { opacity: 0; transform: translateY(-20px) scale(0.9); }
                to { opacity: 1; transform: translateY(0) scale(1); }
            }
            @keyframes toastFadeOut {
                from { opacity: 1; transform: translateY(0) scale(1); }
                to { opacity: 0; transform: translateY(-20px) scale(0.9); }
            }
        `;
        document.head.appendChild(style);
    }

    const icon = type === "success" ? "check_circle" : type === "error" ? "error" : "notifications";
    const color = type === "success" ? "#10b981" : type === "error" ? "#ef4444" : "#38bdf8";

    toast.innerHTML = `
        <span class="material-symbols-outlined" style="color: ${color}; font-size: 20px;">${icon}</span>
        <div style="flex: 1; line-height: 1.4; font-weight: 500;">${message}</div>
        <button onclick="this.parentElement.remove()" style="background: none; border: none; color: rgba(255,255,255,0.4); cursor: pointer; padding: 0; display: flex; align-items: center;"><span class="material-symbols-outlined" style="font-size: 16px;">close</span></button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = "toastFadeOut 0.3s cubic-bezier(0.4, 0, 0.2, 1) forwards";
        setTimeout(() => toast.remove(), 300);
    }, 5000);
}

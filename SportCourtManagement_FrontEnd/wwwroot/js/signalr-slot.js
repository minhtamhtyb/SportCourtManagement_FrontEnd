$(document).ready(function () {
    const courtId = $('#court-id').val();
    if (!courtId) return;

    // Connect to backend SignalR Hub (Local base URL is resolved automatically as relative URL)
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("https://localhost:7075/hubs/slot-status") // Fallback to absolute if different port
        .withAutomaticReconnect()
        .build();

    // Start connection
    connection.start()
        .then(function () {
            console.log("SignalR: Connected to SlotStatusHub for court ID: " + courtId);
            // Join court-specific group
            connection.invoke("JoinCourtGroup", parseInt(courtId))
                .catch(err => console.error("SignalR join group error: " + err.toString()));
        })
        .catch(function (err) {
            console.error("SignalR connection error: " + err.toString());
        });

    // Listen for slot status changed event
    connection.on("SlotStatusChanged", function (updatedCourtId, slotId, date, newStatus) {
        console.log(`SignalR Event: Court ${updatedCourtId}, Slot ${slotId}, Date ${date}, Status ${newStatus}`);
        
        if (parseInt(updatedCourtId) !== parseInt(courtId)) return;

        // Check if currently picked booking date matches the update
        const currentDate = $('#booking-date').val();
        if (currentDate !== date) return;

        // Find the slot element on UI
        const $slotBtn = $(`.sc-slot-btn[data-slot-id="${slotId}"]`);
        if ($slotBtn.length === 0) return;

        // Update classes and text based on new status
        $slotBtn.removeClass('sc-slot-available sc-slot-booked sc-slot-maintenance sc-slot-selected');
        
        const $priceLabel = $slotBtn.find('div').eq(1);
        const originalPrice = $slotBtn.data('price');

        if (newStatus === "Available") {
            $slotBtn.addClass('sc-slot-available');
            $slotBtn.attr('title', 'Còn trống');
            if (originalPrice) {
                $priceLabel.text(parseFloat(originalPrice).toLocaleString('vi-VN') + "đ");
            } else {
                $priceLabel.text("Còn trống");
            }
        } else if (newStatus === "Booked") {
            $slotBtn.addClass('sc-slot-booked');
            $slotBtn.attr('title', 'Đã đặt');
            $priceLabel.text("Đã đặt");
            $slotBtn.off('click'); // Disable clicking
            
            // If the selected slot was this booked one, clear selection
            if ($('#selected-slot-id').val() === slotId.toString()) {
                $('#selected-slot-id').val('');
                $('#selected-slot-label').text('-- Chưa chọn --');
            }
        } else if (newStatus === "Maintenance") {
            $slotBtn.addClass('sc-slot-maintenance');
            $slotBtn.attr('title', 'Bảo trì');
            $priceLabel.text("Bảo trì");
            $slotBtn.off('click'); // Disable clicking

            if ($('#selected-slot-id').val() === slotId.toString()) {
                $('#selected-slot-id').val('');
                $('#selected-slot-label').text('-- Chưa chọn --');
            }
        }
    });

    // Leave group when page unloading
    $(window).on('beforeunload', function () {
        if (connection.state === signalR.HubConnectionState.Connected) {
            connection.invoke("LeaveCourtGroup", parseInt(courtId)).catch(err => console.error(err));
        }
    });
});

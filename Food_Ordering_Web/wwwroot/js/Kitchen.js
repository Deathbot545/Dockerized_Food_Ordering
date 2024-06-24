window.getStatusColor = function (status) {
    console.log("Called getStatusColor from Kitchen Application", status);
    switch (status) {
        case 0: return "orange"; // Pending
        case 1: return "blue";   // Preparing
        case 2: return "green";  // Ready
        case 3: return "grey";   // Served
        case 4: return "red";    // Cancelled
        default: return "black"; // Unknown status or the default color
    }
};

window.mapEnumToStatusText = function (statusValue) {
    switch (statusValue) {
        case 0: return "Pending";
        case 1: return "Preparing";
        case 2: return "Ready";
        case 3: return "Served";
        case 4: return "Cancelled";
        default: return "Unknown";
    }
};

document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM content loaded");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`https://restosolutionssaas.com/api/OrderApi/orderStatusHub?isKitchen=true`)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    console.log("SignalR connection object before adding method:", connection);

    connection.on("ReceiveOrderUpdate", function (order) {
        console.log("ReceiveOrderUpdate method triggered with order:", order);
        if (order && order.orderId && typeof order.status !== 'undefined') {
            if (order.status === 4) { // Assuming 4 means 'Cancelled'
                handleCancellationAlert(order);
            }
            updateOrderUI(order);
        } else {
            console.error("Order update is missing required properties", order);
        }
    });

    connection.on("NewOrderPlaced", function (orderInfo) {
        console.log("NewOrderPlaced method triggered with order info:", orderInfo);
        addNewOrderToUI(orderInfo);
    });

    connection.onreconnecting(error => {
        console.warn(`Connection lost due to error "${error}". Reconnecting.`);
    });

    connection.onreconnected(connectionId => {
        console.log(`Connection reestablished. Connected with connectionId "${connectionId}".`);
    });

    connection.start()
        .then(function () {
            console.log("SignalR connection established");
        })
        .catch(function (err) {
            console.error("Error while starting SignalR connection:", err);
        });

    console.log("SignalR connection object after adding methods:", connection);

    function addNewOrderToUI(order) {
        console.log("Adding new order to UI:", order);
        const orderHtml = createOrderHtml(order);
        const sectionId = statusMappings[order.status]?.section || statusMappings.default.section;
        $('#' + sectionId).append(orderHtml);
    }

    function updateOrderUI(order) {
        console.log("Updating UI for order ID:", order.orderId, "with new status:", order.status);
        let orderStatusText = mapEnumToStatusText(order.status);
        let color = getStatusColor(order.status);
        const $orderCard = $(`.order-card[data-order-id="${order.orderId}"]`);

        if ($orderCard.length) {
            updateExistingOrderCard($orderCard, order, orderStatusText, color);
        } else {
            const orderHtml = createOrderHtml(order);
            const sectionId = statusMappings[order.status]?.section || statusMappings.default.section;
            $('#' + sectionId).append(orderHtml);
        }
    }

    function createOrderHtml(order) {
        const statusText = mapEnumToStatusText(order.status);
        const color = getStatusColor(order.status);
        const formattedDate = new Date(order.orderTime).toLocaleString('en-US', { hour12: false });
        const detailsHtml = order.orderDetails.map(detail => `
            <li>${detail.menuItemName} x ${detail.quantity} <br><small>Note: ${detail.note || 'No note'}</small></li>
        `).join("");
        const tableIdentifier = `Table: ${order.tableId}`;

        const cancelButtonHtml = (order.status === 2 || order.status === 3) ? "" :
            `<button type="button" class="btn btn-danger" data-status="cancelled">Cancel</button>`;

        return `
            <div class="card mb-3 order-card" data-order-id="${order.orderId}" data-table-id="${order.tableId}" style="background-color: ${color};">
                <div class="card-header">
                    Order #${order.orderId} | ${tableIdentifier} | Date: ${formattedDate} | STATUS: ${statusText}
                </div>
                <div class="card-body">
                    <ul>${detailsHtml}</ul>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-warning" data-status="pending">Pending</button>
                        <button type="button" class="btn btn-primary" data-status="preparing">Preparing</button>
                        <button type="button" class="btn btn-success" data-status="ready">Ready</button>
                        ${cancelButtonHtml}
                    </div>
                </div>
            </div>`;
    }

    function updateExistingOrderCard($orderCard, order, statusText, color) {
        const detailsHtml = order.orderDetails.map(detail => `
            <li>${detail.menuItemName} x ${detail.quantity} <br><small>Note: ${detail.note || 'No note'}</small></li>
        `).join("");

        $orderCard.find('.card-body ul').html(detailsHtml);
        $orderCard.find('.btn').removeClass('active');
        $orderCard.find(`.btn[data-status="${statusText.toLowerCase()}"]`).addClass('active');
        $orderCard.find('.card-header').html(
            `Order #${order.orderId} | Table: ${order.tableId} | Date: ${new Date(order.orderTime).toLocaleString('en-US', { hour12: false })} | STATUS: ${statusText}`
        ).css('background-color', color);
        console.log(`Order ${order.orderId} UI updated to ${statusText}`);
    }

    const notifiedCancellations = {};

    function handleCancellationAlert(order) {
        if (!notifiedCancellations[order.orderId]) {
            const cancellationAlert = `
                <div class="alert alert-warning alert-dismissible fade show" role="alert">
                    Order #${order.orderId} has been cancelled by the customer.
                    <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>`;
            $('#cancelledOrders').prepend(cancellationAlert);
            notifiedCancellations[order.orderId] = true;

            setTimeout(() => {
                $(`.alert:contains('Order #${order.orderId}')`).alert('close');
                delete notifiedCancellations[order.orderId];
            }, 300000); // 5 minutes
        }
    }

    $(document).on('click', '.btn-group .btn', function () {
        const $btn = $(this);
        const orderId = $btn.closest('.order-card').data('order-id');
        const newStatus = $btn.data('status');

        if ($btn.hasClass('disabled')) {
            console.error("Button is disabled for order ID:", orderId);
            return;
        }

        console.log("Clicked button:", $btn.text().trim(), "for order ID:", orderId, "to change status to:", newStatus);

        if (newStatus === "cancelled") {
            updateOrderStatusToCancelled(orderId);
        } else if (newStatus !== undefined) {
            updateOrderStatus(orderId, newStatus);
        } else {
            console.error("Undefined status for order ID:", orderId);
        }
    });

    function updateOrderStatus(orderId, newStatus) {
        let statusEnumValue = Object.keys(statusMappings).find(key => statusMappings[key].text.toLowerCase() === newStatus);
        console.log("Preparing to send update request for Order ID:", orderId, "with Status:", statusEnumValue);
        if (statusEnumValue === undefined) {
            console.error("Invalid status value for update:", newStatus);
            return;
        }

        $.ajax({
            url: 'https://restosolutionssaas.com/api/OrderApi/UpdateOrderStatus',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ OrderId: orderId, Status: statusEnumValue }),
            success: function (response) {
                console.log("Order status updated successfully for order ID:", orderId, "with response:", response);
                connection.invoke("SendOrderUpdate", { orderId: orderId, status: statusEnumValue })
                    .catch(err => console.error("Error sending update to hub for order ID:", orderId, "with error:", err));
            },
            error: function (xhr, status, error) {
                console.error("Error updating order status for order ID:", orderId, "with error:", error);
            }
        });
    }

    function updateOrderStatusToCancelled(orderId) {
        $.ajax({
            url: 'https://restosolutionssaas.com/api/OrderApi/CancelOrder',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ OrderId: orderId }),
            success: function (response) {
                console.log("Order cancelled successfully for order ID:", orderId, "with response:", response);
                connection.invoke("SendOrderUpdate", { orderId: orderId, status: 4 }) // Assuming 4 is the status for Cancelled
                    .catch(err => console.error("Error sending cancellation to hub for order ID:", orderId, "with error:", err));
            },
            error: function (xhr, status, error) {
                console.error("Error cancelling order for order ID:", orderId, "with error:", error);
            }
        });
    }
});

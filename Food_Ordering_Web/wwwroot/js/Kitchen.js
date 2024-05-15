window.getStatusColor = function (status) {
    console.log("Called getStatusColor from Kitchen Application", status);
    switch (status) {
        case 0: return "orange"; // Pending
        case 1: return "blue";   // Preparing
        case 2: return "green";  // Ready
        case 3: return "grey";   // Served
        default: return "black"; // Unknown status or the default color
    }
};

window.mapEnumToStatusText = function (statusValue) {
    switch (statusValue) {
        case 0: return "Pending";
        case 1: return "Preparing";
        case 2: return "Ready";
        case 3: return "Served";
        default: return "Unknown";
    }
};

document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM content loaded");

    let currentOrder = JSON.parse(localStorage.getItem('currentOrder'));
    if (currentOrder && typeof currentOrder.status === "number") {
        currentOrder.status = mapEnumToStatusText(currentOrder.status);
        localStorage.setItem('currentOrder', JSON.stringify(currentOrder));
    }

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`https://restosolutionssaas.com/api/OrderApi/orderStatusHub?isKitchen=true`)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    console.log("SignalR connection object before adding method:", connection);

    connection.on("ReceiveWaiterCall", function (tableId) {
        console.log("ReceiveWaiterCall method triggered for table ID:", tableId);
        addWaiterCallToTable(tableId);
    });

    connection.on("ReceiveOrderUpdate", function (order) {
        console.log("ReceiveOrderUpdate method triggered with order:", order);
        if (order && order.orderId && typeof order.status !== 'undefined') {
            if (order.status === 4) { // Assuming 4 means 'Cancelled'
                handleCancellationAlert(order);
            }
            updateOrderStatusUI(order);
            updateKitchenOrderStatusUI(order);
        } else {
            console.error("Order update is missing required properties", order);
        }
    });

    connection.on("NewOrderPlaced", function (order) {
        console.log("NewOrderPlaced method triggered with order:", order);
        updateKitchenUIWithNewOrder(order);
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

    function updateOrderStatusUI(order) {
        console.log("Order status update received:", order);
        let currentOrder = JSON.parse(localStorage.getItem('currentOrder')) || {};
        currentOrder.status = order.status; // Store the numeric value
        localStorage.setItem('currentOrder', JSON.stringify(currentOrder));

        let statusText = getOrderStatusText(mapEnumToStatusText(order.status)); // Map the status to its string representation here
        let color = getStatusColor(mapEnumToStatusText(order.status)); // Determine color based on status

        let orderStatusElement = document.getElementById("orderStatus");
        if (orderStatusElement) {
            orderStatusElement.innerHTML = `<span style="color: ${color};">${statusText}</span>`; // Apply color
            console.log(`Updated UI with new status: ${statusText}`);
        }
    }

    function updateKitchenOrderStatusUI(order) {
        console.log("Received kitchen update for order ID:", order.OrderId, "with new status:", order.Status);
        let orderStatusText = mapEnumToStatusText(order.Status);

        let $orderCard = $(`.order-card[data-order-id="${order.OrderId}"]`);
        if (!$orderCard.length) {
            console.log("No order card found for order ID:", order.OrderId);
            return;
        }

        $orderCard.find('.btn').removeClass('active');
        $orderCard.find(`.btn[data-status="${orderStatusText.toLowerCase()}"]`).addClass('active');
        $orderCard.find('.card-header span:last-child').text(`STATUS: ${orderStatusText.toUpperCase()}`);
        console.log(`Order ${order.OrderId} UI updated to ${orderStatusText}`);
    }

    function getOrderStatusText(status) {
        switch (status) {
            case 'Pending': return "Your order is being prepared.";
            case 'Preparing': return "Your order is in progress.";
            case 'Ready': return "Your order is ready for pickup!";
            case 'Served': return "Your order has been served.";
            case 'Delivered': return "Your order has been delivered.";
            default: return "Order status: " + status;
        }
    }

    function addWaiterCallToTable(tableId) {
        const tableIdentifier = `Table: ${tableId}`;
        const callId = Math.floor(Math.random() * 1000); // Generate a random Call ID for demonstration purposes

        const newCallHtml = `
            <tr>
                <td>${callId}</td>
                <td>${tableIdentifier}</td>
            </tr>
        `;

        console.log("Adding new waiter call to table with HTML:", newCallHtml);

        $('#waiterCalls tbody').append(newCallHtml);
    }

    function handleCancellationAlert(order) {
        const notifiedCancellations = {};
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
            return; // Prevent action if the button is disabled
        }

        console.log("Clicked button:", $btn.text().trim(), "for order ID:", orderId, "to change status to:", newStatus);

        if (newStatus === "cancelled") {
            updateOrderStatusToCancelled(orderId); // Call the cancellation function if "Cancel" is clicked
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
            return; // Exit if status is invalid
        }

        $.ajax({
            url: 'https://restosolutionssaas.com/api/OrderApi/UpdateOrderStatus',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify({ OrderId: orderId, Status: statusEnumValue }),
            success: function () {
                console.log("Successfully updated status for Order ID:", orderId);
                const orderCard = $(`.order-card[data-order-id="${orderId}"]`);
                if (orderCard.length) {
                    const formattedDate = new Date(orderCard.find('.card-header').text().split('|')[2].trim()).toLocaleString('en-US', { hour12: false });
                    const tableName = orderCard.find('.card-header').text().split('|')[1].trim();
                    orderCard.find('.card-header').html(
                        `Order #${orderId} | ${tableName} | Date: ${formattedDate} | STATUS: ${statusMappings[statusEnumValue].text}`
                    );
                    // Optionally, move the card to the correct section if status changes
                    const sectionId = statusMappings[statusEnumValue]?.section || statusMappings.default.section;
                    orderCard.detach().appendTo(`#${sectionId}`);
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                console.error("AJAX Error:", textStatus, "Response Text:", jqXHR.responseText, "Error Thrown:", errorThrown);
            }

        });
    }
    const statusMappings = {
        0: { text: "Pending", color: "#FFDDC1", section: 'pendingOrders' },
        1: { text: "In Progress", color: "#C1CEFF", section: 'preparingOrders' },
        2: { text: "Completed", color: "#C1FFD7", section: 'readyOrders' },
        3: { text: "Served", color: "#FFC1C1", section: 'servedOrders' },
        4: { text: "Cancelled", color: "grey" },
        default: { text: "Unknown", color: "#FFFFFF", section: 'unknownOrders' }
    };

    function createOrderHtml(order) {
        const statusText = statusMappings[order.status]?.text || statusMappings.default.text;
        const color = statusMappings[order.status]?.color || statusMappings.default.color;
        const formattedDate = new Date(order.orderTime).toLocaleString('en-US', { hour12: false });
        const detailsHtml = order.orderDetails.map(detail => `
        <li>${detail.menuItem.name} x ${detail.quantity} <br><small>Note: ${detail.note || 'No note'}</small></li>
    `).join("");
        const tableIdentifier = `Table: ${order.tableId}`;

        // Conditionally show the Cancel button if the status is not 'Completed' or 'Served'
        const cancelButtonHtml = (order.status === 2 || order.status === 3) ? "" :
            `<button type="button" class="btn btn-danger" data-status="cancelled">Cancel</button>`;

        return `
            <div class="card mb-3 order-card bg-light" data-order-id="${order.id}" data-table-id="${order.tableId}" style="background-color: ${color};">
                <div class="card-header">
                    Order #${order.id} | ${tableIdentifier} | Date: ${formattedDate} | STATUS: ${statusText}
                </div>
                <div class="card-body">
                    <ul>${detailsHtml}</ul>
                    <div class="btn-group" role="group">
                        <button type="button" class="btn btn-warning" data-status="pending">Pending</button>
                        <button type="button" class="btn btn-primary" data-status="preparing">In Progress</button>
                        <button type="button" class="btn btn-success" data-status="completed">Completed</button>
                        ${cancelButtonHtml}
                    </div>
                </div>
            </div>`;
    }

    function fetchTableName(tableId) {
        return fetch(`https://restosolutionssaas.com/api/TablesApi/GetTableName/${tableId}`)
            .then(response => {
                if (!response.ok) throw new Error('Failed to fetch table name');
                return response.text();
            })
            .catch(error => {
                console.error('Error fetching table name:', error);
                return "Unknown";
            });
    }

    function updateAllTableNames() {
        $('.order-card').each(function () {
            const $card = $(this);
            const tableId = $card.data('table-id');
            if (tableId && tableId !== 'Unknown' && !$card.data('table-name-updated')) {
                fetchTableName(tableId).then(tableName => {
                    $card.find('.card-header span').text(function (index, text) {
                        return text.replace(/Table: [^|]*/, `Table: ${tableName}`);
                    });
                    $card.data('table-name-updated', true);
                }).catch(() => {
                    $card.find('.card-header span').text(function (index, text) {
                        return text.replace(/Table: [^|]*/, "Table: Unknown");
                    });
                });
            }
        });
    }

    async function updateOrderStatusToCancelled(orderId) {
        const cancelledStatus = 4; // Assuming 4 represents "Cancelled"
        try {
            const response = await fetch(`https://restosolutionssaas.com/api/OrderApi/UpdateOrderStatus`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({
                    OrderId: orderId,
                    Status: cancelledStatus
                })
            });
            if (!response.ok) {
                throw new Error(`Failed to update order status: ${response.statusText}`);
            }
            alert('Order cancelled successfully.');
            location.reload(); // Optionally, you might want to reload or update UI in a different way
        } catch (error) {
            console.error('Error updating order status to cancelled:', error);
            alert('Error cancelling order. Please try again.');
        }
    }

    // Clear existing dummy data from the table
    $('#waiterCalls tbody').empty();
window.makeorder = function (order) {
    const statusText = mapEnumToStatusText(order.status);
    const color = getStatusColor(order.status);
    const formattedDate = new Date(order.orderTime).toLocaleString('en-US', { hour12: false });
    const tableIdentifier = `Table: ${order.tableId}`;

    const cancelButtonHtml = (order.status === 2 || order.status === 3) ? "" :
        `<button type="button" class="btn btn-danger" data-status="cancelled">Cancel</button>`;

    let detailsHtml = "";
    if (Array.isArray(order.orderDetails)) {
        detailsHtml = order.orderDetails.map(detail => `
            <li>${detail.menuItemName} x ${detail.quantity} <br><small>Note: ${detail.note || 'No note'}</small></li>
        `).join("");
    }

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
window.statusMappings = {
    0: { text: "Pending", section: "pendingOrders" },
    1: { text: "Preparing", section: "preparingOrders" },
    2: { text: "Ready", section: "readyOrders" },
    3: { text: "Served", section: "servedOrders" },
    4: { text: "Cancelled", section: "cancelledOrders" },
    default: { text: "Unknown", section: "unknownOrders" }
};


document.addEventListener("DOMContentLoaded", function () {
    console.log("DOM content loaded");

    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`https://restosolutionssaas.com/api/OrderApi/orderStatusHub?isKitchen=true`)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

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

    connection.start()
        .then(function () {
            console.log("SignalR connection established");
        })
        .catch(function (err) {
            console.error("Error while starting SignalR connection:", err);
        });

    function addNewOrderToUI(order) {
        console.log("Adding new order to UI:", order);
        const newOrder = {
            Id: order.orderId,
            OrderTime: new Date(order.orderTime),
            Customer: null, // Assuming this is not provided by the hub
            TableId: order.tableId,
            TableName: null, // Assuming this is not provided by the hub
            OutletId: order.outletId,
            Status: order.status,
            OrderDetails: Array.isArray(order.orderDetails) ? order.orderDetails.map(detail => ({
                Id: 0, // Assign a unique id or use detail.orderId if available
                OrderId: 0, // Assign the order id or use detail.orderId if available
                MenuItemId: detail.menuItemId,
                MenuItemName: detail.menuItemName,
                MenuItem: null, // Assuming this is not provided by the hub
                Quantity: detail.quantity,
                Note: detail.note,
                Size: detail.size,
                ExtraItems: Array.isArray(detail.extraItems) ? detail.extraItems.map(extraItem => ({
                    Id: 0, // Assign a unique id or use extraItem.id if available
                    Name: extraItem.name,
                    Price: extraItem.price
                })) : []
            })) : []
        };

        const orderHtml = makeorder(newOrder, []);
        const sectionId = statusMappings[order.status]?.section || statusMappings.default.section;
        document.getElementById(sectionId).insertAdjacentHTML('beforeend', orderHtml);
    }


    function updateExistingOrderCard(orderCard, order, statusText, color) {
        const detailsHtml = Array.isArray(order.orderDetails) ? order.orderDetails.map(detail => `
        <li>${detail.menuItemName} x ${detail.quantity} <br><small>Note: ${detail.note || 'No note'}</small></li>
    `).join("") : "";

        orderCard.querySelector('.card-body ul').innerHTML = detailsHtml;
        orderCard.querySelector('.btn').classList.remove('active');
        orderCard.querySelector(`.btn[data-status="${statusText.toLowerCase()}"]`).classList.add('active');
        orderCard.querySelector('.card-header').innerHTML =
            `Order #${order.orderId} | Table: ${order.tableId} | Date: ${new Date(order.orderTime).toLocaleString('en-US', { hour12: false })} | STATUS: ${statusText}`;
        orderCard.querySelector('.card-header').style.backgroundColor = color;
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
            document.getElementById('cancelledOrders').insertAdjacentHTML('afterbegin', cancellationAlert);
            notifiedCancellations[order.orderId] = true;

            setTimeout(() => {
                const alert = document.querySelector(`.alert:contains('Order #${order.orderId}')`);
                if (alert) {
                    alert.parentNode.removeChild(alert);
                }
                delete notifiedCancellations[order.orderId];
            }, 300000); // 5 minutes
        }
    }

    document.addEventListener('click', function (event) {
        const button = event.target.closest('.btn-group .btn');
        if (!button) return;

        const orderCard = button.closest('.order-card');
        const orderId = orderCard.dataset.orderId;
        const newStatus = button.dataset.status;

        if (button.classList.contains('disabled')) {
            console.error("Button is disabled for order ID:", orderId);
            return;
        }

        console.log("Clicked button:", button.textContent.trim(), "for order ID:", orderId, "to change status to:", newStatus);

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

        fetch('https://restosolutionssaas.com/api/OrderApi/UpdateOrderStatus', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ OrderId: orderId, Status: statusEnumValue })
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error("Failed to update order status");
                }
            })
            .then(data => {
                console.log("Order status updated successfully for order ID:", orderId, "with response:", data);
                connection.invoke("SendOrderUpdate", { orderId: orderId, status: statusEnumValue })
                    .catch(err => console.error("Error sending update to hub for order ID:", orderId, "with error:", err));
            })
            .catch(err => {
                console.error("Error updating order status for order ID:", orderId, "with error:", err);
            });
    }

    function updateOrderStatusToCancelled(orderId) {
        fetch('https://restosolutionssaas.com/api/OrderApi/CancelOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ OrderId: orderId })
        })
            .then(response => {
                if (response.ok) {
                    return response.json();
                } else {
                    throw new Error("Failed to cancel order");
                }
            })
            .then(data => {
                console.log("Order cancelled successfully for order ID:", orderId, "with response:", data);
                connection.invoke("SendOrderUpdate", { orderId: orderId, status: 4 }) // Assuming 4 is the status for Cancelled
                    .catch(err => console.error("Error sending cancellation to hub for order ID:", orderId, "with error:", err));
            })
            .catch(err => {
                console.error("Error cancelling order for order ID:", orderId, "with error:", err);
            });
    }
});

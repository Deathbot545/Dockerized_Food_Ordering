function mapEnumToStatusText(statusValue) {
    switch (statusValue) {
        case 0:
            return "Pending";
        case 1:
            return "Preparing";
        case 2:
            return "Ready";
        case 3:
            return "Served";
        default:
            return "Unknown";
    }
}

document.addEventListener("DOMContentLoaded", function () {

    let currentOrder = JSON.parse(localStorage.getItem('currentOrder'));
    if (currentOrder && typeof currentOrder.status === "number") {
        currentOrder.status = mapEnumToStatusText(currentOrder.status);
        localStorage.setItem('currentOrder', JSON.stringify(currentOrder));
    }
    if (currentOrder) {
        // If 'currentOrder' exists in local storage, establish a SignalR connection.

        const connection = new signalR.HubConnectionBuilder()
            .withUrl("https://localhost:7268/orderStatusHub?orderId=" + currentOrder.orderId)
            .configureLogging(signalR.LogLevel.Information)
            .build();


        connection.on("NewOrderPlaced", function (order) {
            // Notify kitchen UI about the new order
            updateKitchenUIWithNewOrder(order);
        });

        connection.on("ReceiveOrderUpdate", function (order) {
            // Use camelCase for accessing properties
            updateOrderStatusUI(order);
            updateKitchenOrderStatusUI(order);
        });

        // Handle reconnection events
        connection.onreconnecting(error => {
            console.warn(`Connection lost due to error "${error}". Reconnecting.`);
        });

        connection.onreconnected(connectionId => {
            console.log(`Connection reestablished. Connected with connectionId "${connectionId}".`);
        });

        connection.start().catch(function (err) {
            return console.error(err.toString());
        });

        // Since 'currentOrder' exists, show the indicator to the user.
        let statusText = getOrderStatusText(currentOrder.status);

        let orderStatusElement = document.getElementById("orderStatus");
        if (orderStatusElement) {
            orderStatusElement.innerText = statusText;
        } else {
            console.warn("Element with ID 'orderStatus' not found.");
        }
    }

    function updateOrderStatusUI(order) {
        let currentOrder = JSON.parse(localStorage.getItem('currentOrder')) || {};
        currentOrder.status = order.status;  // Store the numeric value
        localStorage.setItem('currentOrder', JSON.stringify(currentOrder));

        let statusText = getOrderStatusText(mapEnumToStatusText(order.status));  // Map the status to its string representation here
        let orderStatusElement = document.getElementById("orderStatus");
        if (orderStatusElement) {
            orderStatusElement.innerText = statusText;
        }
    }

    function updateKitchenOrderStatusUI(order) {
        let orderStatusText = mapEnumToStatusText(order.Status);

        // Find the order card that corresponds to the updated order.
        let $orderCard = $(`.order-card[data-order-id="${order.OrderId}"]`);

        if (!$orderCard.length) return;

        $orderCard.find('.btn').removeClass('active');
        $orderCard.find(`.btn[data-status="${orderStatusText.toLowerCase()}"]`).addClass('active');

        $orderCard.find('.card-header span:last-child').text(`STATUS: ${orderStatusText.toUpperCase()}`);
    }

    function getOrderStatusText(status) {
        switch (status) {
            case 'Pending': return "Your order is being prepared.";
            case 'Preparing': return "Your order is in progress."; // <-- Added this
            case 'Ready': return "Your order is ready for pickup!";
            case 'Served': return "Your order has been served."; // <-- You might want to add a message for this status as well
            case 'Delivered': return "Your order has been delivered."; // Note: This status is not in mapEnumToStatusText
            default: return "Order status: " + status;
        }
    }

});

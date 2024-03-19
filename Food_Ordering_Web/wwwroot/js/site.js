function getStatusColor(status) {
    switch (status) {
        case "Pending": return "orange";
        case "Preparing": return "blue";
        case "Ready": return "green";
        case "Served": return "grey";
        default: return "black"; // Unknown status or the default color
    }
}
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
}document.addEventListener("DOMContentLoaded", async function () {
    let currentOrder = JSON.parse(localStorage.getItem('currentOrder'));

    // Function to check and update the order status
    async function checkAndUpdateOrderStatus() {
        if (currentOrder) {
            try {
                const currentStatus = await fetchCurrentOrderStatus(currentOrder.orderId);
                if (currentStatus !== undefined) {
                    updateUIWithCurrentStatus(currentStatus);
                }
            } catch (error) {
                console.error("Error updating order status:", error);
            }
        }
    }

    // Immediately check and update order status
    await checkAndUpdateOrderStatus();

    // Setup SignalR connection if there is a current order
    if (currentOrder) {
        console.log("Establishing SignalR connection for order status updates.");
        const connection = new signalR.HubConnectionBuilder()
            .withUrl(`https://restosolutionssaas.com:7268/orderStatusHub?orderId=${currentOrder.orderId}`)
            .configureLogging(signalR.LogLevel.Information)
            .withAutomaticReconnect()
            .build();

        connection.on("NewOrderPlaced", function (order) {
            console.log("SignalR: New order placed", order);
        });

        connection.on("ReceiveOrderUpdate", function (order) {
            console.log("SignalR: Order status update received", order);
            updateOrderStatusUI(order);
        });

        connection.onreconnecting(error => {
            console.warn("SignalR connection lost, attempting to reconnect...", error);
        });

        connection.onreconnected(connectionId => {
            console.log("SignalR connection reestablished, connectionId:", connectionId);
        });

        // Start the SignalR connection
        await connection.start().catch(err => console.error("SignalR connection error:", err));

        // Make the order status element clickable
        document.addEventListener("click", function (event) {
            if (event.target.closest("#orderStatus")) {
                navigateToOrderPage(currentOrder);
            }
        });
    }
});

// Function to fetch the current order status from the API
async function fetchCurrentOrderStatus(orderId) {
    try {
        const response = await fetch(`https://restosolutionssaas.com:7268/api/OrderApi/GetOrderStatus/${orderId}`);
        if (!response.ok) {
            throw new Error('Failed to fetch current order status');
        }
        const data = await response.json();
        return mapEnumToStatusText(data.status);
    } catch (error) {
        console.error('Error fetching order status:', error);
    }
}

// Function to update the UI with the current status
function updateUIWithCurrentStatus(status) {
    let statusText = getOrderStatusText(status);
    let color = getStatusColor(status);

    let orderStatusElement = document.getElementById("orderStatus");
    if (orderStatusElement) {
        orderStatusElement.innerHTML = `<i class="fas fa-truck mr-2"></i><span class="badge" style="background-color: ${color}; font-size: 1.25em;">${statusText}</span>`;
    }
}

// Function to handle updates received via SignalR
function updateOrderStatusUI(order) {
    let status = mapEnumToStatusText(order.status);
    updateUIWithCurrentStatus(status);
}

function navigateToOrderPage(currentOrder) {
    if (currentOrder && currentOrder.orderId) {
        window.location.href = "/Order/Orderpaige";
    } else {
        console.error("Order ID not found in local storage.");
    }
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

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
        case 0: return "Pending";
        case 1: return "Preparing";
        case 2: return "Ready";
        case 3: return "Served";
        default: return "Unknown";
    }
}

async function fetchCurrentOrderStatus(orderId) {
    try {
        const response = await fetch(`https://restosolutionssaas.com/api/OrderApi/GetOrderStatus/${orderId}`);
        if (!response.ok) {
            throw new Error('Failed to fetch current order status');
        }
        const data = await response.json();
        return mapEnumToStatusText(data.status);
    } catch (error) {
        console.error('Error fetching order status:', error);
    }
}

function updateUIWithCurrentStatus(status) {
    let statusText = getOrderStatusText(status);
    let color = getStatusColor(status);

    let orderStatusElement = document.getElementById("orderStatus");
    if (orderStatusElement) {
        orderStatusElement.innerHTML = `<i class="fas fa-truck mr-2"></i><span class="badge" style="background-color: ${color}; font-size: 1.25em;">${statusText}</span>`;
    }
}

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
        case 'Preparing': return "Your order is in progress.";
        case 'Ready': return "Your order is ready for pickup!";
        case 'Served': return "Your order has been served.";
        case 'Delivered': return "Your order has been delivered."; // Note: This status is not in mapEnumToStatusText
        default: return "Order status: " + status;
    }
}

// Global access function
window.checkAndUpdateOrderStatus = async function () {
    let currentOrder = JSON.parse(localStorage.getItem('currentOrder'));
    if (currentOrder) {
        const currentStatus = await fetchCurrentOrderStatus(currentOrder.orderId);
        if (currentStatus !== undefined) {
            updateUIWithCurrentStatus(currentStatus);
        }
    }
};

document.addEventListener("DOMContentLoaded", async function () {
    // Ensure SignalR setup and immediate status check are both handled
    await window.checkAndUpdateOrderStatus(); // Might be redundant if called immediately after an order is placed
    setupSignalRConnection(); // Initialize SignalR connection
});

async function setupSignalRConnection() {
    let currentOrder = JSON.parse(localStorage.getItem('currentOrder'));
    if (!currentOrder) return;

    console.log("Establishing SignalR connection for order status updates.");
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`https://restosolutionssaas.com/api/OrderApi/orderStatusHub?orderId=${currentOrder.orderId}`)
        .configureLogging(signalR.LogLevel.Information)
        .withAutomaticReconnect()
        .build();

    connection.on("NewOrderPlaced", order => console.log("SignalR: New order placed", order));
    connection.on("ReceiveOrderUpdate", updateOrderStatusUI);
    connection.onreconnecting(error => console.warn("SignalR connection lost, attempting to reconnect...", error));
    connection.onreconnected(connectionId => console.log("SignalR connection reestablished, connectionId:", connectionId));

    try {
        await connection.start();
        console.log("SignalR connection established successfully.");
    } catch (err) {
        console.error("SignalR connection error:", err);
    }

    document.addEventListener("click", event => {
        if (event.target.closest("#orderStatus")) {
            navigateToOrderPage(currentOrder);
        }
    });
}

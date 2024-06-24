/*window.makeorder = function (order) {
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
*/
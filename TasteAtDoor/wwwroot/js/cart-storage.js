window.TasteAtDoorCart = (function () {
    const CART_KEY = "tasteAtDoorCart";

    function getCart() {
        const json = localStorage.getItem(CART_KEY);
        if (!json) return [];
        try {
            return JSON.parse(json);
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        localStorage.setItem(CART_KEY, JSON.stringify(cart));
    }

    function clearCart() {
        localStorage.removeItem(CART_KEY);
    }

    function addItem(item) {
        const cart = getCart();
        const existing = cart.find(x => x.menuItemId === item.menuItemId);

        if (existing) {
            existing.quantity += 1;
        } else {
            cart.push({
                menuItemId: item.menuItemId,
                name: item.name,
                unitPrice: item.unitPrice,
                quantity: 1,
                imageContentType: item.imageContentType,
                imageBase64: item.imageBase64
            });
        }

        saveCart(cart);
    }

    function increase(menuItemId) {
        const cart = getCart();
        const item = cart.find(x => x.menuItemId === menuItemId);
        if (item) {
            item.quantity += 1;
            saveCart(cart);
        }
    }

    function decrease(menuItemId) {
        let cart = getCart();
        const item = cart.find(x => x.menuItemId === menuItemId);
        if (item) {
            item.quantity -= 1;
            if (item.quantity <= 0) {
                cart = cart.filter(x => x.menuItemId !== menuItemId);
            }
            saveCart(cart);
        }
    }

    function remove(menuItemId) {
        const cart = getCart().filter(x => x.menuItemId !== menuItemId);
        saveCart(cart);
    }

    function getTotal(cart) {
        return cart.reduce((sum, item) => sum + (item.unitPrice * item.quantity), 0);
    }

    function showMessage(messageElementId, text) {
        const box = document.getElementById(messageElementId);
        if (!box) return;

        box.textContent = text;
        box.style.display = "block";

        setTimeout(() => {
            box.style.display = "none";
        }, 2000);
    }

    function initMenuButtons(messageElementId) {
        const buttons = document.querySelectorAll(".add-to-cart-button");

        buttons.forEach(button => {
            button.addEventListener("click", function () {
                addItem({
                    menuItemId: parseInt(this.dataset.menuId),
                    name: this.dataset.menuName,
                    unitPrice: parseFloat(this.dataset.menuPrice),
                    imageContentType: this.dataset.imageContentType,
                    imageBase64: this.dataset.imageBase64
                });

                if (messageElementId) {
                    showMessage(messageElementId, "Item added to cart.");
                }
            });
        });
    }

    function renderCartPage(containerId, emptyId, summaryId, totalId) {
        const container = document.getElementById(containerId);
        const emptyBox = document.getElementById(emptyId);
        const summaryBox = document.getElementById(summaryId);
        const totalText = document.getElementById(totalId);

        if (!container || !emptyBox || !summaryBox || !totalText) return;

        const cart = getCart();
        container.innerHTML = "";

        if (cart.length === 0) {
            emptyBox.style.display = "block";
            summaryBox.style.display = "none";
            return;
        }

        emptyBox.style.display = "none";
        summaryBox.style.display = "block";

        cart.forEach(item => {
            const lineTotal = item.unitPrice * item.quantity;

            const card = document.createElement("div");
            card.className = "cart-card";
            card.innerHTML = `
                <img src="data:${item.imageContentType};base64,${item.imageBase64}" alt="${item.name}" class="cart-image" />
                <div class="cart-content">
                    <div class="cart-title-row">
                        <h3>${item.name}</h3>
                        <span class="menu-price">${lineTotal.toFixed(2)}</span>
                    </div>
                    <div class="cart-meta">Unit Price: ${item.unitPrice.toFixed(2)}</div>
                    <div class="cart-meta">Quantity: ${item.quantity}</div>
                    <div class="cart-actions">
                        <button class="mini-btn cart-increase">+</button>
                        <button class="mini-btn cart-decrease">-</button>
                        <button class="delete-link-button cart-remove">Remove</button>
                    </div>
                </div>
            `;

            card.querySelector(".cart-increase").addEventListener("click", function () {
                increase(item.menuItemId);
                renderCartPage(containerId, emptyId, summaryId, totalId);
            });

            card.querySelector(".cart-decrease").addEventListener("click", function () {
                decrease(item.menuItemId);
                renderCartPage(containerId, emptyId, summaryId, totalId);
            });

            card.querySelector(".cart-remove").addEventListener("click", function () {
                remove(item.menuItemId);
                renderCartPage(containerId, emptyId, summaryId, totalId);
            });

            container.appendChild(card);
        });

        totalText.textContent = "Total Price: " + getTotal(cart).toFixed(2);
    }

    function renderCheckoutPage(summaryId, emptyId, totalBoxId, totalId, hiddenInputId) {
        const summary = document.getElementById(summaryId);
        const emptyBox = document.getElementById(emptyId);
        const totalBox = document.getElementById(totalBoxId);
        const totalText = document.getElementById(totalId);
        const hiddenInput = document.getElementById(hiddenInputId);

        if (!summary || !emptyBox || !totalBox || !totalText || !hiddenInput) return;

        const cart = getCart();
        summary.innerHTML = "";

        hiddenInput.value = JSON.stringify(cart);

        if (cart.length === 0) {
            emptyBox.style.display = "block";
            totalBox.style.display = "none";
            return;
        }

        emptyBox.style.display = "none";
        totalBox.style.display = "block";

        cart.forEach(item => {
            const row = document.createElement("div");
            row.className = "checkout-summary-row";
            row.innerHTML = `
                <div>
                    <strong>${item.name}</strong>
                    <div class="cart-meta">Quantity: ${item.quantity}</div>
                    <div class="cart-meta">Unit Price: ${item.unitPrice.toFixed(2)}</div>
                </div>
                <div class="menu-price">${(item.unitPrice * item.quantity).toFixed(2)}</div>
            `;
            summary.appendChild(row);
        });

        totalText.textContent = getTotal(cart).toFixed(2);
    }

    return {
        getCart,
        clearCart,
        initMenuButtons,
        renderCartPage,
        renderCheckoutPage
    };
})();
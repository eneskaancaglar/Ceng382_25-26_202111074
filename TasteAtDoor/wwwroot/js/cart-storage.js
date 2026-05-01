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

    function makeSignature(menuItemId, selectedOptions) {
        const optionIds = selectedOptions
            .map(x => x.optionId)
            .sort((a, b) => a - b)
            .join("-");
        return `${menuItemId}|${optionIds}`;
    }

    function addItem(item) {
        const cart = getCart();
        const existing = cart.find(x => x.signature === item.signature);

        if (existing) {
            existing.quantity += item.quantity;
        } else {
            cart.push(item);
        }

        saveCart(cart);
    }

    function increase(menuItemId, signature) {
        const cart = getCart();
        const item = cart.find(x => x.menuItemId === menuItemId && x.signature === signature);

        if (item) {
            item.quantity += 1;
            saveCart(cart);
        }
    }

    function decrease(menuItemId, signature) {
        let cart = getCart();
        const item = cart.find(x => x.menuItemId === menuItemId && x.signature === signature);

        if (item) {
            item.quantity -= 1;

            if (item.quantity <= 0) {
                cart = cart.filter(x => !(x.menuItemId === menuItemId && x.signature === signature));
            }

            saveCart(cart);
        }
    }

    function remove(menuItemId, signature) {
        const cart = getCart().filter(x => !(x.menuItemId === menuItemId && x.signature === signature));
        saveCart(cart);
    }

    function getTotal(cart) {
        return cart.reduce((sum, item) => sum + (item.finalUnitPrice * item.quantity), 0);
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

    function escapeHtml(value) {
        return String(value)
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    }

    function buildSelectionFromInput(input, removableMode = false) {
        const optionName = removableMode
            ? "No " + input.dataset.optionDisplayName
            : input.dataset.optionDisplayName;

        return {
            groupId: parseInt(input.dataset.groupId),
            groupTitle: input.dataset.groupTitle,
            optionId: parseInt(input.dataset.optionId),
            optionName: optionName,
            priceChange: parseFloat(input.dataset.priceChange || "0")
        };
    }

    function gatherSelectedOptions(panel) {
        const groups = panel.querySelectorAll(".customization-group");
        const selected = [];

        for (const group of groups) {
            const groupType = group.dataset.groupType;
            const groupTitle = group.dataset.groupTitle;
            const isRequired = group.dataset.groupRequired === "true";

            if (groupType === "SingleSelect") {
                const checked = group.querySelector('input[type="radio"]:checked');

                if (!checked && isRequired) {
                    return {
                        isValid: false,
                        errorMessage: `Please choose an option for ${groupTitle}.`,
                        selectedOptions: []
                    };
                }

                if (checked) {
                    selected.push(buildSelectionFromInput(checked, false));
                }
            }
            else if (groupType === "MultiSelect") {
                const checkedItems = group.querySelectorAll('input[type="checkbox"]:checked');
                checkedItems.forEach(input => selected.push(buildSelectionFromInput(input, false)));
            }
            else if (groupType === "Removable") {
                const checkedItems = group.querySelectorAll('input[type="checkbox"]:checked');
                checkedItems.forEach(input => selected.push(buildSelectionFromInput(input, true)));
            }
        }

        return {
            isValid: true,
            errorMessage: "",
            selectedOptions: selected
        };
    }

    function initMenuButtons(messageElementId) {
        const previews = document.querySelectorAll(".menu-preview-clickable");

        previews.forEach(preview => {
            preview.addEventListener("click", function () {
                const panel = this.parentElement.querySelector(".customization-panel");
                if (!panel) return;

                panel.style.display = panel.style.display === "none" || panel.style.display === "" ? "block" : "none";
            });

            preview.addEventListener("keydown", function (event) {
                if (event.key === "Enter" || event.key === " ") {
                    event.preventDefault();
                    this.click();
                }
            });
        });

        document.querySelectorAll(".quantity-increase").forEach(button => {
            button.addEventListener("click", function () {
                const valueSpan = this.parentElement.querySelector(".quantity-value");
                let value = parseInt(valueSpan.textContent || "1");
                value += 1;
                valueSpan.textContent = value.toString();
            });
        });

        document.querySelectorAll(".quantity-decrease").forEach(button => {
            button.addEventListener("click", function () {
                const valueSpan = this.parentElement.querySelector(".quantity-value");
                let value = parseInt(valueSpan.textContent || "1");
                value = Math.max(1, value - 1);
                valueSpan.textContent = value.toString();
            });
        });

        document.querySelectorAll(".add-customized-to-cart").forEach(button => {
            button.addEventListener("click", function () {
                const panel = this.closest(".customization-panel");
                const errorBox = panel.querySelector(".panel-error");
                const quantity = parseInt(panel.querySelector(".quantity-value")?.textContent || "1");

                if (errorBox) {
                    errorBox.style.display = "none";
                    errorBox.textContent = "";
                }

                const selectionResult = gatherSelectedOptions(panel);

                if (!selectionResult.isValid) {
                    if (errorBox) {
                        errorBox.textContent = selectionResult.errorMessage;
                        errorBox.style.display = "block";
                    }
                    return;
                }

                const selectedOptions = selectionResult.selectedOptions;
                const baseUnitPrice = parseFloat(this.dataset.basePrice || "0");
                const customizationTotal = selectedOptions.reduce((sum, option) => sum + option.priceChange, 0);
                const finalUnitPrice = baseUnitPrice + customizationTotal;

                const item = {
                    signature: makeSignature(parseInt(this.dataset.menuId), selectedOptions),
                    menuItemId: parseInt(this.dataset.menuId),
                    name: this.dataset.menuName,
                    baseUnitPrice: baseUnitPrice,
                    finalUnitPrice: finalUnitPrice,
                    quantity: quantity,
                    imageContentType: this.dataset.imageContentType,
                    imageBase64: this.dataset.imageBase64,
                    selectedOptions: selectedOptions
                };

                addItem(item);

                if (messageElementId) {
                    showMessage(messageElementId, "Item added to cart.");
                }

                const quantitySpan = panel.querySelector(".quantity-value");
                if (quantitySpan) {
                    quantitySpan.textContent = "1";
                }
            });
        });
    }

    function renderSelectedOptionsHtml(item) {
        if (!item.selectedOptions || item.selectedOptions.length === 0) {
            return `<div class="cart-meta">No customization selected</div>`;
        }

        const list = item.selectedOptions.map(option => {
            const priceHtml = option.priceChange !== 0
                ? ` <span class="option-price-change">(${option.priceChange.toFixed(2)})</span>`
                : "";

            return `<li>${escapeHtml(option.groupTitle)}: ${escapeHtml(option.optionName)}${priceHtml}</li>`;
        }).join("");

        return `<ul class="user-option-list">${list}</ul>`;
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
            const lineTotal = item.finalUnitPrice * item.quantity;

            const card = document.createElement("div");
            card.className = "cart-card";
            card.innerHTML = `
                <img src="data:${item.imageContentType};base64,${item.imageBase64}" alt="${escapeHtml(item.name)}" class="cart-image" />
                <div class="cart-content">
                    <div class="cart-title-row">
                        <h3>${escapeHtml(item.name)}</h3>
                        <span class="menu-price">${lineTotal.toFixed(2)}</span>
                    </div>
                    <div class="cart-meta">Base Price: ${item.baseUnitPrice.toFixed(2)}</div>
                    <div class="cart-meta">Final Unit Price: ${item.finalUnitPrice.toFixed(2)}</div>
                    <div class="cart-meta">Quantity: ${item.quantity}</div>
                    ${renderSelectedOptionsHtml(item)}
                    <div class="cart-actions">
                        <button class="mini-btn cart-increase">+</button>
                        <button class="mini-btn cart-decrease">-</button>
                        <button class="delete-link-button cart-remove">Remove</button>
                    </div>
                </div>
            `;

            card.querySelector(".cart-increase").addEventListener("click", function () {
                increase(item.menuItemId, item.signature);
                renderCartPage(containerId, emptyId, summaryId, totalId);
            });

            card.querySelector(".cart-decrease").addEventListener("click", function () {
                decrease(item.menuItemId, item.signature);
                renderCartPage(containerId, emptyId, summaryId, totalId);
            });

            card.querySelector(".cart-remove").addEventListener("click", function () {
                remove(item.menuItemId, item.signature);
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
                    <strong>${escapeHtml(item.name)}</strong>
                    <div class="cart-meta">Quantity: ${item.quantity}</div>
                    <div class="cart-meta">Base Price: ${item.baseUnitPrice.toFixed(2)}</div>
                    <div class="cart-meta">Final Unit Price: ${item.finalUnitPrice.toFixed(2)}</div>
                    ${renderSelectedOptionsHtml(item)}
                </div>
                <div class="menu-price">${(item.finalUnitPrice * item.quantity).toFixed(2)}</div>
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
(function () {
    const STORAGE_KEY = "tasteAtDoorCart";

    function getCart() {
        const raw = localStorage.getItem(STORAGE_KEY);

        if (!raw) {
            return [];
        }

        try {
            const parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        localStorage.setItem(STORAGE_KEY, JSON.stringify(cart));
        syncCheckoutInputs();
        updateCartCount();
    }

    function clearCart() {
        localStorage.removeItem(STORAGE_KEY);
        syncCheckoutInputs();
        updateCartCount();
    }

    function parseNumber(value) {
        const parsed = Number.parseFloat(value);

        if (Number.isNaN(parsed)) {
            return 0;
        }

        return parsed;
    }

    function getSelectedOptions() {
        const selectedInputs = document.querySelectorAll(".js-customization-option:checked");

        return Array.from(selectedInputs).map(input => ({
            optionId: Number.parseInt(input.dataset.optionId || "0", 10),
            groupTitle: input.dataset.groupTitle || "",
            optionName: input.dataset.optionName || "",
            priceChange: parseNumber(input.dataset.priceChange || "0")
        })).filter(option => option.optionId > 0);
    }

    function getOptionKey(selectedOptions) {
        if (!selectedOptions || selectedOptions.length === 0) {
            return "no-options";
        }

        return selectedOptions
            .map(option => option.optionId)
            .sort((a, b) => a - b)
            .join("-");
    }

    function calculateFinalPrice(basePrice, selectedOptions) {
        const optionTotal = selectedOptions.reduce((sum, option) => {
            return sum + parseNumber(option.priceChange);
        }, 0);

        return basePrice + optionTotal;
    }

    function addItemFromButton(button) {
        const menuItemId = Number.parseInt(button.dataset.menuId || "0", 10);

        if (menuItemId <= 0) {
            showMessage("cart-client-message", "Menu item could not be added.", "danger");
            return;
        }

        const basePrice = parseNumber(button.dataset.price || "0");
        const selectedOptions = getSelectedOptions();
        const finalUnitPrice = calculateFinalPrice(basePrice, selectedOptions);
        const optionKey = getOptionKey(selectedOptions);

        const newItem = {
            menuItemId: menuItemId,
            name: button.dataset.name || "Menu Item",
            description: button.dataset.description || "",
            price: basePrice,
            finalUnitPrice: finalUnitPrice,
            imageContentType: button.dataset.imageContentType || "",
            imageBase64: button.dataset.imageBase64 || "",
            quantity: 1,
            selectedOptions: selectedOptions
        };

        const cart = getCart();

        const existing = cart.find(item =>
            item.menuItemId === newItem.menuItemId &&
            getOptionKey(item.selectedOptions || []) === optionKey
        );

        if (existing) {
            existing.quantity += 1;
        } else {
            cart.push(newItem);
        }

        saveCart(cart);

        showMessage(
            "cart-client-message",
            `${newItem.name} added to cart.`,
            "success"
        );
    }

    function initMenuButtons(messageTargetId) {
        document.querySelectorAll(".js-add-to-cart").forEach(button => {
            if (button.dataset.cartBound === "true") {
                return;
            }

            button.dataset.cartBound = "true";

            button.addEventListener("click", function () {
                addItemFromButton(button);
            });
        });

        updateCartCount();
        syncCheckoutInputs();

        if (messageTargetId) {
            const target = document.getElementById(messageTargetId);

            if (target && !target.dataset.ready) {
                target.dataset.ready = "true";
            }
        }
    }

    function initCustomizationPricePreview() {
        const priceTarget = document.getElementById("detail-total-price");

        if (!priceTarget) {
            return;
        }

        const basePrice = parseNumber(priceTarget.dataset.basePrice || "0");

        function refreshPrice() {
            const selectedOptions = getSelectedOptions();
            const finalPrice = calculateFinalPrice(basePrice, selectedOptions);

            priceTarget.textContent = new Intl.NumberFormat(undefined, {
                style: "currency",
                currency: "TRY"
            }).format(finalPrice);
        }

        document.querySelectorAll(".js-customization-option").forEach(input => {
            input.addEventListener("change", refreshPrice);
        });

        refreshPrice();
    }

    function syncCheckoutInputs() {
        const cart = getCart();
        const value = JSON.stringify(cart);

        document.querySelectorAll("input[name='CartJson'], #CartJson").forEach(input => {
            input.value = value;
        });
    }

    function updateCartCount() {
        const cart = getCart();
        const count = cart.reduce((sum, item) => sum + (item.quantity || 0), 0);

        document.querySelectorAll(".js-cart-count").forEach(element => {
            element.textContent = count.toString();
        });
    }

    function showMessage(targetId, message, type) {
        const target = document.getElementById(targetId);

        if (!target) {
            return;
        }

        const cssClass = type === "success"
            ? "alert alert-success"
            : "alert alert-danger";

        target.innerHTML = `<div class="${cssClass}">${message}</div>`;

        window.setTimeout(() => {
            target.innerHTML = "";
        }, 2500);
    }

    function initCheckoutForm() {
        syncCheckoutInputs();

        document.querySelectorAll("form").forEach(form => {
            form.addEventListener("submit", function () {
                syncCheckoutInputs();
            });
        });
    }

    window.TasteAtDoorCart = {
        getCart,
        saveCart,
        clearCart,
        initMenuButtons,
        initCustomizationPricePreview,
        initCheckoutForm,
        syncCheckoutInputs,
        updateCartCount
    };

    document.addEventListener("DOMContentLoaded", function () {
        updateCartCount();
        syncCheckoutInputs();
        initCheckoutForm();
    });
})();
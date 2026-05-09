(function () {
    const STORAGE_KEY = "tasteAtDoorCart";
    const DEFAULT_MIN_GUESTS = 1;
    const DEFAULT_MAX_GUESTS = 100000;

    function parseNumber(value, fallback = 0) {
        const parsed = Number.parseFloat(value);

        if (Number.isNaN(parsed)) {
            return fallback;
        }

        return parsed;
    }

    function parseInteger(value, fallback = 0) {
        const parsed = Number.parseInt(value, 10);

        if (Number.isNaN(parsed)) {
            return fallback;
        }

        return parsed;
    }

    function getMinMaxFromValues(minValue, maxValue) {
        const minGuestCount = Math.max(
            DEFAULT_MIN_GUESTS,
            parseInteger(minValue, DEFAULT_MIN_GUESTS)
        );

        let maxGuestCount = parseInteger(maxValue, DEFAULT_MAX_GUESTS);

        if (maxGuestCount < minGuestCount) {
            maxGuestCount = minGuestCount;
        }

        return {
            minGuestCount,
            maxGuestCount
        };
    }

    function clampQuantity(value, minGuestCount = DEFAULT_MIN_GUESTS, maxGuestCount = DEFAULT_MAX_GUESTS) {
        let quantity = parseInteger(value, minGuestCount);

        if (quantity < minGuestCount) {
            quantity = minGuestCount;
        }

        if (quantity > maxGuestCount) {
            quantity = maxGuestCount;
        }

        return quantity;
    }

    function getSelectedOptions() {
        const selectedInputs = document.querySelectorAll(".js-customization-option:checked");

        return Array.from(selectedInputs).map(input => ({
            optionId: parseInteger(input.dataset.optionId || "0"),
            groupTitle: input.dataset.groupTitle || "",
            optionName: input.dataset.optionName || "",
            priceChange: parseNumber(input.dataset.priceChange || "0")
        })).filter(option => option.optionId > 0);
    }

    function normalizeOption(raw) {
        return {
            optionId: parseInteger(raw?.optionId ?? raw?.OptionId),
            groupTitle: raw?.groupTitle ?? raw?.GroupTitle ?? "",
            optionName: raw?.optionName ?? raw?.OptionName ?? "",
            priceChange: parseNumber(raw?.priceChange ?? raw?.PriceChange)
        };
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

    function buildSignature(menuItemId, selectedOptions) {
        return `${menuItemId}|${getOptionKey(selectedOptions || [])}`;
    }

    function calculateFinalPrice(basePrice, selectedOptions) {
        const optionTotal = selectedOptions.reduce((sum, option) => {
            return sum + parseNumber(option.priceChange);
        }, 0);

        return basePrice + optionTotal;
    }

    function normalizeItem(raw) {
        const selectedOptionsRaw = raw?.selectedOptions ?? raw?.SelectedOptions ?? [];

        const selectedOptions = Array.isArray(selectedOptionsRaw)
            ? selectedOptionsRaw.map(normalizeOption)
            : [];

        const menuItemId = parseInteger(raw?.menuItemId ?? raw?.MenuItemId);

        const baseUnitPrice = parseNumber(
            raw?.baseUnitPrice ??
            raw?.BaseUnitPrice ??
            raw?.price ??
            raw?.Price ??
            raw?.unitPrice ??
            raw?.UnitPrice
        );

        const finalUnitPrice = parseNumber(
            raw?.finalUnitPrice ??
            raw?.FinalUnitPrice ??
            raw?.price ??
            raw?.Price ??
            raw?.unitPrice ??
            raw?.UnitPrice ??
            baseUnitPrice
        );

        const minMax = getMinMaxFromValues(
            raw?.minGuestCount ?? raw?.MinGuestCount,
            raw?.maxGuestCount ?? raw?.MaxGuestCount
        );

        const quantity = clampQuantity(
            raw?.quantity ?? raw?.Quantity ?? minMax.minGuestCount,
            minMax.minGuestCount,
            minMax.maxGuestCount
        );

        return {
            signature: raw?.signature ?? raw?.Signature ?? buildSignature(menuItemId, selectedOptions),
            menuItemId: menuItemId,
            name: raw?.name ?? raw?.Name ?? "Catering Package",
            description: raw?.description ?? raw?.Description ?? "",
            baseUnitPrice: baseUnitPrice,
            finalUnitPrice: finalUnitPrice,
            quantity: quantity,
            minGuestCount: minMax.minGuestCount,
            maxGuestCount: minMax.maxGuestCount,
            imageContentType: raw?.imageContentType ?? raw?.ImageContentType ?? "",
            imageBase64: raw?.imageBase64 ?? raw?.ImageBase64 ?? "",
            selectedOptions: selectedOptions
        };
    }

    function getCart() {
        const raw = localStorage.getItem(STORAGE_KEY);

        if (!raw) {
            return [];
        }

        try {
            const parsed = JSON.parse(raw);

            if (!Array.isArray(parsed)) {
                return [];
            }

            return parsed
                .map(normalizeItem)
                .filter(item => item.menuItemId > 0 && item.name);
        } catch {
            return [];
        }
    }

    function saveCart(cart) {
        const normalizedCart = Array.isArray(cart)
            ? cart.map(normalizeItem).filter(item => item.menuItemId > 0 && item.name)
            : [];

        localStorage.setItem(STORAGE_KEY, JSON.stringify(normalizedCart));
        syncCheckoutInputs();
        updateCartCount();
    }

    function clearCart() {
        localStorage.removeItem(STORAGE_KEY);
        syncCheckoutInputs();
        updateCartCount();
    }

    function formatMoney(value) {
        return new Intl.NumberFormat("tr-TR", {
            style: "currency",
            currency: "TRY"
        }).format(value);
    }

    function getPackageWrapperFromButton(button) {
        return button.closest("[data-package-card]") || document;
    }

    function getRequestedQuantity(button) {
        const wrapper = getPackageWrapperFromButton(button);

        const minMax = getMinMaxFromValues(
            button.dataset.minGuestCount,
            button.dataset.maxGuestCount
        );

        const input = wrapper.querySelector(".js-package-guest-count");

        if (!input) {
            return minMax.minGuestCount;
        }

        const quantity = clampQuantity(
            input.value,
            minMax.minGuestCount,
            minMax.maxGuestCount
        );

        input.value = quantity;

        return quantity;
    }

    function addItemFromButton(button) {
        const menuItemId = parseInteger(button.dataset.menuId || "0");

        if (menuItemId <= 0) {
            showMessage("cart-client-message", "Catering package could not be added.", "danger");
            return;
        }

        const minMax = getMinMaxFromValues(
            button.dataset.minGuestCount,
            button.dataset.maxGuestCount
        );

        const basePrice = parseNumber(button.dataset.price || "0");
        const selectedOptions = getSelectedOptions();
        const finalUnitPrice = calculateFinalPrice(basePrice, selectedOptions);
        const quantity = getRequestedQuantity(button);
        const signature = buildSignature(menuItemId, selectedOptions);

        const newItem = {
            signature: signature,
            menuItemId: menuItemId,
            name: button.dataset.name || "Catering Package",
            description: button.dataset.description || "",
            baseUnitPrice: basePrice,
            finalUnitPrice: finalUnitPrice,
            quantity: quantity,
            minGuestCount: minMax.minGuestCount,
            maxGuestCount: minMax.maxGuestCount,
            imageContentType: button.dataset.imageContentType || "",
            imageBase64: button.dataset.imageBase64 || "",
            selectedOptions: selectedOptions
        };

        const cart = getCart();

        const existing = cart.find(item => item.signature === newItem.signature);

        if (existing) {
            existing.quantity = quantity;
            existing.baseUnitPrice = basePrice;
            existing.finalUnitPrice = finalUnitPrice;
            existing.minGuestCount = minMax.minGuestCount;
            existing.maxGuestCount = minMax.maxGuestCount;
        } else {
            cart.push(newItem);
        }

        saveCart(cart);

        showMessage(
            "cart-client-message",
            `${newItem.name} added for ${quantity} guests.`,
            "success"
        );
    }

    function refreshPackageEstimate(wrapper) {
        if (!wrapper) {
            return;
        }

        const input = wrapper.querySelector(".js-package-guest-count");
        const totalTarget = wrapper.querySelector(".js-package-estimated-total");

        if (!input || !totalTarget) {
            return;
        }

        if (input.value === "") {
            return;
        }

        const minMax = getMinMaxFromValues(input.min, input.max);
        const quantity = clampQuantity(input.value, minMax.minGuestCount, minMax.maxGuestCount);
        const unitPrice = parseNumber(totalTarget.dataset.unitPrice || "0");

        totalTarget.textContent = formatMoney(unitPrice * quantity);
    }

    function initGuestCountControls(scope) {
        const root = scope || document;

        root.querySelectorAll("[data-package-card]").forEach(wrapper => {
            if (wrapper.dataset.guestControlsBound === "true") {
                refreshPackageEstimate(wrapper);
                return;
            }

            wrapper.dataset.guestControlsBound = "true";

            const input = wrapper.querySelector(".js-package-guest-count");

            wrapper.querySelectorAll(".js-guest-step").forEach(button => {
                button.addEventListener("click", function () {
                    if (!input) {
                        return;
                    }

                    const minMax = getMinMaxFromValues(input.min, input.max);
                    const current = clampQuantity(input.value, minMax.minGuestCount, minMax.maxGuestCount);
                    const step = parseInteger(button.dataset.step || "0");
                    const next = clampQuantity(current + step, minMax.minGuestCount, minMax.maxGuestCount);

                    input.value = next;
                    refreshPackageEstimate(wrapper);
                    refreshDetailTotalWithQuantity();
                });
            });

            if (input) {
                input.addEventListener("input", function () {
                    refreshPackageEstimate(wrapper);
                    refreshDetailTotalWithQuantity();
                });

                input.addEventListener("change", function () {
                    const minMax = getMinMaxFromValues(input.min, input.max);
                    input.value = clampQuantity(input.value, minMax.minGuestCount, minMax.maxGuestCount);
                    refreshPackageEstimate(wrapper);
                    refreshDetailTotalWithQuantity();
                });
            }

            refreshPackageEstimate(wrapper);
        });
    }

    function initMenuButtons(messageTargetId) {
        initGuestCountControls(document);

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

    function refreshDetailTotalWithQuantity() {
        const priceTarget = document.getElementById("detail-total-price");
        const totalTarget = document.getElementById("detail-package-total");
        const input = document.querySelector(".menu-details-page .js-package-guest-count");

        if (!priceTarget || !totalTarget || !input) {
            return;
        }

        const unitPrice = parseNumber(priceTarget.dataset.currentPrice || priceTarget.dataset.basePrice || "0");
        const minMax = getMinMaxFromValues(input.min, input.max);
        const quantity = clampQuantity(input.value, minMax.minGuestCount, minMax.maxGuestCount);

        totalTarget.textContent = formatMoney(unitPrice * quantity);
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

            priceTarget.dataset.currentPrice = finalPrice.toString();
            priceTarget.textContent = formatMoney(finalPrice);

            refreshDetailTotalWithQuantity();
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

        document.querySelectorAll(".js-cart-count").forEach(element => {
            element.textContent = cart.length.toString();
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
        initGuestCountControls,
        syncCheckoutInputs,
        updateCartCount,
        clampQuantity
    };

    document.addEventListener("DOMContentLoaded", function () {
        updateCartCount();
        syncCheckoutInputs();
        initCheckoutForm();
        initGuestCountControls(document);
    });
})();
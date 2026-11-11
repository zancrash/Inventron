// TODO: take the currency from tenant settings once they exist.
const currencyFormat = new Intl.NumberFormat("en-US", { style: "currency", currency: "CAD" });

// Quantities are decimals with up to 4 places on the API.
const quantityFormat = new Intl.NumberFormat("en-US", { maximumFractionDigits: 4 });

export function formatCurrency(value: number): string {
    return currencyFormat.format(value);
}

export function formatQuantity(value: number): string {
    return quantityFormat.format(value);
}

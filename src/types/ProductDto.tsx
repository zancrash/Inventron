export interface ProductDto {
    id: number;
    name: string;
    sku: string;
    description: string | null;
    cost: number;
    sellingPrice: number;
    quantity: number | null;
}
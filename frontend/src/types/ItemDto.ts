export const ItemType = {
    RawMaterial: 1,
    SubAssembly: 2,
    FinishedGood: 3,
    Purchased: 4,
} as const;

export type ItemType = (typeof ItemType)[keyof typeof ItemType];

export interface SupplierDto {
    id: number;
    company: string;
    email: string | null;
    phone: string | null;
    isActive: boolean;
}

export interface ItemDto {
    id: number;
    sku: string;
    name: string;
    description: string | null;
    type: ItemType;
    unitOfMeasure: string;
    standardCost: number;
    sellingPrice: number | null;
    leadTimeDays: number | null;
    safetyStock: number | null;
    minOrderQty: number | null;
    lotSize: number | null;
    preferredSupplierId: number | null;
    preferredSupplier: SupplierDto | null;
    isActive: boolean;
}

export interface PagedResult<T> {
    items: T[];
    total: number;
    page: number;
    pageSize: number;
}

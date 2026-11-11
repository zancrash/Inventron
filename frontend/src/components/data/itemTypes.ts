import { ItemType } from "../../types/ItemDto";

export const itemTypeOptions: { value: ItemType; label: string }[] = [
    { value: ItemType.RawMaterial, label: "Raw material" },
    { value: ItemType.SubAssembly, label: "Sub-assembly" },
    { value: ItemType.FinishedGood, label: "Finished good" },
    { value: ItemType.Purchased, label: "Purchased" },
];

export function itemTypeLabel(type: ItemType): string {
    return itemTypeOptions.find((option) => option.value === type)?.label ?? "Unknown";
}

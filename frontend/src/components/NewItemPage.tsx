import { useEffect, useRef, useState, type ChangeEvent, type FormEvent } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import {
    Alert,
    Autocomplete,
    Box,
    Breadcrumbs,
    Button,
    Divider,
    FormControlLabel,
    Grid,
    InputAdornment,
    Link,
    MenuItem,
    Paper,
    Stack,
    Switch,
    TextField,
    Typography,
} from "@mui/material";
import { ItemType, type SupplierDto } from "../types/ItemDto";
import { addItem, type CreateItemDto } from "../api/items";
import { ApiError } from "../api/client";

const itemTypeOptions: { value: ItemType; label: string }[] = [
    { value: ItemType.RawMaterial, label: "Raw material" },
    { value: ItemType.SubAssembly, label: "Sub-assembly" },
    { value: ItemType.FinishedGood, label: "Finished good" },
    { value: ItemType.Purchased, label: "Purchased" },
];

// Suggestions only; the API stores unit of measure as free text.
const unitOfMeasureOptions = ["Each", "Kg", "Lbs", "g", "m", "cm", "L", "mL", "Roll", "Box"];

// Numeric fields are kept as strings so an empty input stays empty instead of becoming 0.
interface NewItemFormValues {
    sku: string;
    name: string;
    description: string;
    type: ItemType;
    unitOfMeasure: string;
    standardCost: string;
    sellingPrice: string;
    leadTimeDays: string;
    safetyStock: string;
    minOrderQty: string;
    lotSize: string;
    preferredSupplier: SupplierDto | null;
    isActive: boolean;
}

type FormErrors = Partial<Record<keyof NewItemFormValues, string>>;

// Fields bound directly to a text input.
type TextFieldName = {
    [K in keyof NewItemFormValues]: NewItemFormValues[K] extends string ? K : never;
}[keyof NewItemFormValues];

const initialValues: NewItemFormValues = {
    sku: "",
    name: "",
    description: "",
    type: ItemType.Purchased,
    unitOfMeasure: "Each",
    standardCost: "",
    sellingPrice: "",
    leadTimeDays: "",
    safetyStock: "",
    minOrderQty: "",
    lotSize: "",
    preferredSupplier: null,
    isActive: true,
};

function isNonNegativeNumber(value: string) {
    const n = Number(value);
    return Number.isFinite(n) && n >= 0;
}

function validate(values: NewItemFormValues): FormErrors {
    const errors: FormErrors = {};

    if (!values.sku.trim()) errors.sku = "SKU is required";
    if (!values.name.trim()) errors.name = "Name is required";
    if (!values.unitOfMeasure.trim()) errors.unitOfMeasure = "Unit of measure is required";

    if (!values.standardCost.trim()) errors.standardCost = "Standard cost is required";
    else if (!isNonNegativeNumber(values.standardCost)) errors.standardCost = "Must be 0 or more";

    const optionalNumbers = ["sellingPrice", "safetyStock", "minOrderQty", "lotSize"] as const;
    for (const field of optionalNumbers) {
        if (values[field].trim() && !isNonNegativeNumber(values[field])) errors[field] = "Must be 0 or more";
    }

    if (values.leadTimeDays.trim()) {
        const n = Number(values.leadTimeDays);
        if (!Number.isInteger(n) || n < 0) errors.leadTimeDays = "Must be a whole number of days";
    }

    return errors;
}

function toOptionalNumber(value: string): number | null {
    return value.trim() ? Number(value) : null;
}

function toPayload(values: NewItemFormValues): CreateItemDto {
    return {
        sku: values.sku.trim().toUpperCase(),
        name: values.name.trim(),
        description: values.description.trim() || null,
        type: values.type,
        unitOfMeasure: values.unitOfMeasure.trim(),
        standardCost: Number(values.standardCost),
        sellingPrice: toOptionalNumber(values.sellingPrice),
        leadTimeDays: toOptionalNumber(values.leadTimeDays),
        safetyStock: toOptionalNumber(values.safetyStock),
        minOrderQty: toOptionalNumber(values.minOrderQty),
        lotSize: toOptionalNumber(values.lotSize),
        preferredSupplierId: values.preferredSupplier?.id ?? null,
        preferredSupplier: null,
        isActive: values.isActive,
    };
}

// Maps the API's property names (e.g. "Sku", "$.standardCost") to form fields.
const apiFieldToFormField: Record<string, keyof NewItemFormValues> = {
    sku: "sku",
    name: "name",
    description: "description",
    type: "type",
    unitofmeasure: "unitOfMeasure",
    standardcost: "standardCost",
    sellingprice: "sellingPrice",
    leadtimedays: "leadTimeDays",
    safetystock: "safetyStock",
    minorderqty: "minOrderQty",
    lotsize: "lotSize",
    preferredsupplierid: "preferredSupplier",
    isactive: "isActive",
};

interface SubmitErrors {
    fields: FormErrors;
    general: string | null;
}

function toSubmitErrors(error: unknown): SubmitErrors {
    if (!(error instanceof ApiError)) {
        return { fields: {}, general: "Couldn't reach the server. Check your connection and try again." };
    }

    // On create, the only unique constraint an item can hit is (TenantId, Sku).
    if (error.status === 409) {
        return { fields: { sku: "An item with this SKU already exists" }, general: null };
    }

    if ((error.status === 400 || error.status === 422) && error.problem?.errors) {
        const fields: FormErrors = {};
        const unmatched: string[] = [];
        for (const [key, messages] of Object.entries(error.problem.errors)) {
            const field = apiFieldToFormField[key.replace(/^\$\./, "").toLowerCase()];
            if (field) fields[field] = messages[0];
            else unmatched.push(...messages);
        }
        const general = unmatched.length > 0
            ? unmatched.join(" ")
            : Object.keys(fields).length === 0 ? (error.problem.title ?? error.message) : null;
        return { fields, general };
    }

    return { fields: {}, general: error.problem?.detail ?? error.problem?.title ?? "Something went wrong saving the item. Please try again." };
}

function SectionHeading({ title, subtitle }: { title: string; subtitle: string }) {
    return (
        <Box sx={{ mb: 2 }}>
            <Typography variant="h6" component="h2">{title}</Typography>
            <Typography variant="body2" color="text.secondary">{subtitle}</Typography>
        </Box>
    );
}

export default function NewItemPage() {
    const [values, setValues] = useState<NewItemFormValues>(initialValues);
    const [errors, setErrors] = useState<FormErrors>({});
    const [submitting, setSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);
    const navigate = useNavigate();

    // Cancel an in-flight save if the user leaves the page.
    const abortRef = useRef<AbortController | null>(null);
    useEffect(() => () => abortRef.current?.abort(), []);

    // TODO: load suppliers from the API once a suppliers endpoint exists.
    const suppliers: SupplierDto[] = [];

    const setField = <K extends keyof NewItemFormValues>(field: K, value: NewItemFormValues[K]) => {
        setValues((prev) => ({ ...prev, [field]: value }));
        // Clear a field's error as soon as the user edits it.
        if (errors[field]) setErrors((prev) => ({ ...prev, [field]: undefined }));
    };

    const handleText = (field: TextFieldName) =>
        (event: ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => setField(field, event.target.value);

    const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
        event.preventDefault();
        if (submitting) return;

        const validationErrors = validate(values);
        setErrors(validationErrors);
        setSubmitError(null);
        if (Object.keys(validationErrors).length > 0) return;

        const controller = new AbortController();
        abortRef.current = controller;
        setSubmitting(true);

        try {
            const created = await addItem(toPayload(values), controller.signal);
            navigate(`/inventory/${created.id}`);
        } catch (error) {
            if (controller.signal.aborted) return;
            const { fields, general } = toSubmitErrors(error);
            setErrors(fields);
            setSubmitError(general);
        } finally {
            if (!controller.signal.aborted) setSubmitting(false);
        }
    };

    const handleReset = () => {
        setValues(initialValues);
        setErrors({});
        setSubmitError(null);
    };

    const fieldProps = (field: keyof NewItemFormValues) => ({
        error: Boolean(errors[field]),
        helperText: errors[field],
        fullWidth: true,
    });

    const moneyAdornment = { startAdornment: <InputAdornment position="start">$</InputAdornment> };
    const unitAdornment = values.unitOfMeasure
        ? { endAdornment: <InputAdornment position="end">{values.unitOfMeasure}</InputAdornment> }
        : undefined;

    return (
        <Box sx={{ maxWidth: 960, mx: "auto" }}>
            <Breadcrumbs sx={{ mb: 1 }}>
                <Link component={RouterLink} to="/inventory" underline="hover" color="inherit">Inventory</Link>
                <Typography color="text.primary">New item</Typography>
            </Breadcrumbs>
            <Typography variant="h4" component="h1" sx={{ mb: 3 }}>New item</Typography>

            <Paper component="form" variant="outlined" noValidate onSubmit={handleSubmit} sx={{ p: { xs: 2, sm: 3 } }}>
                <SectionHeading title="Details" subtitle="What the item is and how it's counted." />
                <Grid container spacing={2}>
                    <Grid size={{ xs: 12, sm: 4 }}>
                        <TextField
                            label="SKU"
                            required
                            autoFocus
                            value={values.sku}
                            onChange={handleText("sku")}
                            {...fieldProps("sku")}
                            helperText={errors.sku ?? "Saved in upper case"}
                            slotProps={{ htmlInput: { style: { textTransform: "uppercase" } } }}
                        />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 8 }}>
                        <TextField label="Name" required value={values.name} onChange={handleText("name")} {...fieldProps("name")} />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            select
                            label="Type"
                            required
                            value={values.type}
                            onChange={(e) => setField("type", Number(e.target.value) as ItemType)}
                            {...fieldProps("type")}
                        >
                            {itemTypeOptions.map((option) => (
                                <MenuItem key={option.value} value={option.value}>{option.label}</MenuItem>
                            ))}
                        </TextField>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <Autocomplete
                            freeSolo
                            options={unitOfMeasureOptions}
                            inputValue={values.unitOfMeasure}
                            onInputChange={(_, value) => setField("unitOfMeasure", value)}
                            renderInput={(params) => (
                                <TextField {...params} label="Unit of measure" required {...fieldProps("unitOfMeasure")} />
                            )}
                        />
                    </Grid>
                    <Grid size={12}>
                        <TextField
                            label="Description"
                            multiline
                            minRows={2}
                            value={values.description}
                            onChange={handleText("description")}
                            {...fieldProps("description")}
                        />
                    </Grid>
                </Grid>

                <Divider sx={{ my: 3 }} />

                <SectionHeading title="Pricing" subtitle="Cost per unit of measure." />
                <Grid container spacing={2}>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Standard cost"
                            required
                            type="number"
                            value={values.standardCost}
                            onChange={handleText("standardCost")}
                            {...fieldProps("standardCost")}
                            slotProps={{ input: moneyAdornment, htmlInput: { min: 0, step: "0.01", inputMode: "decimal" } }}
                        />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Selling price"
                            type="number"
                            value={values.sellingPrice}
                            onChange={handleText("sellingPrice")}
                            {...fieldProps("sellingPrice")}
                            helperText={errors.sellingPrice ?? "Leave blank if the item isn't sold"}
                            slotProps={{ input: moneyAdornment, htmlInput: { min: 0, step: "0.01", inputMode: "decimal" } }}
                        />
                    </Grid>
                </Grid>

                <Divider sx={{ my: 3 }} />

                <SectionHeading title="Planning" subtitle="Used by MRP to work out what to buy or make, and when." />
                <Grid container spacing={2}>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Lead time"
                            type="number"
                            value={values.leadTimeDays}
                            onChange={handleText("leadTimeDays")}
                            {...fieldProps("leadTimeDays")}
                            slotProps={{
                                input: { endAdornment: <InputAdornment position="end">days</InputAdornment> },
                                htmlInput: { min: 0, step: 1, inputMode: "numeric" },
                            }}
                        />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Safety stock"
                            type="number"
                            value={values.safetyStock}
                            onChange={handleText("safetyStock")}
                            {...fieldProps("safetyStock")}
                            slotProps={{ input: unitAdornment, htmlInput: { min: 0, step: "any", inputMode: "decimal" } }}
                        />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Minimum order quantity"
                            type="number"
                            value={values.minOrderQty}
                            onChange={handleText("minOrderQty")}
                            {...fieldProps("minOrderQty")}
                            slotProps={{ input: unitAdornment, htmlInput: { min: 0, step: "any", inputMode: "decimal" } }}
                        />
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <TextField
                            label="Lot size"
                            type="number"
                            value={values.lotSize}
                            onChange={handleText("lotSize")}
                            {...fieldProps("lotSize")}
                            helperText={errors.lotSize ?? "Order in multiples of this. 0 or 1 orders exactly what's needed."}
                            slotProps={{ input: unitAdornment, htmlInput: { min: 0, step: "any", inputMode: "decimal" } }}
                        />
                    </Grid>
                    <Grid size={12}>
                        <Autocomplete
                            options={suppliers}
                            value={values.preferredSupplier}
                            onChange={(_, supplier) => setField("preferredSupplier", supplier)}
                            getOptionLabel={(supplier) => supplier.company}
                            isOptionEqualToValue={(a, b) => a.id === b.id}
                            noOptionsText="No suppliers yet"
                            renderInput={(params) => (
                                <TextField {...params} label="Preferred supplier" {...fieldProps("preferredSupplier")} />
                            )}
                        />
                    </Grid>
                    <Grid size={12}>
                        <FormControlLabel
                            control={<Switch checked={values.isActive} onChange={(e) => setField("isActive", e.target.checked)} />}
                            label="Active"
                        />
                    </Grid>
                </Grid>

                <Divider sx={{ my: 3 }} />

                {submitError && (
                    <Alert severity="error" onClose={() => setSubmitError(null)} sx={{ mb: 2 }}>
                        {submitError}
                    </Alert>
                )}

                <Stack direction={{ xs: "column-reverse", sm: "row" }} spacing={1} sx={{ justifyContent: "flex-end" }}>
                    <Button component={RouterLink} to="/inventory" color="inherit">Cancel</Button>
                    <Button variant="outlined" onClick={handleReset} disabled={submitting}>Reset</Button>
                    <Button type="submit" variant="contained" loading={submitting}>Create item</Button>
                </Stack>
            </Paper>
        </Box>
    );
}

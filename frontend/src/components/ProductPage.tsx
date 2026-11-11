import { useEffect, useState, type ReactNode } from "react";
import { Link as RouterLink, useParams } from "react-router-dom";
import {
    Alert,
    Box,
    Breadcrumbs,
    Button,
    Chip,
    CircularProgress,
    Divider,
    Grid,
    Link,
    Paper,
    Stack,
    Typography,
} from "@mui/material";
import ArrowBackIcon from "@mui/icons-material/ArrowBack";
import type { ItemDto } from "../types/ItemDto";
import { getItemById } from "../api/items";
import { ApiError } from "../api/client";
import { itemTypeLabel } from "./data/itemTypes";
import { formatCurrency, formatQuantity } from "../utils/format";

// Stores which id a result belongs to, so "loading" is derived (result is for a different id)
// instead of being reset inside the effect.
type LoadResult =
    | { id: string; status: "loaded"; item: ItemDto }
    | { id: string; status: "notFound" }
    | { id: string; status: "error"; message: string };

const empty = <Typography component="span" color="text.secondary">—</Typography>;

function Field({ label, children }: { label: string; children: ReactNode }) {
    return (
        <Box>
            <Typography variant="body2" color="text.secondary">{label}</Typography>
            <Typography component="div">{children}</Typography>
        </Box>
    );
}

function Section({ title, children }: { title: string; children: ReactNode }) {
    return (
        <Paper variant="outlined" sx={{ p: { xs: 2, sm: 3 } }}>
            <Typography variant="h6" component="h2" sx={{ mb: 2 }}>{title}</Typography>
            <Grid container spacing={2}>{children}</Grid>
        </Paper>
    );
}

function BackButton() {
    return (
        <Button component={RouterLink} to="/inventory" startIcon={<ArrowBackIcon />} color="inherit">
            Back to inventory
        </Button>
    );
}

export default function ProductPage() {
    const { id = "" } = useParams<{ id: string }>();
    const [result, setResult] = useState<LoadResult | null>(null);

    useEffect(() => {
        const itemId = Number(id);
        if (!Number.isInteger(itemId) || itemId <= 0) return;

        const controller = new AbortController();
        getItemById(itemId, controller.signal)
            .then((item) => setResult({ id, status: "loaded", item }))
            .catch((error) => {
                if (controller.signal.aborted) return;
                if (error instanceof ApiError && error.status === 404) {
                    setResult({ id, status: "notFound" });
                } else {
                    setResult({ id, status: "error", message: error instanceof Error ? error.message : "Couldn't load the item." });
                }
            });

        return () => controller.abort();
    }, [id]);

    const validId = Number.isInteger(Number(id)) && Number(id) > 0;
    const current = result?.id === id ? result : null;

    if (validId && !current) return <CircularProgress />;

    if (current?.status === "error") {
        return (
            <Stack spacing={2} sx={{ alignItems: "flex-start" }}>
                <Alert severity="error">{current.message}</Alert>
                <BackButton />
            </Stack>
        );
    }

    if (!validId || current?.status === "notFound") {
        return (
            <Stack spacing={2} sx={{ alignItems: "flex-start" }}>
                <Typography variant="h5" component="h1">Item not found</Typography>
                <Typography color="text.secondary">It may have been deleted, or the link is wrong.</Typography>
                <BackButton />
            </Stack>
        );
    }

    const item = current!.item;
    const quantity = (value: number | null) =>
        value === null ? empty : `${formatQuantity(value)} ${item.unitOfMeasure}`;
    const lotSize = item.lotSize === null || item.lotSize <= 1
        ? "Lot-for-lot"
        : `Multiples of ${formatQuantity(item.lotSize)} ${item.unitOfMeasure}`;
    // TODO: GET /api/items/{id} doesn't include the supplier yet, so only the id is available.
    const supplier = item.preferredSupplier?.company
        ?? (item.preferredSupplierId === null ? empty : `Supplier #${item.preferredSupplierId}`);

    return (
        <Box sx={{ maxWidth: 960, mx: "auto" }}>
            <Breadcrumbs sx={{ mb: 1 }}>
                <Link component={RouterLink} to="/inventory" underline="hover" color="inherit">Inventory</Link>
                <Typography color="text.primary">{item.sku}</Typography>
            </Breadcrumbs>

            <Stack
                direction={{ xs: "column", sm: "row" }}
                spacing={2}
                sx={{ mb: 3, justifyContent: "space-between", alignItems: { sm: "flex-start" } }}
            >
                <Box>
                    <Typography variant="h4" component="h1">{item.name}</Typography>
                    <Typography color="text.secondary" sx={{ fontFamily: "monospace" }}>{item.sku}</Typography>
                </Box>
                <Stack direction="row" spacing={1}>
                    <Chip label={itemTypeLabel(item.type)} variant="outlined" />
                    <Chip label={item.isActive ? "Active" : "Archived"} color={item.isActive ? "success" : "default"} />
                </Stack>
            </Stack>

            <Stack spacing={2}>
                <Section title="Details">
                    <Grid size={{ xs: 12, sm: 4 }}>
                        <Field label="SKU">{item.sku}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 4 }}>
                        <Field label="Type">{itemTypeLabel(item.type)}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 4 }}>
                        <Field label="Unit of measure">{item.unitOfMeasure}</Field>
                    </Grid>
                    <Grid size={12}>
                        <Field label="Description">
                            {item.description
                                ? <Box component="span" sx={{ whiteSpace: "pre-line" }}>{item.description}</Box>
                                : empty}
                        </Field>
                    </Grid>
                </Section>

                <Section title="Pricing">
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <Field label="Standard cost">{formatCurrency(item.standardCost)} / {item.unitOfMeasure}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6 }}>
                        <Field label="Selling price">
                            {item.sellingPrice === null ? empty : `${formatCurrency(item.sellingPrice)} / ${item.unitOfMeasure}`}
                        </Field>
                    </Grid>
                </Section>

                <Section title="Planning">
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Field label="Lead time">
                            {item.leadTimeDays === null ? empty : `${item.leadTimeDays} ${item.leadTimeDays === 1 ? "day" : "days"}`}
                        </Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Field label="Safety stock">{quantity(item.safetyStock)}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Field label="Minimum order quantity">{quantity(item.minOrderQty)}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, sm: 6, md: 4 }}>
                        <Field label="Lot size">{lotSize}</Field>
                    </Grid>
                    <Grid size={{ xs: 12, md: 8 }}>
                        <Field label="Preferred supplier">{supplier}</Field>
                    </Grid>
                </Section>
            </Stack>

            <Divider sx={{ my: 3 }} />
            <BackButton />
        </Box>
    );
}

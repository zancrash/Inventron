import { useEffect, useState} from "react"
import type { ProductDto } from "../types/ProductDto";
import { getProducts } from "../api/products";
import { Alert, CircularProgress, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from "@mui/material";
import { Link } from "react-router";

export default function Inventory()
{
    const [products, setProducts] = useState<ProductDto[]>([]);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const currency = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'CAD',
    });

    useEffect(() => {
        const controller = new AbortController();
        
        getProducts(controller.signal)
            .then(setProducts)
            .catch((err) => {
                if (err.name !== 'AbortError') {
                    setError(err.message);
                }
            })
            .finally(() => {
                if(!controller.signal.aborted) {
                    setLoading(false);
                }
            });
        
            return () => {controller.abort()}
    }, []);

    if (loading) return <CircularProgress/>;
    if (error) return <Alert severity="error">Error: {error}</Alert>;

    return(
        <>
        <TableContainer component={Paper}>
            <Table size="small">
            <TableHead>
                <TableRow>
                <TableCell>SKU</TableCell>
                <TableCell>Name</TableCell>
                <TableCell>Description</TableCell>
                <TableCell align="right">Cost</TableCell>
                <TableCell align="right">Selling price</TableCell>
                <TableCell align="right">Quantity</TableCell>
                </TableRow>
            </TableHead>
            <TableBody>
                {products.map((p) => (
                <TableRow key={p.id} hover>
                    <TableCell>{p.sku}</TableCell>
                    <TableCell><Link to={`/inventory/${p.id}`}>{p.name}</Link></TableCell>
                    <TableCell>{p.description ?? '—'}</TableCell>
                    <TableCell align="right">{currency.format(p.cost)}</TableCell>
                    <TableCell align="right">{currency.format(p.sellingPrice)}</TableCell>
                    <TableCell align="right">{p.quantity ?? '—'}</TableCell>
                </TableRow>
                ))}
            </TableBody>
            </Table>
        </TableContainer>
        </>
    );
}
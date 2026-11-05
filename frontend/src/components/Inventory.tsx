import { useEffect, useState} from "react"
import type { ItemDto } from "../types/ItemDto";
import { getItems } from "../api/items";
import { Alert, CircularProgress, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TablePagination, TableRow } from "@mui/material";
import { Link } from "react-router";

export default function Inventory()
{
    const [items, setItems] = useState<ItemDto[]>([]);
    const [total, setTotal] = useState<number>(0);
    // TablePagination counts pages from 0; the API counts from 1.
    const [page, setPage] = useState<number>(0);
    const [pageSize, setPageSize] = useState<number>(50);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    const currency = new Intl.NumberFormat('en-US', {
        style: 'currency',
        currency: 'CAD',
    });

    useEffect(() => {
        const controller = new AbortController();
        setLoading(true);
        setError(null);

        getItems({ page: page + 1, pageSize }, controller.signal)
            .then((result) => {
                setItems(result.items);
                setTotal(result.total);
            })
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
    }, [page, pageSize]);

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
                </TableRow>
            </TableHead>
            <TableBody>
                {items.map((item) => (
                <TableRow key={item.id} hover>
                    <TableCell>{item.sku}</TableCell>
                    <TableCell><Link to={`/inventory/${item.id}`}>{item.name}</Link></TableCell>
                    <TableCell>{item.description ?? '—'}</TableCell>
                    <TableCell align="right">{currency.format(item.standardCost)}</TableCell>
                    <TableCell align="right">{item.sellingPrice === null ? '—' : currency.format(item.sellingPrice)}</TableCell>
                </TableRow>
                ))}
            </TableBody>
            </Table>
        </TableContainer>
        <TablePagination
            component="div"
            count={total}
            page={page}
            rowsPerPage={pageSize}
            rowsPerPageOptions={[25, 50, 100, 200]}
            onPageChange={(_, newPage) => setPage(newPage)}
            onRowsPerPageChange={(event) => {
                setPageSize(parseInt(event.target.value, 10));
                setPage(0);
            }}
        />
        </>
    );
}

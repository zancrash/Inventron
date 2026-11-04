import { useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import type { ProductDto } from "../types/ProductDto";
import { getProductById } from "../api/products";
import { Alert, Button, CircularProgress } from "@mui/material";

export default function ProductPage() {

    const {id} = useParams<{id: string}>();
    const [product, setProduct] = useState<ProductDto | null>(null);
    const [loading, setLoading] = useState<boolean>(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const controller = new AbortController();
        setLoading(true);
        setError(null);

        if (id) {
            getProductById(parseInt(id), controller.signal)
                .then((product) => {
                    setProduct(product);
                    setLoading(false);
                })
                .catch((error) => {
                    setError(error.message);
                    setLoading(false);
                });
        }
    }, [id]);

    if (loading) return <CircularProgress/>;
    if (error) return <Alert severity="error">{error}</Alert>;
    if (!product) return (
        <div>
            <h2>Product not found</h2>
            <Button href="/inventory">Return to Inventory</Button>
        </div>
    );

    return (
        <div>
            <h2>{product?.name}</h2>
            <p>{product?.description}</p>
            <p>Price: ${product?.cost.toFixed(2)}</p>
            <Button href="/inventory">Return to Inventory</Button>
        </div>
    );
}
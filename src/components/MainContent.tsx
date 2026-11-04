import { Box, Toolbar } from "@mui/material";
import { Outlet } from "react-router-dom";
import Footer from "./Footer";

export default function MainContent({ sidebarWidth }: { sidebarWidth: number })
{
    return (
        <Box
            component="main"
            sx={{
                flexGrow: 1,
                p: 3,
                width: { sm: `calc(100% - ${sidebarWidth}px)` },
                minHeight: "100vh",
                display: "flex",
                flexDirection: "column",
            }}
        >
        <Toolbar /> {/* spacer so content starts below the fixed AppBar */}
        <Box sx={{ flexGrow: 1 }}>
            <Outlet />
        </Box>
        <Footer />
        </Box>
    );

}
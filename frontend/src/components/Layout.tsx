import Header from "./Header";
import Sidebar from "./Sidebar";
import { Box } from "@mui/material";
import { useState } from "react";
import MainContent from "./MainContent";


export default function Layout()
{
    const [mobileOpen, setMobileOpen] = useState(false);
    const [isClosing, setIsClosing] = useState(false);

    const handleDrawerClose = () => {
        setIsClosing(true);
        setMobileOpen(false);
    }

    const handleDrawerTransitionEnd = () => setIsClosing(false);

    const handleDrawerToggle = () => {
        if (!isClosing) setMobileOpen(!mobileOpen);
    }

    const sidebarWidth = 300;
    return(
        <Box sx={{ display: 'flex' }}>
            <Header sidebarWidth={sidebarWidth} onMenuClick={handleDrawerToggle} />
            <Sidebar 
                sidebarWidth= {sidebarWidth} 
                mobileOpen={mobileOpen}
                onDrawerClose={handleDrawerClose}
                onDrawerTransitionEnd={handleDrawerTransitionEnd}
            />
            <MainContent sidebarWidth={sidebarWidth} />
        </Box>
    );
}
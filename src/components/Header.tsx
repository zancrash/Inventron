import { AppBar, createTheme, IconButton, ThemeProvider, Toolbar, Typography } from "@mui/material";
import MenuIcon from "@mui/icons-material/Menu";
import { matchPath, useLocation } from "react-router-dom";
import { navItems } from "./data/navItems";

interface HeaderProps {
    sidebarWidth: number;
    onMenuClick: () => void;
}

const darkTheme = createTheme({
        palette: {mode: 'dark'}
    })

export default function Header({ sidebarWidth, onMenuClick }: HeaderProps) 
{
    const { pathname } = useLocation();

    const title = navItems.find((i) => matchPath(i.path, pathname))?.label ?? "Inventron";

    return (
        <ThemeProvider theme={darkTheme}>
        <AppBar
            position="fixed"
            sx={{
                width: { sm: `calc(100% - ${sidebarWidth}px)` },
                ml: { sm: `${sidebarWidth}px` },
            }}
        >
        <Toolbar>
            <IconButton
                color="inherit"
                aria-label="open drawer"
                edge="start"
                onClick={onMenuClick}
                sx={{ mr: 2, display: { sm: "none" } }}
            >
                <MenuIcon />
            </IconButton>
            <Typography variant="h6" noWrap component="div">
                {title}
            </Typography>
        </Toolbar>
        </AppBar>
        </ThemeProvider>
    )
}
import { Box, createTheme, ThemeProvider } from '@mui/material';
import Divider from '@mui/material/Divider';
import Drawer from '@mui/material/Drawer';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemButton from '@mui/material/ListItemButton';
import ListItemText from '@mui/material/ListItemText';
import Toolbar from '@mui/material/Toolbar';
import { Link } from 'react-router-dom';
import { navItems } from './data/navItems';

const darkTheme = createTheme({
        palette: {mode: 'dark'}
    })

interface sidebarProps {
    sidebarWidth: number;
    mobileOpen: boolean;
    onDrawerClose: () => void;
    onDrawerTransitionEnd: () => void;
}

export default function Sidebar({sidebarWidth, mobileOpen, onDrawerClose, onDrawerTransitionEnd}: sidebarProps)
{
    const renderDrawer = (onNavigate?: () => void) => (
    <div>
      <Toolbar>
        <h3>Inventron</h3>
      </Toolbar>
      <Divider />
      <List>
        {navItems.map((navItem) => (
          <ListItem key={navItem.label} disablePadding>
            <ListItemButton component={Link} to={navItem.path}>
              <ListItemText primary={navItem.label} />
            </ListItemButton>
          </ListItem>
        ))}
      </List>
    </div>
  );

  return(
    <ThemeProvider theme={darkTheme}>
        <Box
          component="nav"
          sx={{ width: { sm: sidebarWidth }, flexShrink: { sm: 0 } }}
          aria-label="main navigation"
        >
          <Drawer
            variant="temporary"
            open={mobileOpen}
            onTransitionEnd={onDrawerTransitionEnd}
            onClose={onDrawerClose}
            sx={{ display: { xs: 'block', sm: 'none' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: sidebarWidth } }}
            slotProps={{ root: { keepMounted: true } }}
          >
            {renderDrawer(onDrawerClose)}
          </Drawer>
          <Drawer
            variant="permanent"
            open
            sx={{
            display: { xs: 'none', sm: 'block' },
            '& .MuiDrawer-paper': { boxSizing: 'border-box', width: sidebarWidth },
          }}
          >
            {renderDrawer()}
          </Drawer>
        </Box>
    </ThemeProvider>
  );
}
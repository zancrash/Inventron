import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { ThemeProvider, CssBaseline, createTheme } from '@mui/material'
import './index.css'
import { createBrowserRouter, RouterProvider } from 'react-router-dom'
import Inventory from './components/Inventory.tsx'
import Layout from './components/Layout.tsx'
import Dashboard from './components/Dashboard.tsx'
import ProductPage from './components/ProductPage.tsx'

const router = createBrowserRouter([
  {path: "/", element: <Layout/>, children: [
      {path: "/", element: <Dashboard/>},
      {path: "/inventory", element: <Inventory/>},
      {path: "/inventory/:id", element: <ProductPage/>},
      {path: "*", element: <h2>404: Page not found</h2>},
    ]
  },
]);

const lightTheme = createTheme({
    palette: { mode: "light"}
  })

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router}/>
    <ThemeProvider theme={lightTheme}>
      <CssBaseline/>
    </ThemeProvider>
  </StrictMode>,
)

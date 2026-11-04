import { createTheme, ThemeProvider } from '@mui/material';
import './App.css'

function App() {

  const lightTheme = createTheme({
    palette: { mode: "light"}
  })

  return (
    <>
    <ThemeProvider theme={lightTheme}>
    </ThemeProvider>
    </>
  )
}

export default App

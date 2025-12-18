import { createTheme, ThemeOptions } from '@mui/material/styles';

export const getAppTheme = (mode: 'light' | 'dark') => {
  const themeOptions: ThemeOptions = {
    palette: {
      mode,
      primary: {
        main: '#1976d2',
      },
      secondary: {
        main: '#dc004e',
      },
      error: {
        main: '#d32f2f',
      },
      success: {
        main: '#4CAF50',
        dark: '#45a049',
      },
      ...(mode === 'dark'
        ? {
            background: {
              default: '#121212',
              paper: '#1e1e1e',
            },
          }
        : {
            background: {
              default: '#ffffff',
              paper: '#ffffff',
            },
          }),
    },
    typography: {
      fontFamily: [
        '-apple-system',
        'BlinkMacSystemFont',
        '"Segoe UI"',
        'Roboto',
        '"Helvetica Neue"',
        'Arial',
        'sans-serif',
      ].join(','),
    },
    components: {
      MuiChip: {
        styleOverrides: {
          colorSuccess: {
            backgroundColor: '#4CAF50',
            color: 'white',
          },
        },
      },
    },
  };

  return createTheme(themeOptions);
};


import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider, CssBaseline } from '@mui/material';
import { AppRouter } from '../router';
import { useAuthStore } from '@/features/auth/store/authStore';
import { useEffect } from 'react';
import { ColorModeProvider, useColorMode } from '@/shared/theme/ColorModeContext';
import { getAppTheme } from '@/shared/theme/theme';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      retry: 1,
    },
  },
});

const AppWithTheme = () => {
  const { mode } = useColorMode();
  const initialize = useAuthStore((state) => state.initialize);
  const theme = getAppTheme(mode);

  useEffect(() => {
    initialize();
  }, [initialize]);

  return (
    <ThemeProvider theme={theme}>
      <CssBaseline />
      <AppRouter />
    </ThemeProvider>
  );
};

export const AppProviders = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <ColorModeProvider>
        <AppWithTheme />
      </ColorModeProvider>
    </QueryClientProvider>
  );
};


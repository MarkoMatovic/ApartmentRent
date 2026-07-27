import React from 'react';
import { Box, CircularProgress } from '@mui/material';

/**
 * Suspense fallback for lazily-loaded route components. Sized to roughly match
 * the main content area so the header/footer don't jump while a chunk loads.
 */
const PageLoader: React.FC = () => (
  <Box
    sx={{
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      minHeight: '60vh',
    }}
  >
    <CircularProgress />
  </Box>
);

export default PageLoader;

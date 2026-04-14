import React, { Component, ErrorInfo, ReactNode } from 'react';
import { Container, Typography, Button, Paper, Box } from '@mui/material';
import ErrorOutlineIcon from '@mui/icons-material/ErrorOutline';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    // In production this would go to a logging service (Sentry, etc.)
    if (process.env.NODE_ENV === 'development') {
      console.error('ErrorBoundary caught:', error, info.componentStack);
    }
  }

  handleReload = () => {
    this.setState({ hasError: false, error: null });
    window.location.reload();
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <Container maxWidth="sm" sx={{ py: 8 }}>
          <Paper sx={{ p: 4, textAlign: 'center' }}>
            <Box sx={{ mb: 2 }}>
              <ErrorOutlineIcon sx={{ fontSize: 64, color: 'error.main' }} />
            </Box>
            <Typography variant="h5" gutterBottom>
              Nešto je pošlo naopako
            </Typography>
            <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
              {process.env.NODE_ENV === 'development' && this.state.error?.message}
            </Typography>
            <Button variant="contained" color="secondary" onClick={this.handleReload}>
              Osvježi stranicu
            </Button>
          </Paper>
        </Container>
      );
    }

    return this.props.children;
  }
}

export default ErrorBoundary;

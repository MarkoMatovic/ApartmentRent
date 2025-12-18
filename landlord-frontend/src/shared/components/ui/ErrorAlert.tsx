import { Alert, AlertTitle, Box, Button } from '@mui/material';

interface ErrorAlertProps {
  message: string;
  onRetry?: () => void;
  title?: string;
}

export const ErrorAlert = ({ message, onRetry, title = 'Error' }: ErrorAlertProps) => {
  return (
    <Box p={3}>
      <Alert severity="error">
        <AlertTitle>{title}</AlertTitle>
        {message}
        {onRetry && (
          <Box mt={2}>
            <Button variant="contained" color="error" onClick={onRetry}>
              Retry
            </Button>
          </Box>
        )}
      </Alert>
    </Box>
  );
};


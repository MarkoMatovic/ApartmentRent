import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  Avatar,
  Stack,
  Chip,
  Rating,
  useTheme,
} from '@mui/material';
import {
  Verified,
  Star,
  CalendarToday,
} from '@mui/icons-material';
import { GetApartmentDto } from '../../types';
import { format } from 'date-fns';
import { useAuthStore } from '@/features/auth/store/authStore';

interface ContactSidebarProps {
  apartment: GetApartmentDto;
  onRequestRental?: (moveInDate: string, moveOutDate: string) => void;
}

export const ContactSidebar = ({ apartment, onRequestRental }: ContactSidebarProps) => {
  const theme = useTheme();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const [moveInDate, setMoveInDate] = useState('');
  const [moveOutDate, setMoveOutDate] = useState('');

  // Get minimum date (today)
  const today = new Date().toISOString().split('T')[0];
  
  // Get maximum date (available until or 1 year from now)
  const getMaxDate = () => {
    try {
      const availableUntil = new Date(apartment.availableUntil);
      const oneYearFromNow = new Date();
      oneYearFromNow.setFullYear(oneYearFromNow.getFullYear() + 1);
      return availableUntil < oneYearFromNow
        ? apartment.availableUntil.split('T')[0]
        : oneYearFromNow.toISOString().split('T')[0];
    } catch {
      const oneYearFromNow = new Date();
      oneYearFromNow.setFullYear(oneYearFromNow.getFullYear() + 1);
      return oneYearFromNow.toISOString().split('T')[0];
    }
  };

  const maxDate = getMaxDate();

  const isFormValid = moveInDate && moveOutDate && moveInDate < moveOutDate;

  const handleRequestRental = () => {
    // TEMPORARY: Auth disabled for development - bypassing auth check
    // if (!isAuthenticated) {
    //   navigate('/login', { state: { returnTo: `/apartments/${apartment.apartmentId}` } });
    //   return;
    // }

    if (isFormValid && onRequestRental) {
      onRequestRental(moveInDate, moveOutDate);
    } else if (isFormValid) {
      // Default behavior: navigate to applications page
      navigate(`/apartments/${apartment.apartmentId}/applications?moveIn=${moveInDate}&moveOut=${moveOutDate}`);
    }
  };

  // Placeholder landlord data (would come from backend)
  const landlordName = 'Landlord';
  const landlordRating = 4.5;
  const landlordReviewCount = 0;
  const isVerified = true;

  return (
    <Box
      sx={{
        position: { md: 'sticky' },
        top: { md: 100 },
        height: { md: 'fit-content' },
      }}
    >
      {/* Landlord Card */}
      <Paper
        elevation={2}
        sx={{
          p: 3,
          mb: 3,
          borderRadius: 2,
        }}
      >
        <Stack direction="row" spacing={2} alignItems="center" sx={{ mb: 2 }}>
          <Avatar
            sx={{
              width: 64,
              height: 64,
              bgcolor: 'primary.main',
              fontSize: '1.5rem',
            }}
          >
            {landlordName.charAt(0).toUpperCase()}
          </Avatar>
          <Box sx={{ flex: 1 }}>
            <Typography variant="h6" sx={{ fontWeight: 600, mb: 0.5 }}>
              {landlordName}
            </Typography>
            {landlordReviewCount > 0 && (
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <Rating value={landlordRating} readOnly size="small" />
                <Typography variant="body2" color="text.secondary">
                  {landlordRating.toFixed(1)} ({landlordReviewCount})
                </Typography>
              </Box>
            )}
          </Box>
        </Stack>

        <Stack direction="row" spacing={1} flexWrap="wrap">
          {isVerified && (
            <Chip
              icon={<Verified sx={{ fontSize: 16 }} />}
              label="Verified"
              color="success"
              size="small"
              sx={{ fontSize: '0.75rem' }}
            />
          )}
          {landlordReviewCount > 10 && (
            <Chip
              icon={<Star sx={{ fontSize: 16 }} />}
              label="Excellent Landlord"
              color="primary"
              size="small"
              sx={{ fontSize: '0.75rem' }}
            />
          )}
        </Stack>
      </Paper>

      {/* Date Selection */}
      <Paper
        elevation={2}
        sx={{
          p: 3,
          mb: 3,
          borderRadius: 2,
        }}
      >
        <Typography
          variant="h6"
          sx={{
            fontWeight: 600,
            mb: 2,
          }}
        >
          Select Dates
        </Typography>

        <Stack spacing={2}>
          <TextField
            label="Move-in Date"
            type="date"
            value={moveInDate}
            onChange={(e) => setMoveInDate(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{
              min: today,
              max: maxDate,
            }}
            InputProps={{
              startAdornment: (
                <CalendarToday
                  sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }}
                />
              ),
            }}
            fullWidth
            required
          />

          <TextField
            label="Move-out Date"
            type="date"
            value={moveOutDate}
            onChange={(e) => setMoveOutDate(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{
              min: moveInDate || today,
              max: maxDate,
            }}
            InputProps={{
              startAdornment: (
                <CalendarToday
                  sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }}
                />
              ),
            }}
            fullWidth
            required
          />

          {moveInDate && moveOutDate && moveInDate >= moveOutDate && (
            <Typography variant="caption" color="error">
              Move-out date must be after move-in date
            </Typography>
          )}
        </Stack>
      </Paper>

      {/* Primary Action Button */}
      <Button
        variant="contained"
        fullWidth
        size="large"
        onClick={handleRequestRental}
        disabled={!isFormValid}
        sx={{
          py: 1.5,
          borderRadius: 2,
          textTransform: 'none',
          fontSize: '1rem',
          fontWeight: 600,
          mb: 3,
        }}
      >
        Request Rental
      </Button>

      {/* Trust/Info Card (Optional) */}
      <Paper
        elevation={0}
        sx={{
          p: 2,
          bgcolor: 'background.default',
          border: 1,
          borderColor: 'divider',
          borderRadius: 2,
        }}
      >
        <Typography
          variant="body2"
          color="text.secondary"
          sx={{ fontSize: '0.875rem', lineHeight: 1.6 }}
        >
          All rental requests are subject to landlord approval. You'll receive a
          response within 24-48 hours.
        </Typography>
      </Paper>
    </Box>
  );
};


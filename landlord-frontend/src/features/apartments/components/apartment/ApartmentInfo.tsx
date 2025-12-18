import { useState } from 'react';
import {
  Box,
  Typography,
  Chip,
  Stack,
  Grid,
  IconButton,
} from '@mui/material';
import {
  ExpandMore,
  ExpandLess,
  LocationOn,
  Bed,
  CheckCircle,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { GetApartmentDto } from '../../types';

interface ApartmentInfoProps {
  apartment: GetApartmentDto;
}

export const ApartmentInfo = ({ apartment }: ApartmentInfoProps) => {
  const [showFullDescription, setShowFullDescription] = useState(false);
  
  const description = apartment.description || 'No description available.';
  const shouldTruncate = description.length > 300;
  const displayDescription = shouldTruncate && !showFullDescription
    ? `${description.substring(0, 300)}...`
    : description;

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return format(date, 'MMM d, yyyy');
    } catch {
      return dateString;
    }
  };

  const formatAvailability = () => {
    try {
      const fromDate = new Date(apartment.availableFrom);
      const now = new Date();
      if (fromDate <= now) {
        return { text: 'Available now', color: 'success' as const };
      }
      return {
        text: `Available from ${formatDate(apartment.availableFrom)}`,
        color: 'info' as const,
      };
    } catch {
      return { text: 'Available now', color: 'success' as const };
    }
  };

  const availability = formatAvailability();

  // Basic amenities based on available data
  const amenities = [
    { name: 'WiFi', icon: 'wifi', available: true },
    { name: 'Kitchen', icon: 'kitchen', available: true },
    { name: 'Heating', icon: 'heating', available: true },
    { name: 'Bathroom', icon: 'bathroom', available: true },
  ];

  return (
    <Box>
      {/* Title and Location */}
      <Box sx={{ mb: 3 }}>
        <Typography
          variant="h4"
          component="h1"
          sx={{
            fontWeight: 700,
            mb: 1,
          }}
        >
          {apartment.title}
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, mb: 2 }}>
          <LocationOn sx={{ fontSize: 20, color: 'text.secondary' }} />
          <Typography variant="body1" color="text.secondary">
            {apartment.address}, {apartment.city}
          </Typography>
        </Box>
      </Box>

      {/* Price and Availability */}
      <Box sx={{ mb: 3 }}>
        <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, mb: 2 }}>
          <Typography
            variant="h4"
            color="primary"
            sx={{
              fontWeight: 700,
            }}
          >
            €{apartment.rent.toFixed(0)}
          </Typography>
          <Typography variant="h6" color="text.secondary">
            /month
          </Typography>
          {apartment.rentIncludeUtilities && (
            <Typography variant="body2" color="text.secondary">
              , incl. utilities
            </Typography>
          )}
        </Box>

        <Stack direction="row" spacing={1} flexWrap="wrap" sx={{ mb: 2 }}>
          <Chip
            label={availability.text}
            color={availability.color}
            size="small"
            sx={{ fontWeight: 500 }}
          />
          {apartment.numberOfRooms && (
            <Chip
              icon={<Bed sx={{ fontSize: 16 }} />}
              label={`${apartment.numberOfRooms} ${apartment.numberOfRooms === 1 ? 'room' : 'rooms'}`}
              variant="outlined"
              size="small"
            />
          )}
          {apartment.rentIncludeUtilities && (
            <Chip
              label="Utilities included"
              color="success"
              size="small"
            />
          )}
        </Stack>
      </Box>

      {/* Description */}
      <Box sx={{ mb: 4 }}>
        <Typography
          variant="h6"
          sx={{
            fontWeight: 600,
            mb: 2,
          }}
        >
          Description
        </Typography>
        <Typography
          variant="body1"
          sx={{
            lineHeight: 1.7,
            color: 'text.primary',
            whiteSpace: 'pre-wrap',
          }}
        >
          {displayDescription}
        </Typography>
        {shouldTruncate && (
          <IconButton
            onClick={() => setShowFullDescription(!showFullDescription)}
            sx={{
              mt: 1,
              color: 'primary.main',
            }}
          >
            {showFullDescription ? (
              <>
                <ExpandLess sx={{ mr: 0.5 }} />
                Show less
              </>
            ) : (
              <>
                <ExpandMore sx={{ mr: 0.5 }} />
                Show more
              </>
            )}
          </IconButton>
        )}
      </Box>

      {/* Amenities / Features */}
      <Box sx={{ mb: 4 }}>
        <Typography
          variant="h6"
          sx={{
            fontWeight: 600,
            mb: 2,
          }}
        >
          Amenities & Features
        </Typography>
        <Grid container spacing={2}>
          {amenities.map((amenity) => (
            <Grid item xs={6} sm={4} md={3} key={amenity.name}>
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'center',
                  gap: 1,
                  p: 1.5,
                  borderRadius: 2,
                  bgcolor: 'background.paper',
                  border: 1,
                  borderColor: 'divider',
                }}
              >
                <CheckCircle
                  sx={{
                    fontSize: 20,
                    color: 'success.main',
                  }}
                />
                <Typography variant="body2" sx={{ fontWeight: 500 }}>
                  {amenity.name}
                </Typography>
              </Box>
            </Grid>
          ))}
        </Grid>
      </Box>
    </Box>
  );
};


import { Box, Typography, useTheme } from '@mui/material';
import { LocationOn } from '@mui/icons-material';
import { GetApartmentDto } from '../../types';

interface ApartmentMapProps {
  apartment: GetApartmentDto;
  coordinates?: { lat: number; lng: number };
}

export const ApartmentMap = ({ apartment, coordinates }: ApartmentMapProps) => {
  const theme = useTheme();

  // Build address string for map
  const address = `${apartment.address}, ${apartment.city}${apartment.postalCode ? ` ${apartment.postalCode}` : ''}`;
  
  // Encode address for URL
  const encodedAddress = encodeURIComponent(address);

  // Use coordinates if available, otherwise use address
  const mapUrl = coordinates
    ? `https://www.openstreetmap.org/export/embed.html?bbox=${coordinates.lng - 0.01},${coordinates.lat - 0.01},${coordinates.lng + 0.01},${coordinates.lat + 0.01}&layer=mapnik&marker=${coordinates.lat},${coordinates.lng}`
    : `https://www.openstreetmap.org/export/embed.html?bbox=-0.1,-0.1,0.1,0.1&layer=mapnik&q=${encodedAddress}`;

  return (
    <Box sx={{ mb: 4 }}>
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
        <LocationOn sx={{ color: 'primary.main', fontSize: 28 }} />
        <Typography
          variant="h6"
          sx={{
            fontWeight: 600,
          }}
        >
          Location
        </Typography>
      </Box>

      <Box
        sx={{
          width: '100%',
          height: 400,
          borderRadius: 2,
          overflow: 'hidden',
          border: 1,
          borderColor: 'divider',
          bgcolor: 'background.paper',
          position: 'relative',
        }}
      >
        <iframe
          width="100%"
          height="100%"
          frameBorder="0"
          scrolling="no"
          marginHeight={0}
          marginWidth={0}
          src={mapUrl}
          style={{
            border: 0,
            filter: theme.palette.mode === 'dark' ? 'invert(0.9) hue-rotate(180deg)' : 'none',
          }}
          title="Apartment Location"
        />
        
        {/* Fallback if iframe doesn't load */}
        <Box
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            bgcolor: 'background.default',
            pointerEvents: 'none',
            opacity: 0,
            transition: 'opacity 0.3s',
            '&:hover': {
              opacity: 0.02,
            },
          }}
        >
          <Typography variant="body2" color="text.secondary">
            {address}
          </Typography>
        </Box>
      </Box>

      <Typography
        variant="body2"
        color="text.secondary"
        sx={{ mt: 1, display: 'flex', alignItems: 'center', gap: 0.5 }}
      >
        <LocationOn sx={{ fontSize: 16 }} />
        {address}
      </Typography>
    </Box>
  );
};


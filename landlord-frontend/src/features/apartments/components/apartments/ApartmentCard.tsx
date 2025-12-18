import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Card,
  CardMedia,
  CardContent,
  Box,
  Typography,
  IconButton,
  Chip,
  Stack,
  Rating,
  useTheme,
} from '@mui/material';
import {
  Favorite,
  FavoriteBorder,
  LocationOn,
  Bed,
  SquareFoot,
} from '@mui/icons-material';
import { format } from 'date-fns';
import { ApartmentDto } from '../../types';

interface ApartmentCardProps {
  apartment: ApartmentDto;
  imageUrl?: string;
  rating?: number;
  reviewCount?: number;
  size?: number;
  rooms?: number;
  furnished?: boolean;
  availableFrom?: string;
  rentIncludeUtilities?: boolean;
}

export const ApartmentCard = ({
  apartment,
  imageUrl,
  rating,
  reviewCount,
  size,
  rooms,
  furnished,
  availableFrom,
  rentIncludeUtilities,
}: ApartmentCardProps) => {
  const navigate = useNavigate();
  const theme = useTheme();
  const [isFavorite, setIsFavorite] = useState(false);
  const [imageError, setImageError] = useState(false);

  const handleCardClick = () => {
    navigate(`/apartments/${apartment.apartmentId}`);
  };

  const handleFavoriteClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    setIsFavorite(!isFavorite);
    // TODO: Implement favorite API call
  };

  const formatAvailability = () => {
    if (!availableFrom) return 'Available now';
    try {
      const date = new Date(availableFrom);
      const now = new Date();
      if (date <= now) return 'Available now';
      return `Available from ${format(date, 'MMM d, yyyy')}`;
    } catch {
      return 'Available now';
    }
  };

  const defaultImage = 'https://via.placeholder.com/400x300?text=Apartment';

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        borderRadius: 2,
        overflow: 'hidden',
        cursor: 'pointer',
        transition: 'transform 0.2s, box-shadow 0.2s',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: theme.shadows[8],
        },
      }}
      onClick={handleCardClick}
    >
      {/* Image Container */}
      <Box sx={{ position: 'relative', width: '100%', paddingTop: '75%' }}>
        <CardMedia
          component="img"
          image={imageError ? defaultImage : (imageUrl || defaultImage)}
          alt={apartment.title}
          onError={() => setImageError(true)}
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
          }}
        />
        
        {/* Favorite Icon */}
        <IconButton
          onClick={handleFavoriteClick}
          sx={{
            position: 'absolute',
            top: 8,
            right: 8,
            bgcolor: 'rgba(255, 255, 255, 0.9)',
            '&:hover': {
              bgcolor: 'rgba(255, 255, 255, 1)',
            },
            zIndex: 1,
          }}
        >
          {isFavorite ? (
            <Favorite sx={{ color: 'error.main' }} />
          ) : (
            <FavoriteBorder />
          )}
        </IconButton>

        {/* Image Dots Indicator (placeholder for carousel) */}
        {imageUrl && !imageError && (
          <Box
            sx={{
              position: 'absolute',
              bottom: 8,
              left: '50%',
              transform: 'translateX(-50%)',
              display: 'flex',
              gap: 0.5,
            }}
          >
            <Box
              sx={{
                width: 6,
                height: 6,
                borderRadius: '50%',
                bgcolor: 'rgba(255, 255, 255, 0.8)',
              }}
            />
          </Box>
        )}
      </Box>

      <CardContent sx={{ flexGrow: 1, p: 2 }}>
        {/* Title */}
        <Typography
          variant="h6"
          component="h3"
          sx={{
            fontWeight: 600,
            mb: 1,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            display: '-webkit-box',
            WebkitLineClamp: 1,
            WebkitBoxOrient: 'vertical',
          }}
        >
          {apartment.title}
        </Typography>

        {/* Location */}
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 1.5, gap: 0.5 }}>
          <LocationOn sx={{ fontSize: 16, color: 'text.secondary' }} />
          <Typography variant="body2" color="text.secondary">
            {apartment.address}, {apartment.city}
          </Typography>
        </Box>

        {/* Rating (if available) */}
        {rating !== undefined && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1.5 }}>
            <Rating value={rating} readOnly size="small" />
            <Typography variant="body2" color="text.secondary">
              {rating.toFixed(1)}
              {reviewCount !== undefined && ` (${reviewCount})`}
            </Typography>
          </Box>
        )}

        {/* Info Row */}
        <Stack direction="row" spacing={2} sx={{ mb: 1.5, flexWrap: 'wrap' }}>
          {size !== undefined && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <SquareFoot sx={{ fontSize: 16, color: 'text.secondary' }} />
              <Typography variant="body2" color="text.secondary">
                {size} m²
              </Typography>
            </Box>
          )}
          {rooms !== undefined && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <Bed sx={{ fontSize: 16, color: 'text.secondary' }} />
              <Typography variant="body2" color="text.secondary">
                {rooms} {rooms === 1 ? 'room' : 'rooms'}
              </Typography>
            </Box>
          )}
          {furnished && (
            <Chip
              label="Furnished"
              size="small"
              sx={{
                height: 20,
                fontSize: '0.7rem',
              }}
            />
          )}
        </Stack>

        {/* Price */}
        <Box sx={{ display: 'flex', alignItems: 'baseline', gap: 1, mb: 1 }}>
          <Typography
            variant="h6"
            color="primary"
            sx={{
              fontWeight: 700,
            }}
          >
            €{apartment.rent.toFixed(0)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            /month
          </Typography>
          {rentIncludeUtilities && (
            <Typography variant="caption" color="text.secondary">
              , incl. utilities
            </Typography>
          )}
        </Box>

        {/* Availability Badge */}
        <Chip
          label={formatAvailability()}
          size="small"
          color="success"
          sx={{
            mt: 0.5,
            height: 24,
            fontSize: '0.75rem',
            fontWeight: 500,
          }}
        />
      </CardContent>
    </Card>
  );
};


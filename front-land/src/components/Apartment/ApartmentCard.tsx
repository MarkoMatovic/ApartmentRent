import React, { useState } from 'react';
import {
  Card,
  CardContent,
  CardActions,
  Typography,
  Box,
  Button,
  Chip,
  IconButton,
  Switch,
  FormControlLabel,
} from '@mui/material';
import {
  Home as HomeIcon,
  LocationOn as LocationIcon,
  ChevronLeft,
  ChevronRight,
  People as PeopleIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { ApartmentDto, ApartmentImage } from '../../shared/types/apartment';
import StarRating from '../Review/StarRating';

interface ApartmentCardProps {
  apartment: ApartmentDto & {
    apartmentImages?: ApartmentImage[];
    isLookingForRoommate?: boolean;
    averageRating?: number;
    reviewCount?: number;
  };
  isOwner?: boolean;
  onToggleRoommate?: (isLookingForRoommate: boolean) => void;
  isUpdating?: boolean;
}

const ApartmentCard: React.FC<ApartmentCardProps> = ({ apartment, isOwner = false, onToggleRoommate, isUpdating = false }) => {
  const { t } = useTranslation(['common', 'apartments']);
  const navigate = useNavigate();
  const [currentImageIndex, setCurrentImageIndex] = useState(0);
  const [isHovered, setIsHovered] = useState(false);

  const images = apartment.apartmentImages || [];
  const hasImages = images.length > 0;
  const hasMultipleImages = images.length > 1;

  const handlePrevImage = (e: React.MouseEvent) => {
    e.stopPropagation();
    setCurrentImageIndex((prev) => (prev === 0 ? images.length - 1 : prev - 1));
  };

  const handleNextImage = (e: React.MouseEvent) => {
    e.stopPropagation();
    setCurrentImageIndex((prev) => (prev === images.length - 1 ? 0 : prev + 1));
  };

  const handleCardClick = () => {
    navigate(`/apartments/${apartment.apartmentId}`);
  };

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        cursor: 'pointer',
        transition: 'all 0.3s ease',
        '&:hover': { 
          boxShadow: 4,
          transform: 'translateY(-4px)',
        },
      }}
      onClick={handleCardClick}
    >
      <Box
        sx={{
          position: 'relative',
          height: 200,
          overflow: 'hidden',
        }}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {hasImages ? (
          <Box
            component="img"
            src={images[currentImageIndex].imageUrl}
            alt={apartment.title}
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'cover',
              transition: 'opacity 0.3s ease',
            }}
          />
        ) : (
          <Box
            sx={{
              width: '100%',
              height: '100%',
              bgcolor: 'grey.300',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <HomeIcon sx={{ fontSize: 60, color: 'grey.500' }} />
          </Box>
        )}


        {/* Public badge - shown when looking for roommate and not owner */}
        {!isOwner && apartment.isLookingForRoommate && (
          <Box
            sx={{
              position: 'absolute',
              top: 12,
              right: 12,
              zIndex: 2,
            }}
          >
            <Chip
              icon={<PeopleIcon />}
              label={t('apartments:lookingForRoommate', { defaultValue: 'Looking for Roommate' })}
              size="small"
              sx={{
                bgcolor: 'success.main',
                color: 'white',
                fontWeight: 600,
                animation: 'blink 2s ease-in-out infinite',
                '@keyframes blink': {
                  '0%, 100%': {
                    opacity: 1,
                  },
                  '50%': {
                    opacity: 0.6,
                  },
                },
              }}
            />
          </Box>
        )}

        {hasMultipleImages && isHovered && (
          <>
            <IconButton
              onClick={handlePrevImage}
              sx={{
                position: 'absolute',
                left: 8,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                color: 'grey.900',
                zIndex: 2,
                '&:hover': {
                  bgcolor: 'white',
                },
                transition: 'all 0.3s ease',
              }}
            >
              <ChevronLeft />
            </IconButton>
            <IconButton
              onClick={handleNextImage}
              sx={{
                position: 'absolute',
                right: 8,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                color: 'grey.900',
                zIndex: 2,
                '&:hover': {
                  bgcolor: 'white',
                },
                transition: 'all 0.3s ease',
              }}
            >
              <ChevronRight />
            </IconButton>
          </>
        )}

        {hasMultipleImages && (
          <Box
            sx={{
              position: 'absolute',
              bottom: 8,
              left: '50%',
              transform: 'translateX(-50%)',
              display: 'flex',
              gap: 0.5,
              zIndex: 1,
            }}
          >
            {images.map((_, index) => (
              <Box
                key={index}
                sx={{
                  width: 8,
                  height: 8,
                  borderRadius: '50%',
                  bgcolor: index === currentImageIndex ? 'white' : 'rgba(255, 255, 255, 0.5)',
                  transition: 'all 0.3s ease',
                }}
              />
            ))}
          </Box>
        )}
      </Box>

      <CardContent sx={{ flexGrow: 1 }}>
        {/* Owner controls - switcher below image, above description */}
        {isOwner && (
          <Box
            sx={{
              mb: 2,
              pb: 1,
              borderBottom: 1,
              borderColor: 'divider',
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <FormControlLabel
              control={
                <Switch
                  size="small"
                  checked={apartment.isLookingForRoommate || false}
                  onChange={(e) => {
                    e.stopPropagation();
                    if (onToggleRoommate) {
                      onToggleRoommate(e.target.checked);
                    }
                  }}
                  disabled={isUpdating}
                />
              }
              label={
                <Typography variant="body2">
                  {t('apartments:lookingForRoommate', { defaultValue: 'Looking for Roommate' })}
                </Typography>
              }
            />
          </Box>
        )}

        <Typography variant="h6" component="h2" gutterBottom>
          {apartment.title}
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
          <LocationIcon fontSize="small" color="action" />
          <Typography variant="body2" color="text.secondary">
            {apartment.address}, {apartment.city}
          </Typography>
        </Box>
        
        {apartment.averageRating !== undefined && apartment.reviewCount !== undefined && apartment.reviewCount > 0 && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <StarRating rating={apartment.averageRating} size="small" showNumber />
            <Typography variant="caption" color="text.secondary">
              ({apartment.reviewCount})
            </Typography>
          </Box>
        )}
        
        <Typography variant="h5" color="primary" sx={{ mt: 1 }}>
          €{apartment.rent}/mo
        </Typography>
        <Box sx={{ mt: 1, display: 'flex', gap: 1, flexWrap: 'wrap' }}>
          {apartment.isFurnished && (
            <Chip label={t('apartments:furnished')} size="small" />
          )}
          {apartment.isImmediatelyAvailable && (
            <Chip label={t('apartments:immediatelyAvailable')} size="small" color="success" />
          )}
        </Box>
      </CardContent>
      <CardActions>
        <Button size="small" color="secondary">
          {t('view')}
        </Button>
      </CardActions>
    </Card>
  );
};

export default ApartmentCard;

import React, { useState } from 'react';
import { Box, IconButton, Chip } from '@mui/material';
import {
  ChevronLeft,
  ChevronRight,
  Home as HomeIcon,
  People as PeopleIcon,
} from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { ApartmentImage } from '../../shared/types/apartment';

interface ApartmentImageGalleryProps {
  images?: ApartmentImage[];
  title: string;
  isLookingForRoommate?: boolean;
}

const ApartmentImageGallery: React.FC<ApartmentImageGalleryProps> = ({
  images = [],
  title,
  isLookingForRoommate,
}) => {
  const { t } = useTranslation('apartments');
  const [currentImageIndex, setCurrentImageIndex] = useState(0);
  const [isHovered, setIsHovered] = useState(false);

  const hasImages = images.length > 0;
  const hasMultipleImages = images.length > 1;

  const handlePrevImage = () => {
    setCurrentImageIndex((prev) => (prev === 0 ? images.length - 1 : prev - 1));
  };

  const handleNextImage = () => {
    setCurrentImageIndex((prev) => (prev === images.length - 1 ? 0 : prev + 1));
  };

  const handleThumbnailClick = (index: number) => {
    setCurrentImageIndex(index);
  };

  return (
    <Box>
      <Box
        sx={{
          position: 'relative',
          height: 500,
          bgcolor: 'grey.300',
          borderRadius: 2,
          overflow: 'hidden',
          mb: 2,
        }}
        onMouseEnter={() => setIsHovered(true)}
        onMouseLeave={() => setIsHovered(false)}
      >
        {hasImages ? (
          <Box
            component="img"
            src={images[currentImageIndex].imageUrl}
            alt={`${title} - Image ${currentImageIndex + 1}`}
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
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <HomeIcon sx={{ fontSize: 100, color: 'grey.500' }} />
          </Box>
        )}

        {isLookingForRoommate && (
          <Box
            sx={{
              position: 'absolute',
              top: 16,
              right: 16,
              zIndex: 2,
            }}
          >
            <Chip
              icon={<PeopleIcon />}
              label={t('lookingForRoommate', { defaultValue: 'Looking for Roommate' })}
              size="medium"
              sx={{
                bgcolor: 'success.main',
                color: 'white',
                fontWeight: 600,
                fontSize: '0.9rem',
                px: 1,
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
                left: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                color: 'grey.900',
                zIndex: 2,
                width: 48,
                height: 48,
                '&:hover': {
                  bgcolor: 'white',
                  transform: 'translateY(-50%) scale(1.1)',
                },
                transition: 'all 0.3s ease',
              }}
            >
              <ChevronLeft sx={{ fontSize: 32 }} />
            </IconButton>
            <IconButton
              onClick={handleNextImage}
              sx={{
                position: 'absolute',
                right: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                color: 'grey.900',
                zIndex: 2,
                width: 48,
                height: 48,
                '&:hover': {
                  bgcolor: 'white',
                  transform: 'translateY(-50%) scale(1.1)',
                },
                transition: 'all 0.3s ease',
              }}
            >
              <ChevronRight sx={{ fontSize: 32 }} />
            </IconButton>
          </>
        )}

        {hasMultipleImages && (
          <Box
            sx={{
              position: 'absolute',
              bottom: 16,
              left: '50%',
              transform: 'translateX(-50%)',
              display: 'flex',
              gap: 1,
              zIndex: 1,
              bgcolor: 'rgba(0, 0, 0, 0.4)',
              borderRadius: 2,
              px: 2,
              py: 1,
            }}
          >
            {images.map((_, index) => (
              <Box
                key={index}
                onClick={(e) => {
                  e.stopPropagation();
                  handleThumbnailClick(index);
                }}
                sx={{
                  width: 10,
                  height: 10,
                  borderRadius: '50%',
                  bgcolor: index === currentImageIndex ? 'white' : 'rgba(255, 255, 255, 0.5)',
                  cursor: 'pointer',
                  transition: 'all 0.3s ease',
                  '&:hover': {
                    bgcolor: 'white',
                    transform: 'scale(1.2)',
                  },
                }}
              />
            ))}
          </Box>
        )}
      </Box>

      {hasMultipleImages && (
        <Box
          sx={{
            display: 'flex',
            gap: 1,
            overflowX: 'auto',
            pb: 1,
            '&::-webkit-scrollbar': {
              height: 6,
            },
            '&::-webkit-scrollbar-thumb': {
              bgcolor: 'grey.400',
              borderRadius: 3,
            },
          }}
        >
          {images.map((image, index) => (
            <Box
              key={image.imageId}
              onClick={() => handleThumbnailClick(index)}
              sx={{
                minWidth: 100,
                height: 80,
                borderRadius: 1,
                overflow: 'hidden',
                cursor: 'pointer',
                border: index === currentImageIndex ? '3px solid' : '2px solid transparent',
                borderColor: index === currentImageIndex ? 'primary.main' : 'transparent',
                transition: 'all 0.3s ease',
                '&:hover': {
                  borderColor: 'primary.light',
                  transform: 'scale(1.05)',
                },
              }}
            >
              <Box
                component="img"
                src={image.imageUrl}
                alt={`${title} - Thumbnail ${index + 1}`}
                sx={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                }}
              />
            </Box>
          ))}
        </Box>
      )}
    </Box>
  );
};

export default ApartmentImageGallery;

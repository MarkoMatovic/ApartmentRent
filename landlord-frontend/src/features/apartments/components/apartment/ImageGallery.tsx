import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  IconButton,
  Stack,
  useTheme,
} from '@mui/material';
import {
  ChevronLeft,
  ChevronRight,
  Favorite,
  FavoriteBorder,
  Share,
} from '@mui/icons-material';
import { useAuthStore } from '@/features/auth/store/authStore';

interface ImageGalleryProps {
  images?: string[];
  apartmentId: number;
  apartmentTitle: string;
}

export const ImageGallery = ({ images = [], apartmentId, apartmentTitle }: ImageGalleryProps) => {
  const theme = useTheme();
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const [currentIndex, setCurrentIndex] = useState(0);
  const [isFavorite, setIsFavorite] = useState(false);

  // Use placeholder if no images
  const displayImages = images.length > 0 
    ? images 
    : ['https://via.placeholder.com/800x600?text=Apartment+Image'];

  const currentImage = displayImages[currentIndex];

  const handlePrevious = () => {
    setCurrentIndex((prev) => (prev === 0 ? displayImages.length - 1 : prev - 1));
  };

  const handleNext = () => {
    setCurrentIndex((prev) => (prev === displayImages.length - 1 ? 0 : prev + 1));
  };

  const handleThumbnailClick = (index: number) => {
    setCurrentIndex(index);
  };

  const handleFavoriteClick = () => {
    // TEMPORARY: Auth disabled for development - bypassing auth check
    // if (!isAuthenticated) {
    //   navigate('/login', { state: { returnTo: `/apartments/${apartmentId}` } });
    //   return;
    // }

    setIsFavorite(!isFavorite);
    // TODO: Implement favorite API call
  };

  const handleShareClick = () => {
    if (navigator.share) {
      navigator.share({
        title: apartmentTitle,
        text: `Check out this apartment: ${apartmentTitle}`,
        url: window.location.href,
      }).catch(() => {
        // Fallback: copy to clipboard
        navigator.clipboard.writeText(window.location.href);
      });
    } else {
      // Fallback: copy to clipboard
      navigator.clipboard.writeText(window.location.href);
    }
  };

  return (
    <Box sx={{ mb: 4 }}>
      {/* Main Image Container */}
      <Box
        sx={{
          position: 'relative',
          width: '100%',
          paddingTop: '56.25%', // 16:9 aspect ratio
          borderRadius: 2,
          overflow: 'hidden',
          bgcolor: 'background.paper',
          mb: 2,
        }}
      >
        <Box
          component="img"
          src={currentImage}
          alt={`${apartmentTitle} - Image ${currentIndex + 1}`}
          sx={{
            position: 'absolute',
            top: 0,
            left: 0,
            width: '100%',
            height: '100%',
            objectFit: 'cover',
            transition: 'opacity 0.3s ease',
          }}
        />

        {/* Navigation Arrows */}
        {displayImages.length > 1 && (
          <>
            <IconButton
              onClick={handlePrevious}
              sx={{
                position: 'absolute',
                left: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                '&:hover': {
                  bgcolor: 'rgba(255, 255, 255, 1)',
                },
                zIndex: 1,
              }}
            >
              <ChevronLeft />
            </IconButton>
            <IconButton
              onClick={handleNext}
              sx={{
                position: 'absolute',
                right: 16,
                top: '50%',
                transform: 'translateY(-50%)',
                bgcolor: 'rgba(255, 255, 255, 0.9)',
                '&:hover': {
                  bgcolor: 'rgba(255, 255, 255, 1)',
                },
                zIndex: 1,
              }}
            >
              <ChevronRight />
            </IconButton>
          </>
        )}

        {/* Favorite and Share Icons */}
        <Stack
          direction="row"
          spacing={1}
          sx={{
            position: 'absolute',
            top: 16,
            right: 16,
            zIndex: 1,
          }}
        >
          <IconButton
            onClick={handleFavoriteClick}
            sx={{
              bgcolor: 'rgba(255, 255, 255, 0.9)',
              '&:hover': {
                bgcolor: 'rgba(255, 255, 255, 1)',
              },
            }}
          >
            {isFavorite ? (
              <Favorite sx={{ color: 'error.main' }} />
            ) : (
              <FavoriteBorder />
            )}
          </IconButton>
          <IconButton
            onClick={handleShareClick}
            sx={{
              bgcolor: 'rgba(255, 255, 255, 0.9)',
              '&:hover': {
                bgcolor: 'rgba(255, 255, 255, 1)',
              },
            }}
          >
            <Share />
          </IconButton>
        </Stack>

        {/* Image Counter */}
        {displayImages.length > 1 && (
          <Box
            sx={{
              position: 'absolute',
              bottom: 16,
              right: 16,
              bgcolor: 'rgba(0, 0, 0, 0.6)',
              color: 'white',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              fontSize: '0.875rem',
            }}
          >
            {currentIndex + 1} / {displayImages.length}
          </Box>
        )}
      </Box>

      {/* Thumbnail Gallery */}
      {displayImages.length > 1 && (
        <Stack
          direction="row"
          spacing={1}
          sx={{
            overflowX: 'auto',
            pb: 1,
            '&::-webkit-scrollbar': {
              height: 6,
            },
            '&::-webkit-scrollbar-thumb': {
              bgcolor: 'divider',
              borderRadius: 3,
            },
          }}
        >
          {displayImages.map((image, index) => (
            <Box
              key={index}
              onClick={() => handleThumbnailClick(index)}
              sx={{
                minWidth: 100,
                width: 100,
                height: 75,
                borderRadius: 1,
                overflow: 'hidden',
                cursor: 'pointer',
                border: 2,
                borderColor: currentIndex === index ? 'primary.main' : 'transparent',
                opacity: currentIndex === index ? 1 : 0.7,
                transition: 'opacity 0.2s, border-color 0.2s',
                '&:hover': {
                  opacity: 1,
                },
              }}
            >
              <Box
                component="img"
                src={image}
                alt={`Thumbnail ${index + 1}`}
                sx={{
                  width: '100%',
                  height: '100%',
                  objectFit: 'cover',
                }}
              />
            </Box>
          ))}
        </Stack>
      )}
    </Box>
  );
};


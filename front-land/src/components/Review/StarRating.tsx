import React from 'react';
import { Box } from '@mui/material';
import { Star, StarBorder, StarHalf } from '@mui/icons-material';

interface StarRatingProps {
  rating: number;
  maxRating?: number;
  size?: 'small' | 'medium' | 'large';
  showNumber?: boolean;
  interactive?: boolean;
  onChange?: (rating: number) => void;
}

const StarRating: React.FC<StarRatingProps> = ({
  rating,
  maxRating = 5,
  size = 'medium',
  showNumber = false,
  interactive = false,
  onChange,
}) => {
  const [hoverRating, setHoverRating] = React.useState(0);

  const getFontSize = () => {
    switch (size) {
      case 'small':
        return 18;
      case 'large':
        return 32;
      default:
        return 24;
    }
  };

  const fontSize = getFontSize();
  const displayRating = interactive && hoverRating > 0 ? hoverRating : rating;

  const handleClick = (value: number) => {
    if (interactive && onChange) {
      onChange(value);
    }
  };

  const handleMouseEnter = (value: number) => {
    if (interactive) {
      setHoverRating(value);
    }
  };

  const handleMouseLeave = () => {
    if (interactive) {
      setHoverRating(0);
    }
  };

  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 0.5,
      }}
    >
      {Array.from({ length: maxRating }, (_, index) => {
        const starValue = index + 1;
        const filled = displayRating >= starValue;
        const half = displayRating >= starValue - 0.5 && displayRating < starValue;

        return (
          <Box
            key={index}
            onClick={() => handleClick(starValue)}
            onMouseEnter={() => handleMouseEnter(starValue)}
            onMouseLeave={handleMouseLeave}
            sx={{
              cursor: interactive ? 'pointer' : 'default',
              color: filled || half ? 'warning.main' : 'grey.400',
              transition: 'all 0.2s ease',
              '&:hover': interactive
                ? {
                    transform: 'scale(1.2)',
                  }
                : {},
            }}
          >
            {filled ? (
              <Star sx={{ fontSize }} />
            ) : half ? (
              <StarHalf sx={{ fontSize }} />
            ) : (
              <StarBorder sx={{ fontSize }} />
            )}
          </Box>
        );
      })}
      {showNumber && (
        <Box
          component="span"
          sx={{
            ml: 1,
            fontWeight: 600,
            color: 'text.secondary',
            fontSize: size === 'small' ? '0.875rem' : '1rem',
          }}
        >
          {rating.toFixed(1)}
        </Box>
      )}
    </Box>
  );
};

export default StarRating;

import React from 'react';
import {
  Box,
  Paper,
  Typography,
  Avatar,
  Chip,
} from '@mui/material';
import { Person as PersonIcon, Lock as LockIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { Review } from '../../shared/types/review';
import StarRating from './StarRating';
import { format } from 'date-fns';

interface ReviewCardProps {
  review: Review;
}

const ReviewCard: React.FC<ReviewCardProps> = ({ review }) => {
  const { t } = useTranslation(['common', 'reviews']);

  const displayName = review.isAnonymous
    ? t('reviews:anonymous', { defaultValue: 'Anonymous User' })
    : `${review.user?.firstName || ''} ${review.user?.lastName || ''}`.trim() || t('reviews:unknownUser', { defaultValue: 'Unknown User' });

  const formattedDate = review.createdAt
    ? format(new Date(review.createdAt), 'MMM dd, yyyy')
    : '';

  return (
    <Paper
      elevation={1}
      sx={{
        p: 2.5,
        mb: 2,
        borderRadius: 2,
        transition: 'all 0.2s ease',
        '&:hover': {
          boxShadow: 3,
        },
      }}
    >
      <Box sx={{ display: 'flex', gap: 2 }}>
        {/* Avatar */}
        <Avatar
          src={!review.isAnonymous ? review.user?.profilePicture : undefined}
          sx={{
            width: 48,
            height: 48,
            bgcolor: review.isAnonymous ? 'grey.400' : 'primary.main',
          }}
        >
          {review.isAnonymous ? (
            <PersonIcon />
          ) : (
            displayName.charAt(0).toUpperCase()
          )}
        </Avatar>

        {/* Content */}
        <Box sx={{ flex: 1 }}>
          <Box
            sx={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'flex-start',
              mb: 1,
            }}
          >
            <Box>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 0.5 }}>
                <Typography variant="subtitle1" fontWeight={600}>
                  {displayName}
                </Typography>
                {review.isAnonymous && (
                  <Chip
                    label={t('reviews:anonymous', { defaultValue: 'Anonymous' })}
                    size="small"
                    sx={{
                      height: 20,
                      fontSize: '0.7rem',
                      bgcolor: 'grey.300',
                    }}
                  />
                )}
                {!review.isPublic && (
                  <Chip
                    icon={<LockIcon sx={{ fontSize: 14 }} />}
                    label={t('reviews:private', { defaultValue: 'Private' })}
                    size="small"
                    sx={{
                      height: 20,
                      fontSize: '0.7rem',
                      bgcolor: 'warning.light',
                      color: 'warning.dark',
                    }}
                  />
                )}
              </Box>
              <Typography variant="caption" color="text.secondary">
                {formattedDate}
              </Typography>
            </Box>
            <StarRating rating={review.rating} size="small" />
          </Box>

          {review.comment && (
            <Typography
              variant="body2"
              color="text.secondary"
              sx={{
                mt: 1.5,
                lineHeight: 1.6,
              }}
            >
              {review.comment}
            </Typography>
          )}
        </Box>
      </Box>
    </Paper>
  );
};

export default ReviewCard;

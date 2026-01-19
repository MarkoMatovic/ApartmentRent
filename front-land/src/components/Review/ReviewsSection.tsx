import React from 'react';
import {
  Box,
  Paper,
  Typography,
  LinearProgress,
  CircularProgress,
  Alert,
} from '@mui/material';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { reviewsApi } from '../../shared/api/reviews';
import StarRating from './StarRating';
import ReviewCard from './ReviewCard';
import CreateReviewForm from './CreateReviewForm';

interface ReviewsSectionProps {
  apartmentId: number;
}

const ReviewsSection: React.FC<ReviewsSectionProps> = ({ apartmentId }) => {
  const { t } = useTranslation(['common', 'reviews']);

  const {
    data: reviews = [],
    isLoading,
    error,
  } = useQuery({
    queryKey: ['reviews', apartmentId],
    queryFn: () => reviewsApi.getByApartmentId(apartmentId),
  });

  // Calculate statistics
  const publicReviews = reviews.filter((r) => r.isPublic !== false);
  const totalReviews = publicReviews.length;
  const averageRating = totalReviews > 0
    ? publicReviews.reduce((sum, r) => sum + r.rating, 0) / totalReviews
    : 0;

  const ratingDistribution = [5, 4, 3, 2, 1].map((star) => ({
    star,
    count: publicReviews.filter((r) => r.rating === star).length,
    percentage: totalReviews > 0
      ? (publicReviews.filter((r) => r.rating === star).length / totalReviews) * 100
      : 0,
  }));

  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  return (
    <Box>
      {/* Create Review Form */}
      <CreateReviewForm apartmentId={apartmentId} />

      {/* Reviews Summary */}
      {totalReviews > 0 && (
        <Paper elevation={2} sx={{ p: 3, mb: 3, borderRadius: 2 }}>
          <Typography variant="h6" gutterBottom>
            {t('reviews:reviewsSummary', { defaultValue: 'Reviews Summary' })}
          </Typography>

          <Box sx={{ display: 'flex', gap: 4, mb: 3 }}>
            {/* Average Rating */}
            <Box sx={{ textAlign: 'center', minWidth: 150 }}>
              <Typography variant="h2" fontWeight={700} color="warning.main">
                {averageRating.toFixed(1)}
              </Typography>
              <StarRating rating={averageRating} size="medium" />
              <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                {t('reviews:basedOn', { defaultValue: 'Based on {{count}} reviews', count: totalReviews })}
              </Typography>
            </Box>

            {/* Rating Distribution */}
            <Box sx={{ flex: 1 }}>
              {ratingDistribution.map(({ star, count, percentage }) => (
                <Box
                  key={star}
                  sx={{
                    display: 'flex',
                    alignItems: 'center',
                    gap: 2,
                    mb: 1,
                  }}
                >
                  <Typography
                    variant="body2"
                    sx={{ minWidth: 60, fontWeight: 500 }}
                  >
                    {star} {t('reviews:stars', { defaultValue: 'stars' })}
                  </Typography>
                  <LinearProgress
                    variant="determinate"
                    value={percentage}
                    sx={{
                      flex: 1,
                      height: 8,
                      borderRadius: 4,
                      bgcolor: 'grey.200',
                      '& .MuiLinearProgress-bar': {
                        bgcolor: 'warning.main',
                      },
                    }}
                  />
                  <Typography
                    variant="body2"
                    color="text.secondary"
                    sx={{ minWidth: 40, textAlign: 'right' }}
                  >
                    {count}
                  </Typography>
                </Box>
              ))}
            </Box>
          </Box>
        </Paper>
      )}

      {/* Reviews List */}
      <Box>
        <Typography variant="h6" gutterBottom sx={{ mb: 2 }}>
          {t('reviews:allReviews', { defaultValue: 'All Reviews' })} ({totalReviews})
        </Typography>

        {totalReviews === 0 ? (
          <Paper
            elevation={1}
            sx={{
              p: 4,
              textAlign: 'center',
              bgcolor: 'background.paper',
              borderRadius: 2,
            }}
          >
            <Typography variant="body1" color="text.secondary">
              {t('reviews:noReviewsYet', { defaultValue: 'No reviews yet. Be the first to review!' })}
            </Typography>
          </Paper>
        ) : (
          <Box>
            {publicReviews.map((review) => (
              <ReviewCard key={review.reviewId} review={review} />
            ))}
          </Box>
        )}
      </Box>

      {error && (
        <Alert severity="error" sx={{ mt: 2 }}>
          {t('reviews:loadError', { defaultValue: 'Failed to load reviews' })}
        </Alert>
      )}
    </Box>
  );
};

export default ReviewsSection;

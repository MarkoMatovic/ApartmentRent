import React, { useState } from 'react';
import {
  Box,
  Paper,
  Typography,
  TextField,
  Button,
  FormControlLabel,
  Switch,
  Alert,
  Collapse,
} from '@mui/material';
import { RateReview as ReviewIcon } from '@mui/icons-material';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { reviewsApi } from '../../shared/api/reviews';
import { CreateReviewRequest } from '../../shared/types/review';
import StarRating from './StarRating';
import { useAuth } from '../../shared/context/AuthContext';

interface CreateReviewFormProps {
  apartmentId: number;
  onSuccess?: () => void;
}

const CreateReviewForm: React.FC<CreateReviewFormProps> = ({
  apartmentId,
  onSuccess,
}) => {
  const { t } = useTranslation(['common', 'reviews']);
  const { isAuthenticated, user } = useAuth();
  const queryClient = useQueryClient();
  
  const [isOpen, setIsOpen] = useState(false);
  const [rating, setRating] = useState(0);
  const [comment, setComment] = useState('');
  const [isAnonymous, setIsAnonymous] = useState(false);
  const [isPublic, setIsPublic] = useState(true);
  const [error, setError] = useState('');

  const createReviewMutation = useMutation({
    mutationFn: (data: CreateReviewRequest) => reviewsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reviews', apartmentId] });
      setRating(0);
      setComment('');
      setIsAnonymous(false);
      setIsPublic(true);
      setError('');
      setIsOpen(false);
      onSuccess?.();
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || t('reviews:createError', { defaultValue: 'Failed to submit review' }));
    },
  });

  const handleSubmit = () => {
    if (rating === 0) {
      setError(t('reviews:ratingRequired', { defaultValue: 'Please select a rating' }));
      return;
    }

    if (!user) {
      setError(t('reviews:userNotFound', { defaultValue: 'User not authenticated' }));
      return;
    }

    createReviewMutation.mutate({
      userId: user.userId,
      apartmentId,
      rating,
      comment: comment.trim() || undefined,
      isAnonymous,
      isPublic,
      createdByGuid: user.userGuid,
    });
  };

  const handleCancel = () => {
    setIsOpen(false);
    setRating(0);
    setComment('');
    setError('');
  };

  if (!isAuthenticated) {
    return null;
  }

  return (
    <Box sx={{ mb: 3 }}>
      {!isOpen ? (
        <Button
          variant="contained"
          color="secondary"
          startIcon={<ReviewIcon />}
          onClick={() => setIsOpen(true)}
          fullWidth
          sx={{
            py: 1.5,
            fontWeight: 600,
          }}
        >
          {t('reviews:writeReview', { defaultValue: 'Write a Review' })}
        </Button>
      ) : (
        <Paper
          elevation={2}
          sx={{
            p: 3,
            borderRadius: 2,
            border: '2px solid',
            borderColor: 'secondary.main',
          }}
        >
          <Typography variant="h6" gutterBottom sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <ReviewIcon />
            {t('reviews:yourReview', { defaultValue: 'Your Review' })}
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }} onClose={() => setError('')}>
              {error}
            </Alert>
          )}

          {/* Rating */}
          <Box sx={{ mb: 3 }}>
            <Typography variant="subtitle2" gutterBottom>
              {t('reviews:rating', { defaultValue: 'Rating' })} *
            </Typography>
            <StarRating
              rating={rating}
              interactive
              onChange={setRating}
              size="large"
            />
            {rating > 0 && (
              <Typography variant="caption" color="text.secondary" sx={{ ml: 1 }}>
                {rating === 1 && t('reviews:poor', { defaultValue: 'Poor' })}
                {rating === 2 && t('reviews:fair', { defaultValue: 'Fair' })}
                {rating === 3 && t('reviews:good', { defaultValue: 'Good' })}
                {rating === 4 && t('reviews:veryGood', { defaultValue: 'Very Good' })}
                {rating === 5 && t('reviews:excellent', { defaultValue: 'Excellent' })}
              </Typography>
            )}
          </Box>

          {/* Comment */}
          <TextField
            fullWidth
            multiline
            rows={4}
            label={t('reviews:comment', { defaultValue: 'Comment (Optional)' })}
            placeholder={t('reviews:commentPlaceholder', { defaultValue: 'Share your experience with this apartment...' })}
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            sx={{ mb: 2 }}
          />

          {/* Privacy Options */}
          <Box sx={{ mb: 3 }}>
            <FormControlLabel
              control={
                <Switch
                  checked={isAnonymous}
                  onChange={(e) => setIsAnonymous(e.target.checked)}
                  color="secondary"
                />
              }
              label={
                <Box>
                  <Typography variant="body2" fontWeight={500}>
                    {t('reviews:postAnonymously', { defaultValue: 'Post Anonymously' })}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {t('reviews:anonymousDescription', { defaultValue: 'Your name will be hidden from other users' })}
                  </Typography>
                </Box>
              }
            />
            <FormControlLabel
              control={
                <Switch
                  checked={isPublic}
                  onChange={(e) => setIsPublic(e.target.checked)}
                  color="secondary"
                />
              }
              label={
                <Box>
                  <Typography variant="body2" fontWeight={500}>
                    {t('reviews:makePublic', { defaultValue: 'Make Public' })}
                  </Typography>
                  <Typography variant="caption" color="text.secondary">
                    {t('reviews:publicDescription', { defaultValue: 'Allow everyone to see this review' })}
                  </Typography>
                </Box>
              }
            />
          </Box>

          {/* Actions */}
          <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end' }}>
            <Button
              variant="outlined"
              onClick={handleCancel}
              disabled={createReviewMutation.isPending}
            >
              {t('cancel')}
            </Button>
            <Button
              variant="contained"
              color="secondary"
              onClick={handleSubmit}
              disabled={createReviewMutation.isPending || rating === 0}
            >
              {createReviewMutation.isPending
                ? t('reviews:submitting', { defaultValue: 'Submitting...' })
                : t('reviews:submitReview', { defaultValue: 'Submit Review' })}
            </Button>
          </Box>
        </Paper>
      )}
    </Box>
  );
};

export default CreateReviewForm;

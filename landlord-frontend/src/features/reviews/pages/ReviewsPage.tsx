import { useState } from 'react';
import {
  Box,
  Typography,
  Paper,
  TextField,
  Button,
  Rating,
  Alert,
} from '@mui/material';
import { useMutation } from '@tanstack/react-query';
import { reviewsService } from '../api/reviewsService';
import { useAuthStore } from '@/features/auth/store/authStore';
import { CreateReviewRequest } from '../types';
import { AppLayout } from '@/shared/components/layout/AppLayout';

const ReviewsPage = () => {
  const { userGuid, userId } = useAuthStore();
  const [rating, setRating] = useState<number | null>(0);
  const [reviewText, setReviewText] = useState('');
  const [tenantId, setTenantId] = useState('');
  const [landlordId, setLandlordId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState(false);

  const createReviewMutation = useMutation({
    mutationFn: (data: CreateReviewRequest) => reviewsService.createReview(data),
    onSuccess: () => {
      setSuccess(true);
      setReviewText('');
      setRating(0);
      setTenantId('');
      setLandlordId('');
      setTimeout(() => setSuccess(false), 3000);
    },
    onError: (err: any) => {
      setError(err.message || 'Failed to create review');
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setSuccess(false);

    if (!rating || !reviewText || !tenantId || !landlordId || !userGuid) {
      setError('Please fill in all fields');
      return;
    }

    createReviewMutation.mutate({
      tenantId: parseInt(tenantId, 10),
      landlordId: parseInt(landlordId, 10),
      rating,
      reviewText,
      createdByGuid: userGuid,
    });
  };

  return (
    <AppLayout>
      <Typography variant="h4" component="h1" gutterBottom>
        Create Review
      </Typography>
      <Paper elevation={3} sx={{ p: 4, mt: 2 }}>
        {error && (
          <Alert severity="error" sx={{ mb: 2 }}>
            {error}
          </Alert>
        )}
        {success && (
          <Alert severity="success" sx={{ mb: 2 }}>
            Review created successfully!
          </Alert>
        )}
        <Box component="form" onSubmit={handleSubmit}>
          <TextField
            fullWidth
            label="Tenant ID"
            type="number"
            value={tenantId}
            onChange={(e) => setTenantId(e.target.value)}
            margin="normal"
            required
          />
          <TextField
            fullWidth
            label="Landlord ID"
            type="number"
            value={landlordId}
            onChange={(e) => setLandlordId(e.target.value)}
            margin="normal"
            required
          />
          <Box sx={{ mt: 2, mb: 2 }}>
            <Typography component="legend">Rating</Typography>
            <Rating
              value={rating}
              onChange={(_, newValue) => setRating(newValue)}
              size="large"
            />
          </Box>
          <TextField
            fullWidth
            label="Review Text"
            multiline
            rows={4}
            value={reviewText}
            onChange={(e) => setReviewText(e.target.value)}
            margin="normal"
            required
          />
          <Button
            type="submit"
            variant="contained"
            sx={{ mt: 3 }}
            disabled={createReviewMutation.isPending}
          >
            {createReviewMutation.isPending ? 'Submitting...' : 'Submit Review'}
          </Button>
        </Box>
      </Paper>
    </AppLayout>
  );
};

export default ReviewsPage;


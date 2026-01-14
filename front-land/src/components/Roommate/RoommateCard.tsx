import React from 'react';
import { Card, CardContent, CardActions, Typography, Chip, Box, Button, Avatar } from '@mui/material';
import {
  LocationOn as LocationIcon,
  Euro as EuroIcon,
  Favorite as FavoriteIcon,
  SmokingRooms as SmokingIcon,
  Pets as PetsIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';

interface RoommateCardProps {
  roommate: any;
  matchScore?: number;
}

const RoommateCard: React.FC<RoommateCardProps> = ({ roommate, matchScore }) => {
  const navigate = useNavigate();

  const getMatchColor = (score: number) => {
    if (score >= 80) return 'success';
    if (score >= 60) return 'info';
    if (score >= 40) return 'warning';
    return 'default';
  };

  const getMatchQuality = (score: number) => {
    if (score >= 80) return 'Excellent Match';
    if (score >= 60) return 'Good Match';
    if (score >= 40) return 'Fair Match';
    return 'Low Match';
  };

  return (
    <Card sx={{ height: '100%', display: 'flex', flexDirection: 'column', position: 'relative' }}>
      {matchScore !== undefined && (
        <Box
          sx={{
            position: 'absolute',
            top: 10,
            right: 10,
            zIndex: 1,
          }}
        >
          <Chip
            label={`${matchScore.toFixed(0)}% Match`}
            color={getMatchColor(matchScore)}
            size="small"
            icon={<FavoriteIcon />}
            sx={{ fontWeight: 'bold' }}
          />
        </Box>
      )}

      <CardContent sx={{ flexGrow: 1 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', mb: 2 }}>
          <Avatar sx={{ width: 56, height: 56, mr: 2, bgcolor: 'primary.main' }}>
            {roommate.user?.firstName?.[0] || 'R'}
          </Avatar>
          <Box>
            <Typography variant="h6">
              {roommate.user?.firstName || 'Roommate'} {roommate.user?.lastName?.[0] || ''}
            </Typography>
            {roommate.profession && (
              <Typography variant="body2" color="text.secondary">
                {roommate.profession}
              </Typography>
            )}
          </Box>
        </Box>

        {matchScore !== undefined && (
          <Chip
            label={getMatchQuality(matchScore)}
            size="small"
            variant="outlined"
            sx={{ mb: 2 }}
          />
        )}

        {roommate.bio && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }} noWrap>
            {roommate.bio.substring(0, 100)}...
          </Typography>
        )}

        <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1, mb: 1 }}>
          {roommate.preferredLocation && (
            <Chip
              icon={<LocationIcon />}
              label={roommate.preferredLocation}
              size="small"
              variant="outlined"
            />
          )}
          {(roommate.budgetMin || roommate.budgetMax) && (
            <Chip
              icon={<EuroIcon />}
              label={`€${roommate.budgetMin || 0} - €${roommate.budgetMax || 0}`}
              size="small"
              variant="outlined"
            />
          )}
        </Box>

        <Box sx={{ display: 'flex', gap: 1, mt: 2 }}>
          {roommate.smokingAllowed && (
            <Chip icon={<SmokingIcon />} label="Smoking OK" size="small" color="default" />
          )}
          {roommate.petFriendly && (
            <Chip icon={<PetsIcon />} label="Pet Friendly" size="small" color="default" />
          )}
        </Box>

        {roommate.lifestyle && (
          <Typography variant="caption" color="text.secondary" display="block" sx={{ mt: 1 }}>
            Lifestyle: {roommate.lifestyle}
          </Typography>
        )}
      </CardContent>

      <CardActions>
        <Button
          size="small"
          fullWidth
          variant="outlined"
          onClick={() => navigate(`/roommates/${roommate.roommateId}`)}
        >
          View Profile
        </Button>
      </CardActions>
    </Card>
  );
};

export default RoommateCard;

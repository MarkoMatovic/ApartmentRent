import React from 'react';
import {
  Card,
  CardContent,
  CardMedia,
  Typography,
  Box,
  Chip,
  Button,
  Avatar,
} from '@mui/material';
import { Person as PersonIcon, LocationOn as LocationIcon, Euro as EuroIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { Roommate } from '../../shared/types/roommate';

interface RoommateCardProps {
  roommate: Roommate;
}

const RoommateCard: React.FC<RoommateCardProps> = ({ roommate }) => {
  const { t } = useTranslation(['common', 'roommates']);
  const navigate = useNavigate();

  const age = roommate.dateOfBirth
    ? new Date().getFullYear() - new Date(roommate.dateOfBirth).getFullYear()
    : null;

  return (
    <Card
      sx={{
        height: '100%',
        display: 'flex',
        flexDirection: 'column',
        cursor: 'pointer',
        transition: 'transform 0.2s',
        '&:hover': {
          transform: 'translateY(-4px)',
          boxShadow: 4,
        },
      }}
      onClick={() => navigate(`/roommates/${roommate.userId}`)}
    >
      <CardMedia
        component="div"
        sx={{
          height: 200,
          bgcolor: 'grey.300',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        {roommate.profilePicture ? (
          <Avatar src={roommate.profilePicture} sx={{ width: 100, height: 100 }} />
        ) : (
          <PersonIcon sx={{ fontSize: 80, color: 'grey.500' }} />
        )}
      </CardMedia>
      <CardContent sx={{ flexGrow: 1 }}>
        <Typography variant="h6" component="h2" gutterBottom>
          {roommate.firstName} {roommate.lastName}
          {age && `, ${age}`}
        </Typography>
        {roommate.preferredLocation && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <LocationIcon fontSize="small" color="action" />
            <Typography variant="body2" color="text.secondary">
              {roommate.preferredLocation}
            </Typography>
          </Box>
        )}
        {roommate.budgetMin && roommate.budgetMax && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
            <EuroIcon fontSize="small" color="action" />
            <Typography variant="body2" color="text.secondary">
              €{roommate.budgetMin} - €{roommate.budgetMax}/mo
            </Typography>
          </Box>
        )}
        {roommate.bio && (
          <Typography variant="body2" color="text.secondary" sx={{ mb: 1, overflow: 'hidden', textOverflow: 'ellipsis', display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical' }}>
            {roommate.bio}
          </Typography>
        )}
        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 1 }}>
          {roommate.smokingAllowed && (
            <Chip label={t('roommates:smokingAllowed')} size="small" />
          )}
          {roommate.petFriendly && (
            <Chip label={t('roommates:petFriendly')} size="small" color="success" />
          )}
          {roommate.lifestyle && (
            <Chip label={t(`roommates:${roommate.lifestyle}`)} size="small" />
          )}
        </Box>
      </CardContent>
      <Box sx={{ p: 2, pt: 0 }}>
        <Button
          fullWidth
          variant="contained"
          color="secondary"
          onClick={(e) => {
            e.stopPropagation();
            navigate(`/roommates/${roommate.userId}`);
          }}
        >
          {t('roommates:contact')}
        </Button>
      </Box>
    </Card>
  );
};

export default RoommateCard;


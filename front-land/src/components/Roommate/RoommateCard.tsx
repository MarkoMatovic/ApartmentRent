import React from 'react';
import {
  Card, CardActionArea, Box, Typography, Avatar, Chip,
  LinearProgress, Tooltip,
} from '@mui/material';
import {
  LocationOn as LocationIcon,
  Euro as EuroIcon,
  CalendarToday as CalIcon,
  SmokingRooms as SmokingIcon,
  SmokeFree as NoSmokingIcon,
  Pets as PetsIcon,
  MusicNote as MusicIcon,
  Favorite as HeartIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Roommate,
  getAge, formatAvailableFrom,
  LIFESTYLE_ICONS, LIFESTYLE_KEYS,
  SCHEDULE_ICONS, SCHEDULE_KEYS,
  GENDER_KEYS,
} from '../../shared/types/roommate';

interface Props {
  roommate: Roommate;
  matchScore?: number;
  isOwn?: boolean;
}

const TagChips: React.FC<{ value?: string; max?: number }> = ({ value, max = 4 }) => {
  if (!value) return null;
  const items = value.split(',').map(s => s.trim()).filter(Boolean).slice(0, max);
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5 }}>
      {items.map(item => (
        <Chip key={item} label={item} size="small"
          sx={{ fontSize: '0.68rem', height: 20, bgcolor: 'action.hover' }} />
      ))}
    </Box>
  );
};

const RoommateCard: React.FC<Props> = ({ roommate, matchScore, isOwn }) => {
  const navigate = useNavigate();
  const { t } = useTranslation('roommates');
  const age = getAge(roommate.dateOfBirth);
  const availableFrom = formatAvailableFrom(roommate.availableFrom);

  // Profile completeness score
  const fields = [
    roommate.bio, roommate.profession, roommate.hobbies,
    roommate.preferredLocation, roommate.languages,
    roommate.lifestyle, roommate.cleanliness,
    roommate.budgetMin, roommate.availableFrom,
    roommate.gender !== undefined && roommate.gender !== 0,
    roommate.workSchedule !== undefined,
    roommate.profilePicture,
  ];
  const completeness = Math.round((fields.filter(Boolean).length / fields.length) * 100);

  const matchColor = matchScore !== undefined
    ? matchScore >= 80 ? '#4caf50' : matchScore >= 60 ? '#2196f3' : matchScore >= 40 ? '#ff9800' : '#9e9e9e'
    : undefined;

  return (
    <Card sx={{
      borderRadius: 3, overflow: 'hidden', height: '100%',
      display: 'flex', flexDirection: 'column', position: 'relative',
      border: matchScore && matchScore >= 80 ? '2px solid #4caf50' : undefined,
      transition: 'transform 0.15s, box-shadow 0.15s',
      '&:hover': { transform: 'translateY(-4px)', boxShadow: 6 },
    }}>
      {/* Badges */}
      <Box sx={{ position: 'absolute', top: 10, left: 10, zIndex: 2, display: 'flex', gap: 0.5, flexDirection: 'column' }}>
        {isOwn && (
          <Chip label={t('myProfileBadge')} size="small" color="primary" sx={{ fontWeight: 700, fontSize: '0.7rem' }} />
        )}
      </Box>
      {matchScore !== undefined && (
        <Box sx={{ position: 'absolute', top: 10, right: 10, zIndex: 2 }}>
          <Chip
            icon={<HeartIcon sx={{ fontSize: '0.9rem !important', color: matchColor + ' !important' }} />}
            label={`${matchScore.toFixed(0)}%`} size="small"
            sx={{ bgcolor: 'background.paper', fontWeight: 800, fontSize: '0.78rem', border: `2px solid ${matchColor}`, color: matchColor }}
          />
        </Box>
      )}

      <CardActionArea onClick={() => navigate(`/roommates/${roommate.roommateId}`)}
        sx={{ flexGrow: 1, display: 'flex', flexDirection: 'column', alignItems: 'stretch' }}>

        {/* Header */}
        <Box sx={{
          background: 'linear-gradient(135deg, #1C3C58 0%, #305B7A 100%)',
          px: 2, pt: 3.5, pb: 2, display: 'flex', gap: 2, alignItems: 'center',
        }}>
          <Avatar src={roommate.profilePicture || undefined}
            sx={{ width: 72, height: 72, border: '3px solid rgba(255,255,255,0.3)', flexShrink: 0 }}>
            {(roommate.firstName?.[0] || '?').toUpperCase()}
          </Avatar>
          <Box sx={{ flex: 1, minWidth: 0 }}>
            <Typography variant="h6" fontWeight="bold" noWrap sx={{ color: '#fff' }}>
              {roommate.firstName}{age ? `, ${age}` : ''}
              {roommate.gender !== undefined && roommate.gender !== 0 && (
                <Typography component="span" sx={{ color: 'rgba(255,255,255,0.6)', fontSize: '0.8rem', ml: 0.5 }}>
                  · {t(GENDER_KEYS[roommate.gender as number])}
                </Typography>
              )}
            </Typography>
            {roommate.profession && (
              <Typography variant="body2" sx={{ color: 'rgba(255,255,255,0.75)' }} noWrap>
                {roommate.profession}
              </Typography>
            )}
            <Typography variant="caption" sx={{ color: 'rgba(255,255,255,0.55)' }}>
              {roommate.lifestyle && `${LIFESTYLE_ICONS[roommate.lifestyle]} ${t(LIFESTYLE_KEYS[roommate.lifestyle])}`}
              {roommate.lifestyle && roommate.workSchedule !== undefined && ' · '}
              {roommate.workSchedule !== undefined && `${SCHEDULE_ICONS[roommate.workSchedule as number]} ${t(SCHEDULE_KEYS[roommate.workSchedule as number])}`}
            </Typography>
          </Box>
        </Box>

        {/* Key info strip */}
        <Box sx={{ px: 2, py: 1.2, bgcolor: 'primary.main', display: 'flex', gap: 2, flexWrap: 'wrap' }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: '#fff' }}>
            <CalIcon sx={{ fontSize: 14 }} />
            <Typography variant="caption" fontWeight="bold">
              {availableFrom === 'Odmah'
                ? <span style={{ color: '#a5d6a7' }}>{t('availableNowFull')}</span>
                : availableFrom}
            </Typography>
          </Box>
          {(roommate.budgetMin || roommate.budgetMax) && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, color: '#fff' }}>
              <EuroIcon sx={{ fontSize: 14 }} />
              <Typography variant="caption" fontWeight="bold">
                {roommate.budgetMin && roommate.budgetMax
                  ? `${roommate.budgetMin}–${roommate.budgetMax}`
                  : roommate.budgetMin ? `${t('budgetMinPrefix')} ${roommate.budgetMin}` : `${t('budgetMaxPrefix')} ${roommate.budgetMax}`}/{t('monthAbbr')}
              </Typography>
            </Box>
          )}
        </Box>

        {/* Body */}
        <Box sx={{ px: 2, py: 1.5, flexGrow: 1, display: 'flex', flexDirection: 'column', gap: 1 }}>
          {roommate.preferredLocation && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <LocationIcon sx={{ fontSize: 14, color: 'text.secondary' }} />
              <Typography variant="body2" color="text.secondary" noWrap>{roommate.preferredLocation}</Typography>
            </Box>
          )}
          {roommate.bio && (
            <Typography variant="body2" color="text.secondary"
              sx={{ display: '-webkit-box', WebkitLineClamp: 2, WebkitBoxOrient: 'vertical', overflow: 'hidden', lineHeight: 1.4 }}>
              {roommate.bio}
            </Typography>
          )}
          {roommate.hobbies && <TagChips value={roommate.hobbies} max={4} />}
          {roommate.languages && (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
              <Typography variant="caption">🌐</Typography>
              <TagChips value={roommate.languages} max={3} />
            </Box>
          )}
          {/* Preference icons */}
          <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap', mt: 'auto', pt: 0.5 }}>
            {roommate.smokingAllowed === true && (
              <Tooltip title={t('smokingOkTooltip')}><Chip icon={<SmokingIcon />} label={t('smokingOkLabel')} size="small" variant="outlined" sx={{ fontSize: '0.68rem', height: 22 }} /></Tooltip>
            )}
            {roommate.smokingAllowed === false && (
              <Tooltip title={t('nonSmokerTooltip')}><Chip icon={<NoSmokingIcon />} label={t('nonSmokerLabel')} size="small" variant="outlined" sx={{ fontSize: '0.68rem', height: 22 }} /></Tooltip>
            )}
            {roommate.petFriendly && (
              <Tooltip title={t('petsOkTooltip')}><Chip icon={<PetsIcon />} label={t('petsOkLabel')} size="small" variant="outlined" sx={{ fontSize: '0.68rem', height: 22 }} /></Tooltip>
            )}
            {roommate.musicFriendly && (
              <Tooltip title={t('musicOkTooltip')}><Chip icon={<MusicIcon />} label={t('musicOkLabel')} size="small" variant="outlined" sx={{ fontSize: '0.68rem', height: 22 }} /></Tooltip>
            )}
            {roommate.guestsAllowed && (
              <Tooltip title={t('guestsOkTooltip')}><Chip label={t('guestsOkLabel')} size="small" variant="outlined" sx={{ fontSize: '0.68rem', height: 22 }} /></Tooltip>
            )}
          </Box>
        </Box>

        {/* Completeness bar (own profile) */}
        {isOwn && (
          <Box sx={{ px: 2, pb: 1.5 }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.3 }}>
              <Typography variant="caption" color="text.secondary">{t('profileCompleteness')}</Typography>
              <Typography variant="caption" fontWeight="bold" color={completeness >= 80 ? 'success.main' : 'warning.main'}>
                {completeness}%
              </Typography>
            </Box>
            <LinearProgress variant="determinate" value={completeness}
              color={completeness >= 80 ? 'success' : completeness >= 50 ? 'warning' : 'error'}
              sx={{ borderRadius: 4, height: 5 }} />
          </Box>
        )}
      </CardActionArea>
    </Card>
  );
};

export default RoommateCard;

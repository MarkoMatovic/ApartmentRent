import React, { useState } from 'react';
import {
  Container,
  Typography,
  Box,
  Grid,
  Card,
  CardContent,
  CardActions,
  Button,
  Avatar,
  Chip,
  CircularProgress,
  Alert,
  LinearProgress,
  Paper,
  Tooltip,
} from '@mui/material';
import {
  Person as PersonIcon,
  Message as MessageIcon,
  Psychology as PsychologyIcon,
  Visibility as VisibilityIcon,
} from '@mui/icons-material';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';
import { machineLearningApi, RoommateMatchScore } from '../shared/api/machineLearning';

/** `percent` is 0-100, as the server sends it. */
const getScoreColor = (percent: number): 'success' | 'primary' | 'warning' | 'error' => {
  if (percent >= 80) return 'success';
  if (percent >= 60) return 'primary';
  if (percent >= 40) return 'warning';
  return 'error';
};

// The four parts of the score the server reports, and the text that names each of them.
const FACTORS = [
  ['Lifestyle', 'lifestyleFactor'],
  ['Budget', 'budgetFactor'],
  ['Preferences', 'preferencesFactor'],
  ['Location', 'locationFactor'],
] as const;

const RoommateMatchesPage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);
  const navigate = useNavigate();
  const { user } = useAuth();
  const [topN] = useState(10);

  const {
    data: matches,
    isLoading,
    error,
    refetch,
  } = useQuery<RoommateMatchScore[]>({
    queryKey: ['roommate-matches', user?.userId, topN],
    queryFn: () => machineLearningApi.getRoommateMatches(user!.userId, topN),
    enabled: !!user?.userId,
    retry: 1,
  });

  if (!user) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Alert severity="warning">
          {t('common:loginRequired', { defaultValue: 'Please log in to view roommate matches.' })}
        </Alert>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Box sx={{ mb: 4 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 1 }}>
          <PsychologyIcon color="primary" sx={{ fontSize: 36 }} />
          <Typography variant="h4" component="h1">
            {t('roommates:matches', { defaultValue: 'Roommate Matches' })}
          </Typography>
        </Box>
        <Typography variant="body1" color="text.secondary">
          {t('roommates:matchesDesc', {
            defaultValue: 'AI-powered compatibility matches based on your lifestyle, budget, and preferences.',
          })}
        </Typography>
      </Box>

      {isLoading && (
        <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}>
          <CircularProgress size={48} />
        </Box>
      )}

      {error && (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Alert severity="info" sx={{ mb: 2 }}>
            {t('roommates:matchesUnavailable', {
              defaultValue:
                'Roommate matching requires a roommate profile. Create one to see your matches.',
            })}
          </Alert>
          <Button
            variant="contained"
            color="secondary"
            onClick={() => navigate('/roommates/create')}
            sx={{ mt: 1 }}
          >
            {t('roommates:createProfile', { defaultValue: 'Create Roommate Profile' })}
          </Button>
        </Paper>
      )}

      {!isLoading && !error && matches && matches.length === 0 && (
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <PersonIcon sx={{ fontSize: 64, color: 'text.disabled', mb: 2 }} />
          <Typography variant="h6" color="text.secondary" gutterBottom>
            {t('roommates:noMatches', { defaultValue: 'No matches found yet.' })}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {t('roommates:noMatchesHint', {
              defaultValue: 'Complete your roommate profile with more details to improve matching.',
            })}
          </Typography>
          <Button
            variant="outlined"
            onClick={() => navigate('/roommates/create')}
            sx={{ mt: 2 }}
          >
            {t('roommates:editProfile', { defaultValue: 'Edit Roommate Profile' })}
          </Button>
        </Paper>
      )}

      {!isLoading && !error && matches && matches.length > 0 && (
        <>
          <Typography variant="body2" color="text.secondary" sx={{ mb: 3 }}>
            {t('roommates:matchesFound', { count: matches.length })}
          </Typography>
          <Grid container spacing={3}>
            {matches.map((match, index) => {
              const roommate = match.roommate;
              const scorePercent = Math.round(match.matchPercentage);
              const color = getScoreColor(match.matchPercentage);

              return (
                <Grid item xs={12} sm={6} md={4} key={match.roommateId}>
                  <Card
                    sx={{
                      height: '100%',
                      display: 'flex',
                      flexDirection: 'column',
                      transition: 'all 0.3s ease',
                      '&:hover': { boxShadow: 4, transform: 'translateY(-4px)' },
                    }}
                  >
                    <CardContent sx={{ flexGrow: 1 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2, mb: 2 }}>
                        <Box sx={{ position: 'relative' }}>
                          <Avatar
                            src={roommate?.profilePicture || undefined}
                            alt={roommate?.firstName}
                            sx={{ width: 56, height: 56, bgcolor: `${color}.light` }}
                          >
                            {roommate?.firstName ? roommate.firstName[0].toUpperCase() : <PersonIcon />}
                          </Avatar>
                          {index < 3 && (
                            <Chip
                              label={`#${index + 1}`}
                              size="small"
                              color={index === 0 ? 'success' : index === 1 ? 'primary' : 'default'}
                              sx={{
                                position: 'absolute',
                                top: -8,
                                right: -8,
                                height: 20,
                                fontSize: '0.65rem',
                              }}
                            />
                          )}
                        </Box>
                        <Box>
                          <Typography variant="subtitle1" fontWeight={600}>
                            {roommate?.firstName}
                          </Typography>
                          <Tooltip title={t('roommates:compatibilityTooltip', { percent: scorePercent })}>
                            <Chip
                              label={`${scorePercent}%`}
                              size="small"
                              color={color}
                              variant="outlined"
                            />
                          </Tooltip>
                        </Box>
                      </Box>

                      <Box sx={{ mb: 2 }}>
                        <Box sx={{ display: 'flex', justifyContent: 'space-between', mb: 0.5 }}>
                          <Typography variant="caption" color="text.secondary">
                            {t('roommates:compatibility', { defaultValue: 'Compatibility' })}
                          </Typography>
                          <Typography variant="caption" fontWeight={600} color={`${color}.main`}>
                            {scorePercent}%
                          </Typography>
                        </Box>
                        <LinearProgress
                          variant="determinate"
                          value={scorePercent}
                          color={color}
                          sx={{ borderRadius: 4, height: 6 }}
                        />
                      </Box>

                      {match.featureScores && (
                        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                          {FACTORS.map(([factor, labelKey]) => {
                            const value = match.featureScores?.[factor];
                            return value === undefined ? null : (
                              <Chip
                                key={factor}
                                label={t(`roommates:${labelKey}`, { percent: Math.round(value) })}
                                size="small"
                                variant="outlined"
                              />
                            );
                          })}
                        </Box>
                      )}
                    </CardContent>

                    <CardActions sx={{ gap: 1, px: 2, pb: 2 }}>
                      <Button
                        size="small"
                        variant="outlined"
                        startIcon={<VisibilityIcon />}
                        onClick={() => navigate(`/roommates/${match.roommateId}`)}
                      >
                        {t('common:view', { defaultValue: 'View' })}
                      </Button>
                      <Button
                        size="small"
                        variant="contained"
                        color="secondary"
                        startIcon={<MessageIcon />}
                        disabled={!roommate}
                        onClick={() => roommate && navigate(`/chat?userId=${roommate.userId}`)}
                      >
                        {t('roommates:contact', { defaultValue: 'Contact' })}
                      </Button>
                    </CardActions>
                  </Card>
                </Grid>
              );
            })}
          </Grid>

          <Box sx={{ mt: 3, display: 'flex', justifyContent: 'center' }}>
            <Button variant="outlined" onClick={() => refetch()}>
              {t('roommates:refreshMatches')}
            </Button>
          </Box>
        </>
      )}
    </Container>
  );
};

export default RoommateMatchesPage;

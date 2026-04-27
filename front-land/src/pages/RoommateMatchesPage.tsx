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

const getScoreColor = (score: number): 'success' | 'primary' | 'warning' | 'error' => {
  if (score >= 0.8) return 'success';
  if (score >= 0.6) return 'primary';
  if (score >= 0.4) return 'warning';
  return 'error';
};

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
            {t('roommates:matchesFound', {
              defaultValue: `Found ${matches.length} compatible roommates`,
              count: matches.length,
            })}
          </Typography>
          <Grid container spacing={3}>
            {matches.map((match, index) => {
              const scorePercent = Math.round(match.matchScore * 100);
              const color = getScoreColor(match.matchScore);

              return (
                <Grid item xs={12} sm={6} md={4} key={match.userId}>
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
                          <Avatar sx={{ width: 56, height: 56, bgcolor: `${color}.light` }}>
                            <PersonIcon />
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
                            {match.userName}
                          </Typography>
                          <Tooltip title={`${scorePercent}% compatibility`}>
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

                      {match.compatibilityFactors && (
                        <Box sx={{ display: 'flex', gap: 0.5, flexWrap: 'wrap' }}>
                          {match.compatibilityFactors.lifestyle !== undefined && (
                            <Chip
                              label={`Lifestyle ${Math.round(match.compatibilityFactors.lifestyle * 100)}%`}
                              size="small"
                              variant="outlined"
                            />
                          )}
                          {match.compatibilityFactors.budget !== undefined && (
                            <Chip
                              label={`Budget ${Math.round(match.compatibilityFactors.budget * 100)}%`}
                              size="small"
                              variant="outlined"
                            />
                          )}
                          {match.compatibilityFactors.cleanliness !== undefined && (
                            <Chip
                              label={`Cleanliness ${Math.round(match.compatibilityFactors.cleanliness * 100)}%`}
                              size="small"
                              variant="outlined"
                            />
                          )}
                        </Box>
                      )}
                    </CardContent>

                    <CardActions sx={{ gap: 1, px: 2, pb: 2 }}>
                      <Button
                        size="small"
                        variant="outlined"
                        startIcon={<VisibilityIcon />}
                        onClick={() => navigate(`/roommates/${match.userId}`)}
                      >
                        {t('common:view', { defaultValue: 'View' })}
                      </Button>
                      <Button
                        size="small"
                        variant="contained"
                        color="secondary"
                        startIcon={<MessageIcon />}
                        onClick={() => navigate(`/messages?userId=${match.userId}`)}
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
              {t('common:refresh', { defaultValue: 'Refresh Matches' })}
            </Button>
          </Box>
        </>
      )}
    </Container>
  );
};

export default RoommateMatchesPage;

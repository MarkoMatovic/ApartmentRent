import React, { useState } from 'react';
import {
  Container,
  Paper,
  TextField,
  Button,
  Typography,
  Box,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Grid,
  Alert,
} from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { roommatesApi, RoommateInputDto } from '../shared/api/roommates';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';

const CreateRoommatePage: React.FC = () => {
  const { t } = useTranslation(['common', 'roommates']);
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);

  const [formData, setFormData] = useState<RoommateInputDto>({
    bio: '',
    hobbies: '',
    profession: '',
    smokingAllowed: undefined,
    petFriendly: undefined,
    lifestyle: '',
    cleanliness: '',
    guestsAllowed: undefined,
    budgetMin: undefined,
    budgetMax: undefined,
    budgetIncludes: '',
    availableFrom: undefined,
    availableUntil: undefined,
    minimumStayMonths: undefined,
    maximumStayMonths: undefined,
    lookingForRoomType: '',
    lookingForApartmentType: '',
    preferredLocation: '',
    lookingForApartmentId: undefined,
  });

  const createMutation = useMutation({
    mutationFn: (data: RoommateInputDto) => roommatesApi.create(data),
    onSuccess: () => {
      setSuccess(true);
      queryClient.invalidateQueries({ queryKey: ['roommates'] });
      setTimeout(() => {
        navigate('/roommates');
      }, 2000);
    },
    onError: (err: any) => {
      setError(err.response?.data || 'Failed to create roommate profile');
    },
  });

  const handleChange = (field: keyof RoommateInputDto, value: any) => {
    setFormData({ ...formData, [field]: value });
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    createMutation.mutate(formData);
  };

  if (success) {
    return (
      <Container maxWidth="md" sx={{ py: 8 }}>
        <Paper sx={{ p: 4 }}>
          <Alert severity="success" sx={{ mb: 2 }}>
            {t('roommates:profileCreated', { defaultValue: 'Roommate profile created successfully!' })}
          </Alert>
        </Paper>
      </Container>
    );
  }

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Container maxWidth="md" sx={{ py: 4 }}>
        <Paper elevation={3} sx={{ p: 4 }}>
          <Typography variant="h4" component="h1" gutterBottom align="center">
            {t('roommates:createProfile', { defaultValue: 'Create Roommate Profile' })}
          </Typography>

          {error && (
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
          )}

          <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label={t('roommates:bio')}
                  multiline
                  rows={4}
                  value={formData.bio}
                  onChange={(e) => handleChange('bio', e.target.value)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={t('roommates:hobbies', { defaultValue: 'Hobbies' })}
                  value={formData.hobbies}
                  onChange={(e) => handleChange('hobbies', e.target.value)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={t('roommates:profession', { defaultValue: 'Profession' })}
                  value={formData.profession}
                  onChange={(e) => handleChange('profession', e.target.value)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={`${t('roommates:minBudget')} (€)`}
                  type="number"
                  value={formData.budgetMin || ''}
                  onChange={(e) => handleChange('budgetMin', e.target.value ? parseFloat(e.target.value) : undefined)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={`${t('roommates:maxBudget')} (€)`}
                  type="number"
                  value={formData.budgetMax || ''}
                  onChange={(e) => handleChange('budgetMax', e.target.value ? parseFloat(e.target.value) : undefined)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label={t('roommates:budgetIncludes', { defaultValue: 'Budget Includes' })}
                  value={formData.budgetIncludes}
                  onChange={(e) => handleChange('budgetIncludes', e.target.value)}
                  margin="normal"
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <DatePicker
                  label={t('roommates:availableFrom')}
                  value={formData.availableFrom ? new Date(formData.availableFrom) : null}
                  onChange={(newValue) => handleChange('availableFrom', newValue?.toISOString().split('T')[0])}
                  slotProps={{
                    textField: {
                      fullWidth: true,
                      margin: 'normal',
                    },
                  }}
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <DatePicker
                  label={t('roommates:availableUntil')}
                  value={formData.availableUntil ? new Date(formData.availableUntil) : null}
                  onChange={(newValue) => handleChange('availableUntil', newValue?.toISOString().split('T')[0])}
                  slotProps={{
                    textField: {
                      fullWidth: true,
                      margin: 'normal',
                    },
                  }}
                />
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth margin="normal">
                  <InputLabel>{t('roommates:smokingAllowed')}</InputLabel>
                  <Select
                    value={formData.smokingAllowed === undefined ? '' : formData.smokingAllowed.toString()}
                    onChange={(e) => handleChange('smokingAllowed', e.target.value === '' ? undefined : e.target.value === 'true')}
                    label={t('roommates:smokingAllowed')}
                  >
                    <MenuItem value="">{t('common:cancel')}</MenuItem>
                    <MenuItem value="true">Yes</MenuItem>
                    <MenuItem value="false">No</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth margin="normal">
                  <InputLabel>{t('roommates:petFriendly')}</InputLabel>
                  <Select
                    value={formData.petFriendly === undefined ? '' : formData.petFriendly.toString()}
                    onChange={(e) => handleChange('petFriendly', e.target.value === '' ? undefined : e.target.value === 'true')}
                    label={t('roommates:petFriendly')}
                  >
                    <MenuItem value="">{t('common:cancel')}</MenuItem>
                    <MenuItem value="true">Yes</MenuItem>
                    <MenuItem value="false">No</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth margin="normal">
                  <InputLabel>{t('roommates:lifestyle')}</InputLabel>
                  <Select
                    value={formData.lifestyle || ''}
                    onChange={(e) => handleChange('lifestyle', e.target.value || undefined)}
                    label={t('roommates:lifestyle')}
                  >
                    <MenuItem value="">{t('common:cancel')}</MenuItem>
                    <MenuItem value="quiet">{t('roommates:quiet')}</MenuItem>
                    <MenuItem value="social">{t('roommates:social')}</MenuItem>
                    <MenuItem value="mixed">{t('roommates:mixed')}</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12} sm={6}>
                <FormControl fullWidth margin="normal">
                  <InputLabel>{t('roommates:cleanliness')}</InputLabel>
                  <Select
                    value={formData.cleanliness || ''}
                    onChange={(e) => handleChange('cleanliness', e.target.value || undefined)}
                    label={t('roommates:cleanliness')}
                  >
                    <MenuItem value="">{t('common:cancel')}</MenuItem>
                    <MenuItem value="veryClean">{t('roommates:veryClean')}</MenuItem>
                    <MenuItem value="clean">{t('roommates:clean')}</MenuItem>
                    <MenuItem value="moderate">{t('roommates:moderate')}</MenuItem>
                  </Select>
                </FormControl>
              </Grid>

              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label={t('roommates:preferredLocation')}
                  value={formData.preferredLocation}
                  onChange={(e) => handleChange('preferredLocation', e.target.value)}
                  margin="normal"
                />
              </Grid>
            </Grid>

            <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
              <Button
                type="submit"
                variant="contained"
                color="secondary"
                size="large"
                disabled={createMutation.isPending}
              >
                {createMutation.isPending ? t('loading') : t('common:save')}
              </Button>
              <Button
                variant="outlined"
                size="large"
                onClick={() => navigate('/roommates')}
              >
                {t('common:cancel')}
              </Button>
            </Box>
          </Box>
        </Paper>
      </Container>
    </LocalizationProvider>
  );
};

export default CreateRoommatePage;


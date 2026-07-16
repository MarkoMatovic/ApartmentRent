import React, { useState, useEffect } from 'react';
import {
  Container, Paper, TextField, Button, Typography, Box,
  FormControl, InputLabel, Select, MenuItem, Grid, Alert,
  CircularProgress, Chip, Divider, ToggleButtonGroup, ToggleButton,
  LinearProgress,
} from '@mui/material';
import {
  Person as PersonIcon, Home as HomeIcon,
  Favorite as PrefsIcon, Euro as EuroIcon, Check as CheckIcon,
} from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient, useQuery } from '@tanstack/react-query';
import { roommatesApi, RoommateInputDto } from '../shared/api/roommates';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { useAuth } from '../shared/context/AuthContext';

// ── Helpers ───────────────────────────────────────────────────────────────────

const YesNoToggle: React.FC<{
  label: string; value?: boolean; onChange: (v: boolean | undefined) => void;
}> = ({ label, value, onChange }) => {
  const { t } = useTranslation('roommates');
  return (
    <Box>
      <Typography variant="caption" color="text.secondary" display="block" mb={0.5}>{label}</Typography>
      <ToggleButtonGroup value={value === undefined ? 'any' : String(value)} exclusive size="small"
        onChange={(_, v) => onChange(v === 'any' ? undefined : v === 'true')}>
        <ToggleButton value="true"  sx={{ minWidth: 60 }}>{t('yesToggle')}</ToggleButton>
        <ToggleButton value="any"   sx={{ minWidth: 60 }}>{t('anyToggle')}</ToggleButton>
        <ToggleButton value="false" sx={{ minWidth: 60 }}>{t('noToggle')}</ToggleButton>
      </ToggleButtonGroup>
    </Box>
  );
};

const SectionHeader: React.FC<{ icon: React.ReactNode; title: string; subtitle?: string }> = ({ icon, title, subtitle }) => (
  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2.5 }}>
    <Box sx={{ color: 'primary.main', display: 'flex' }}>{icon}</Box>
    <Box>
      <Typography variant="h6" fontWeight="bold">{title}</Typography>
      {subtitle && <Typography variant="body2" color="text.secondary">{subtitle}</Typography>}
    </Box>
  </Box>
);

const POPULAR_LANGS = ['Srpski', 'Engleski', 'Nemački', 'Francuski', 'Italijanski', 'Španski', 'Mađarski', 'Rumunski'];

const LanguageSelector: React.FC<{ value?: string; onChange: (v: string) => void }> = ({ value, onChange }) => {
  const selected = value ? value.split(',').map(s => s.trim()).filter(Boolean) : [];
  const toggle = (lang: string) => {
    const next = selected.includes(lang) ? selected.filter(l => l !== lang) : [...selected, lang];
    onChange(next.join(','));
  };
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.8 }}>
      {POPULAR_LANGS.map(lang => (
        <Chip key={lang} label={lang} clickable size="small"
          color={selected.includes(lang) ? 'primary' : 'default'}
          variant={selected.includes(lang) ? 'filled' : 'outlined'}
          icon={selected.includes(lang) ? <CheckIcon style={{ fontSize: 14 }} /> : undefined}
          onClick={() => toggle(lang)} />
      ))}
    </Box>
  );
};

// ── Component ─────────────────────────────────────────────────────────────────
const CreateRoommatePage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation('roommates');
  const queryClient = useQueryClient();
  const { user } = useAuth();
  const [error, setError] = useState('');
  const [success, setSuccess] = useState(false);
  const [isEdit, setIsEdit] = useState(false);

  // ── Constants built with t() ──────────────────────────────────────────────
  const LIFESTYLE_OPTIONS = [
    { value: 'quiet',  label: `📚 ${t('lifestyleQuiet')}`,  desc: t('lifestyleQuietDesc') },
    { value: 'social', label: `🎉 ${t('lifestyleSocial')}`, desc: t('lifestyleSocialDesc') },
    { value: 'mixed',  label: `☕ ${t('lifestyleMixed')}`,  desc: t('lifestyleMixedDesc') },
  ];

  const CLEANLINESS_OPTIONS = [
    { value: 'veryClean', label: t('cleanlinessVeryClean') },
    { value: 'clean',     label: t('cleanlinessClean') },
    { value: 'moderate',  label: t('cleanlinessModerate') },
  ];

  const SCHEDULE_OPTIONS = [
    { value: 0, label: `🔄 ${t('scheduleFlexible')}` },
    { value: 1, label: `🌅 ${t('scheduleMorning')}` },
    { value: 2, label: `🌇 ${t('scheduleEvening')}` },
    { value: 3, label: `🌙 ${t('scheduleNight')}` },
  ];

  const GENDER_OPTIONS = [
    { value: 0, label: t('genderNotSay') },
    { value: 1, label: t('genderMale') },
    { value: 2, label: t('genderFemale') },
    { value: 3, label: t('genderNonBinary') },
    { value: 4, label: t('genderOther') },
  ];

  const ROOM_TYPES = [
    { value: 'Privatna soba', label: t('roomTypePrivate') },
    { value: 'Deljenje sobe', label: t('roomTypeShared') },
    { value: 'Garsonjera',    label: t('roomTypeStudio1') },
    { value: 'Studio',        label: t('roomTypeStudio2') },
    { value: 'Svejedno',      label: t('roomTypeAny') },
  ];

  const emptyForm: RoommateInputDto = {
    bio: '', hobbies: '', profession: '',
    smokingAllowed: undefined, petFriendly: undefined,
    lifestyle: '', cleanliness: '', guestsAllowed: undefined, musicFriendly: undefined,
    budgetMin: undefined, budgetMax: undefined, budgetIncludes: '',
    availableFrom: undefined, availableUntil: undefined,
    minimumStayMonths: undefined, maximumStayMonths: undefined,
    lookingForRoomType: '', preferredLocation: '',
    gender: 0, languages: '', workSchedule: 0,
  };

  const [form, setForm] = useState<RoommateInputDto>(emptyForm);
  const set = (field: keyof RoommateInputDto, value: any) =>
    setForm(prev => ({ ...prev, [field]: value }));

  const { data: existing, isLoading: loadingExisting } = useQuery({
    queryKey: ['roommate-my', user?.userId],
    queryFn: () => roommatesApi.getByUserId(user!.userId),
    enabled: !!user?.userId, retry: false,
  });

  useEffect(() => {
    if (!existing) return;
    setIsEdit(true);
    setForm({
      bio: existing.bio || '', hobbies: existing.hobbies || '', profession: existing.profession || '',
      smokingAllowed: existing.smokingAllowed, petFriendly: existing.petFriendly,
      lifestyle: existing.lifestyle || '', cleanliness: existing.cleanliness || '',
      guestsAllowed: existing.guestsAllowed, musicFriendly: existing.musicFriendly,
      budgetMin: existing.budgetMin, budgetMax: existing.budgetMax,
      budgetIncludes: existing.budgetIncludes || '',
      availableFrom: existing.availableFrom, availableUntil: existing.availableUntil,
      minimumStayMonths: existing.minimumStayMonths, maximumStayMonths: existing.maximumStayMonths,
      lookingForRoomType: existing.lookingForRoomType || '',
      lookingForApartmentType: existing.lookingForApartmentType || '',
      preferredLocation: existing.preferredLocation || '',
      gender: existing.gender ?? 0, languages: existing.languages || '', workSchedule: existing.workSchedule ?? 0,
    });
  }, [existing]);

  // Completeness
  const fields = [form.bio, form.profession, form.hobbies, form.preferredLocation, form.languages,
    form.lifestyle, form.cleanliness, form.budgetMin, form.availableFrom,
    form.gender !== 0, form.workSchedule !== 0, user?.profilePicture];
  const completeness = Math.round((fields.filter(Boolean).length / fields.length) * 100);

  const mutation = useMutation({
    mutationFn: (data: RoommateInputDto) => roommatesApi.create(data),
    onSuccess: () => {
      setSuccess(true);
      queryClient.invalidateQueries({ queryKey: ['roommates'] });
      queryClient.invalidateQueries({ queryKey: ['roommate-my'] });
      setTimeout(() => navigate('/roommates'), 1800);
    },
    onError: (err: any) =>
      setError(err.response?.data?.message || err.message || t('savingError')),
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    if (form.budgetMin && form.budgetMax && form.budgetMin > form.budgetMax)
      return setError(t('budgetError'));
    mutation.mutate(form);
  };

  if (loadingExisting) return <Box sx={{ display: 'flex', justifyContent: 'center', py: 8 }}><CircularProgress /></Box>;

  if (success) return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper sx={{ p: 4, textAlign: 'center', borderRadius: 3 }}>
        <Typography variant="h5" gutterBottom>
          ✅ {isEdit ? t('profileUpdated') : t('profilePublished')}
        </Typography>
        <Typography color="text.secondary">{t('redirecting')}</Typography>
      </Paper>
    </Container>
  );

  return (
    <LocalizationProvider dateAdapter={AdapterDateFns}>
      <Container maxWidth="md" sx={{ py: 4 }}>
        {/* Header with progress */}
        <Box sx={{ mb: 3 }}>
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
            <Box>
              <Typography variant="h4" fontWeight="bold">
                {isEdit ? t('editProfile') : t('createProfile')}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {t('profileIncompleteHint')}
              </Typography>
            </Box>
            <Box sx={{ textAlign: 'right', ml: 2 }}>
              <Typography variant="caption" fontWeight="bold"
                color={completeness >= 80 ? 'success.main' : completeness >= 50 ? 'warning.main' : 'error.main'}>
                {t('completenessLabel', { percent: completeness })}
              </Typography>
              <LinearProgress variant="determinate" value={completeness}
                color={completeness >= 80 ? 'success' : completeness >= 50 ? 'warning' : 'error'}
                sx={{ width: 120, borderRadius: 4, height: 6, mt: 0.3 }} />
            </Box>
          </Box>
          {isEdit && <Alert severity="info" sx={{ mt: 1 }} icon={false}>{t('editingExisting')}</Alert>}
          {!user?.profilePicture && (
            <Alert severity="warning" sx={{ mt: 1 }}>
              {t('addProfilePhoto')}{' '}
              <Button size="small" href="/profile" sx={{ p: 0, minWidth: 0, textTransform: 'none', verticalAlign: 'baseline' }}>
                {t('profileSettingsLink')}
              </Button>{' '}
              {t('addProfilePhotoSuffix')}
            </Alert>
          )}
        </Box>

        {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}

        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 3 }}>

          {/* ── 1. Ko si ──────────────────────────────────────────────── */}
          <Paper sx={{ p: 3, borderRadius: 3 }}>
            <SectionHeader icon={<PersonIcon />} title={t('whoAreYou')} subtitle={t('whoAreYouSub')} />
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <FormControl fullWidth size="small">
                  <InputLabel>{t('genderLabel')}</InputLabel>
                  <Select value={form.gender ?? 0} onChange={e => set('gender', Number(e.target.value))} label={t('genderLabel')}>
                    {GENDER_OPTIONS.map(o => <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>)}
                  </Select>
                </FormControl>
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label={t('occupationLabel')}
                  placeholder={t('occupationPlaceholder')}
                  value={form.profession || ''} onChange={e => set('profession', e.target.value)} />
              </Grid>
              <Grid item xs={12}>
                <TextField fullWidth multiline rows={4} label={t('bioLabel')}
                  placeholder={t('bioPlaceholder')}
                  inputProps={{ maxLength: 500 }}
                  value={form.bio || ''} onChange={e => set('bio', e.target.value)} />
                <Typography variant="caption" color="text.secondary">
                  {t('bioCharCount', { count: (form.bio || '').length })}
                </Typography>
              </Grid>
              <Grid item xs={12}>
                <TextField fullWidth size="small" label={t('hobbiesLabel')}
                  placeholder={t('hobbiesPlaceholder')}
                  value={form.hobbies || ''} onChange={e => set('hobbies', e.target.value)} />
              </Grid>
              <Grid item xs={12}>
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('languagesYouSpeak')}</Typography>
                <LanguageSelector value={form.languages || ''} onChange={v => set('languages', v)} />
              </Grid>
            </Grid>
          </Paper>

          {/* ── 2. Stil života ────────────────────────────────────────── */}
          <Paper sx={{ p: 3, borderRadius: 3 }}>
            <SectionHeader icon={<PrefsIcon />} title={t('lifestyleSection')} subtitle={t('lifestyleSectionSub')} />
            <Grid container spacing={2.5}>
              <Grid item xs={12}>
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('tenantType')}</Typography>
                <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap' }}>
                  {LIFESTYLE_OPTIONS.map(o => (
                    <Box key={o.value} onClick={() => set('lifestyle', form.lifestyle === o.value ? '' : o.value)}
                      sx={{
                        border: 2, borderRadius: 2, p: 1.5, cursor: 'pointer', flex: '1 1 150px',
                        borderColor: form.lifestyle === o.value ? 'primary.main' : 'divider',
                        bgcolor: form.lifestyle === o.value ? 'action.selected' : 'transparent',
                        transition: 'all 0.15s', '&:hover': { borderColor: 'primary.light' },
                      }}>
                      <Typography variant="body2" fontWeight="bold">{o.label}</Typography>
                      <Typography variant="caption" color="text.secondary">{o.desc}</Typography>
                    </Box>
                  ))}
                </Box>
              </Grid>
              <Grid item xs={12}>
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('dailyRhythm')}</Typography>
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                  {SCHEDULE_OPTIONS.map(o => (
                    <Chip key={o.value} label={o.label} clickable size="small"
                      color={form.workSchedule === o.value ? 'primary' : 'default'}
                      variant={form.workSchedule === o.value ? 'filled' : 'outlined'}
                      onClick={() => set('workSchedule', form.workSchedule === o.value ? 0 : o.value)} />
                  ))}
                </Box>
              </Grid>
              <Grid item xs={12}>
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('apartmentCleanliness')}</Typography>
                <Box sx={{ display: 'flex', gap: 1 }}>
                  {CLEANLINESS_OPTIONS.map(o => (
                    <Chip key={o.value} label={o.label} clickable size="small"
                      color={form.cleanliness === o.value ? 'primary' : 'default'}
                      variant={form.cleanliness === o.value ? 'filled' : 'outlined'}
                      onClick={() => set('cleanliness', form.cleanliness === o.value ? '' : o.value)} />
                  ))}
                </Box>
              </Grid>
              <Grid item xs={12}>
                <Divider sx={{ mb: 2 }} />
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('apartmentRules')}</Typography>
                <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }}>
                  <YesNoToggle label={t('smokingPref')} value={form.smokingAllowed} onChange={v => set('smokingAllowed', v)} />
                  <YesNoToggle label={t('petsPref')} value={form.petFriendly} onChange={v => set('petFriendly', v)} />
                  <YesNoToggle label={t('guestsPref')} value={form.guestsAllowed} onChange={v => set('guestsAllowed', v)} />
                  <YesNoToggle label={t('musicPref')} value={form.musicFriendly} onChange={v => set('musicFriendly', v)} />
                </Box>
              </Grid>
            </Grid>
          </Paper>

          {/* ── 3. Budžet i raspoloživost ─────────────────────────────── */}
          <Paper sx={{ p: 3, borderRadius: 3 }}>
            <SectionHeader icon={<EuroIcon />} title={t('budgetSection')} subtitle={t('budgetSectionSub')} />
            <Grid container spacing={2}>
              <Grid item xs={12} sm={4}>
                <TextField fullWidth size="small" label={t('minBudgetLabel')} type="number"
                  value={form.budgetMin || ''}
                  onChange={e => set('budgetMin', e.target.value ? parseFloat(e.target.value) : undefined)} />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField fullWidth size="small" label={t('maxBudgetLabel')} type="number"
                  value={form.budgetMax || ''}
                  onChange={e => set('budgetMax', e.target.value ? parseFloat(e.target.value) : undefined)} />
              </Grid>
              <Grid item xs={12} sm={4}>
                <TextField fullWidth size="small" label={t('budgetIncludesLabel')}
                  placeholder={t('budgetIncludesPlaceholder')}
                  value={form.budgetIncludes || ''} onChange={e => set('budgetIncludes', e.target.value)} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <DatePicker label={t('availableFromDateLabel')}
                  value={form.availableFrom ? new Date(form.availableFrom) : null}
                  onChange={v => set('availableFrom', v ? (v as Date).toISOString().split('T')[0] : undefined)}
                  slotProps={{ textField: { fullWidth: true, size: 'small' } }} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <DatePicker label={t('availableUntilDateLabel')}
                  value={form.availableUntil ? new Date(form.availableUntil) : null}
                  onChange={v => set('availableUntil', v ? (v as Date).toISOString().split('T')[0] : undefined)}
                  slotProps={{ textField: { fullWidth: true, size: 'small' } }} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label={t('minStayLabel')} type="number"
                  value={form.minimumStayMonths || ''}
                  onChange={e => set('minimumStayMonths', e.target.value ? parseInt(e.target.value) : undefined)} />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField fullWidth size="small" label={t('maxStayLabel')} type="number"
                  value={form.maximumStayMonths || ''}
                  onChange={e => set('maximumStayMonths', e.target.value ? parseInt(e.target.value) : undefined)} />
              </Grid>
            </Grid>
          </Paper>

          {/* ── 4. Šta tražiš ────────────────────────────────────────── */}
          <Paper sx={{ p: 3, borderRadius: 3 }}>
            <SectionHeader icon={<HomeIcon />} title={t('lookingForSection2')} subtitle={t('lookingForSub')} />
            <Grid container spacing={2}>
              <Grid item xs={12}>
                <TextField fullWidth size="small" label={t('preferredLocationLabel')}
                  placeholder={t('preferredLocationPlaceholder')}
                  value={form.preferredLocation || ''} onChange={e => set('preferredLocation', e.target.value)} />
              </Grid>
              <Grid item xs={12}>
                <Typography variant="body2" fontWeight="medium" gutterBottom>{t('accommodationType')}</Typography>
                <Box sx={{ display: 'flex', gap: 1, flexWrap: 'wrap' }}>
                  {ROOM_TYPES.map(rt => (
                    <Chip key={rt.value} label={rt.label} clickable size="small"
                      color={form.lookingForRoomType === rt.value ? 'primary' : 'default'}
                      variant={form.lookingForRoomType === rt.value ? 'filled' : 'outlined'}
                      onClick={() => set('lookingForRoomType', form.lookingForRoomType === rt.value ? '' : rt.value)} />
                  ))}
                </Box>
              </Grid>
            </Grid>
          </Paper>

          {/* Actions */}
          <Box sx={{ display: 'flex', gap: 2, justifyContent: 'flex-end', pb: 2 }}>
            <Button variant="outlined" size="large" onClick={() => navigate('/roommates')}>
              {t('cancelBtn')}
            </Button>
            <Button type="submit" variant="contained" color="secondary" size="large"
              disabled={mutation.isPending}
              startIcon={mutation.isPending ? <CircularProgress size={18} /> : undefined}>
              {mutation.isPending ? t('savingBtn') : isEdit ? t('updateProfileBtn') : t('publishProfileBtn')}
            </Button>
          </Box>
        </Box>
      </Container>
    </LocalizationProvider>
  );
};

export default CreateRoommatePage;

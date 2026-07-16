import React, { useState, useEffect } from 'react';
import {
  Box,
  Typography,
  Button,
  Chip,
  IconButton,
  List,
  ListItem,
  ListItemIcon,
  ListItemText,
  Tab,
  Tabs,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  CircularProgress,
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CheckIcon from '@mui/icons-material/Check';
import AutoGraphIcon from '@mui/icons-material/AutoGraph';
import StarIcon from '@mui/icons-material/Star';
import VisibilityIcon from '@mui/icons-material/Visibility';
import HomeIcon from '@mui/icons-material/Home';
import RocketLaunchIcon from '@mui/icons-material/RocketLaunch';
import MarkEmailReadIcon from '@mui/icons-material/MarkEmailRead';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { paymentsApi, submitMonriForm } from '../shared/api/paymentsApi';
import { useNotifications } from '../shared/context/NotificationContext';
import { useAuth } from '../shared/context/AuthContext';
import { apiClient } from '../shared/api/client';
import WithdrawalWaiverModal from '../components/Payment/WithdrawalWaiverModal';

const PricingPage: React.FC = () => {
  const navigate = useNavigate();
  const { t } = useTranslation('subscriptions');
  const { addNotification } = useNotifications();
  const { isAuthenticated } = useAuth();
  const [tabIndex, setTabIndex] = useState(0);

  // Analytics
  const [analyticsCycle, setAnalyticsCycle] = useState<'Monthly' | 'Yearly'>('Yearly');

  // Featured listing
  const [featuredDuration, setFeaturedDuration] = useState<'7 Days' | '30 Days'>('7 Days');
  const [myApartments, setMyApartments] = useState<{ apartmentId: number; title: string }[]>([]);
  const [selectedApartmentId, setSelectedApartmentId] = useState<number | ''>('');
  const [apartmentsLoading, setApartmentsLoading] = useState(false);

  // Listing
  const [listingCount, setListingCount] = useState(1);
  const [loading, setLoading] = useState(false);

  // Withdrawal waiver modal state
  const [waiverOpen, setWaiverOpen] = useState(false);
  const [pendingPlanId, setPendingPlanId] = useState<string | null>(null);
  const [pendingApartmentId, setPendingApartmentId] = useState<number | null>(null);
  const [waiverPlanName, setWaiverPlanName] = useState('');
  const [waiverAmount, setWaiverAmount] = useState('');

  // Feature lists (built inside component to use t())
  const analyticsFeatures = [
    { icon: <AutoGraphIcon fontSize="small" />, text: t('planAnalytics_feat1'), highlighted: true },
    { icon: <CheckIcon fontSize="small" />, text: t('planAnalytics_feat2') },
    { icon: <CheckIcon fontSize="small" />, text: t('planAnalytics_feat3') },
    { icon: <CheckIcon fontSize="small" />, text: t('planAnalytics_feat4') },
  ];

  const listingFeatures = [
    { icon: <HomeIcon fontSize="small" />, text: t('planListing_feat1'), highlighted: true },
    { icon: <CheckIcon fontSize="small" />, text: t('planListing_feat2') },
    { icon: <CheckIcon fontSize="small" />, text: t('planListing_feat3') },
    { icon: <CheckIcon fontSize="small" />, text: t('planListing_feat4') },
    { icon: <CheckIcon fontSize="small" />, text: t('planListing_feat5') },
  ];

  const boostProfileFeatures = [
    { icon: <RocketLaunchIcon fontSize="small" />, text: t('planBoost_feat1'), highlighted: true },
    { icon: <CheckIcon fontSize="small" />, text: t('planBoost_feat2') },
    { icon: <CheckIcon fontSize="small" />, text: t('planBoost_feat3') },
    { icon: <CheckIcon fontSize="small" />, text: t('planBoost_feat4') },
  ];

  const priorityInboxFeatures = [
    { icon: <MarkEmailReadIcon fontSize="small" />, text: t('planPriority_feat1'), highlighted: true },
    { icon: <CheckIcon fontSize="small" />, text: t('planPriority_feat2') },
    { icon: <CheckIcon fontSize="small" />, text: t('planPriority_feat3') },
    { icon: <CheckIcon fontSize="small" />, text: t('planPriority_feat4') },
  ];

  const featuredFeatures = [
    { icon: <StarIcon fontSize="small" />, text: t('planFeatured_feat1'), highlighted: true },
    { icon: <VisibilityIcon fontSize="small" />, text: t('planFeatured_feat2') },
    { icon: <CheckIcon fontSize="small" />, text: t('planFeatured_feat3') },
    { icon: <CheckIcon fontSize="small" />, text: t('planFeatured_feat4') },
  ];

  // Plan labels for waiver modal
  const PLAN_LABELS: Record<string, { name: string; amount: string }> = {
    'analytics-monthly':  { name: t('planLabelAnalyticsMonthly'), amount: '€4.99' },
    'analytics-yearly':   { name: t('planLabelAnalyticsYearly'),  amount: '€49.99' },
    'tokens-10':          { name: t('planLabelTokens10'),          amount: '€2.99' },
    'tokens-50':          { name: t('planLabelTokens50'),          amount: '€9.99' },
    'tokens-150':         { name: t('planLabelTokens150'),         amount: '€24.99' },
    'featured-7':         { name: t('planLabelFeatured7'),         amount: '€9.99' },
    'featured-30':        { name: t('planLabelFeatured30'),        amount: '€29.99' },
    'listing-1':          { name: t('planLabelListing1'),          amount: '€5.00' },
    'listing-3':          { name: t('planLabelListing3'),          amount: '€15.00' },
    'listing-5':          { name: t('planLabelListing5'),          amount: '€25.00' },
    'boost-7':            { name: t('planLabelBoost7'),            amount: '€2.00' },
    'priority-30':        { name: t('planLabelPriority30'),        amount: '€2.00' },
  };

  // Load user's apartments when on the Featured tab
  useEffect(() => {
    if (tabIndex !== 1 || !isAuthenticated || myApartments.length > 0) return;
    setApartmentsLoading(true);
    apiClient.get('/api/v1/rent/get-my-apartments')
      .then(res => {
        const items = (res.data?.items ?? res.data ?? []) as { apartmentId: number; title: string }[];
        setMyApartments(items);
        if (items.length > 0) setSelectedApartmentId(items[0].apartmentId);
      })
      .catch(() => {/* silently ignore */})
      .finally(() => setApartmentsLoading(false));
  }, [tabIndex, isAuthenticated]);

  const handleBoostProfile   = () => handleSubscribe('boost-7');
  const handlePriorityInbox  = () => handleSubscribe('priority-30');
  const handlePublishListing = () => handleSubscribe(`listing-${listingCount}`);
  const handlePromoteFeature = () => {
    if (!selectedApartmentId) {
      addNotification({ title: t('selectApartmentTitle'), message: t('selectApartmentMessage'), type: 'warning' });
      return;
    }
    handleSubscribeWithApartment(
      featuredDuration === '7 Days' ? 'featured-7' : 'featured-30',
      selectedApartmentId as number,
    );
  };

  const handleSubscribe = (planId: string) => {
    const info = PLAN_LABELS[planId] ?? { name: planId, amount: '' };
    setPendingPlanId(planId);
    setPendingApartmentId(null);
    setWaiverPlanName(info.name);
    setWaiverAmount(info.amount);
    setWaiverOpen(true);
  };

  const handleSubscribeWithApartment = (planId: string, apartmentId: number) => {
    const info = PLAN_LABELS[planId] ?? { name: planId, amount: '' };
    setPendingPlanId(planId);
    setPendingApartmentId(apartmentId);
    setWaiverPlanName(info.name);
    setWaiverAmount(info.amount);
    setWaiverOpen(true);
  };

  const handleWaiverConfirm = async () => {
    if (!pendingPlanId) return;
    setWaiverOpen(false);
    setLoading(true);
    try {
      const formFields = await paymentsApi.createPayment(
        pendingPlanId,
        `${window.location.origin}/payment-success`,
        `${window.location.origin}/payment-failure`,
        pendingApartmentId ?? undefined,
      );
      submitMonriForm(formFields);
    } catch (error: any) {
      addNotification({
        title: t('paymentErrorTitle'),
        message: error.response?.data?.message || t('paymentErrorMessage'),
        type: 'error',
      });
      setLoading(false);
    }
  };

  const handleSubscribeAnalytics = () =>
    handleSubscribe(analyticsCycle === 'Monthly' ? 'analytics-monthly' : 'analytics-yearly');

  return (
    <Box sx={{
      minHeight: '100vh',
      background: 'linear-gradient(180deg, #1C3C58 0%, #305B7A 40%, #4B7795 100%)',
      color: '#fff',
      pt: 6,
      px: 3,
      fontFamily: 'sans-serif'
    }}>
      {/* Header */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, position: 'relative' }}>
        <IconButton sx={{ color: '#fff', position: 'absolute', left: -10 }} onClick={() => navigate(-1)}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h6" sx={{ flex: 1, textAlign: 'center', fontWeight: 'bold' }}>
          {t('pageTitle')}
        </Typography>
      </Box>

      {/* Product Tabs */}
      <Tabs
        value={tabIndex}
        onChange={(_, newValue) => setTabIndex(newValue)}
        centered
        sx={{
          mb: 4,
          '& .MuiTabs-indicator': { backgroundColor: '#89D9F8' },
          '& .MuiTab-root': { color: 'rgba(255,255,255,0.6)', textTransform: 'none', fontSize: '1.05rem' },
          '& .Mui-selected': { color: '#fff !important', fontWeight: 'bold' }
        }}
      >
        <Tab label={t('tabAnalytics')} />
        <Tab label={t('tabPromote')} />
        <Tab label={t('tabListing')} />
        <Tab label={t('tabBoost')} />
        <Tab label={t('tabTokens')} />
      </Tabs>

      {/* Analytics (Tab 0) */}
      {tabIndex === 0 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            {t('analyticsSubtitle')}
          </Typography>

          <Box sx={{ display: 'flex', borderRadius: '30px', border: '1px solid rgba(255,255,255,0.3)', mb: 4, p: '2px' }}>
            <Button
              fullWidth
              sx={{
                borderRadius: '28px',
                color: analyticsCycle === 'Monthly' ? '#fff' : 'rgba(255,255,255,0.6)',
                backgroundColor: analyticsCycle === 'Monthly' ? 'rgba(255,255,255,0.15)' : 'transparent',
                textTransform: 'none',
                '&:hover': { backgroundColor: 'rgba(255,255,255,0.1)' }
              }}
              onClick={() => setAnalyticsCycle('Monthly')}
            >
              {t('cycleMonthly')}
            </Button>
            <Button
              fullWidth
              sx={{
                borderRadius: '28px',
                color: analyticsCycle === 'Yearly' ? '#fff' : 'rgba(255,255,255,0.6)',
                backgroundColor: analyticsCycle === 'Yearly' ? 'rgba(255,255,255,0.15)' : 'transparent',
                textTransform: 'none',
                '&:hover': { backgroundColor: 'rgba(255,255,255,0.1)' }
              }}
              onClick={() => setAnalyticsCycle('Yearly')}
            >
              {t('cycleYearly')}
            </Button>
          </Box>

          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                {analyticsCycle === 'Monthly' ? t('analyticsPlanMonthly') : t('analyticsPlanYearly')}
              </Typography>
              {analyticsCycle === 'Yearly' && (
                <Chip label={t('savePercent')} size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
              )}
            </Box>

            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                  {analyticsCycle === 'Yearly' ? '4.15' : '4.99'}
                </Typography>
                <Typography variant="body2" sx={{ ml: 0.5, opacity: 0.8 }}>{t('perMonth')}</Typography>
              </Box>

              {analyticsCycle === 'Yearly' && (
                <Typography variant="body2" sx={{ opacity: 0.7, mb: 2 }}>{t('yearlyBilling')}</Typography>
              )}

              <List sx={{ mt: 2 }}>
                {analyticsFeatures.map((feature, idx) => (
                  <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1.5 }}>
                    <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#89D9F8' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                      {feature.icon}
                    </ListItemIcon>
                    <ListItemText primary={feature.text} primaryTypographyProps={{ sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } }} />
                  </ListItem>
                ))}
              </List>

              <Button
                fullWidth variant="contained" onClick={handleSubscribeAnalytics} disabled={loading}
                sx={{
                  mt: 2, backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold', borderRadius: '24px', py: 1.5, textTransform: 'none', fontSize: '1rem',
                  '&:hover': { backgroundColor: '#6FC9F0' }
                }}
              >
                {loading ? t('processing') : t('btnSubscribe')}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Promote (Tab 1) */}
      {tabIndex === 1 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            {t('promoteSubtitle')}
          </Typography>

          <Box sx={{ display: 'flex', borderRadius: '30px', border: '1px solid rgba(255,255,255,0.3)', mb: 4, p: '2px' }}>
            <Button
              fullWidth
              sx={{
                borderRadius: '28px',
                color: featuredDuration === '7 Days' ? '#fff' : 'rgba(255,255,255,0.6)',
                backgroundColor: featuredDuration === '7 Days' ? 'rgba(255,255,255,0.15)' : 'transparent',
                textTransform: 'none',
                '&:hover': { backgroundColor: 'rgba(255,255,255,0.1)' }
              }}
              onClick={() => setFeaturedDuration('7 Days')}
            >
              {t('duration7Days')}
            </Button>
            <Button
              fullWidth
              sx={{
                borderRadius: '28px',
                color: featuredDuration === '30 Days' ? '#fff' : 'rgba(255,255,255,0.6)',
                backgroundColor: featuredDuration === '30 Days' ? 'rgba(255,255,255,0.15)' : 'transparent',
                textTransform: 'none',
                '&:hover': { backgroundColor: 'rgba(255,255,255,0.1)' }
              }}
              onClick={() => setFeaturedDuration('30 Days')}
            >
              {t('duration30Days')}
            </Button>
          </Box>

          {isAuthenticated && (
            <Box sx={{ mb: 3 }}>
              {apartmentsLoading ? (
                <Box sx={{ textAlign: 'center' }}><CircularProgress size={24} sx={{ color: '#89D9F8' }} /></Box>
              ) : myApartments.length === 0 ? (
                <Typography variant="body2" sx={{ opacity: 0.7, textAlign: 'center' }}>
                  {t('noApartments')}
                </Typography>
              ) : (
                <FormControl fullWidth size="small">
                  <InputLabel sx={{ color: 'rgba(255,255,255,0.7)' }}>{t('selectApartmentLabel')}</InputLabel>
                  <Select
                    value={selectedApartmentId}
                    onChange={e => setSelectedApartmentId(e.target.value as number)}
                    label={t('selectApartmentLabel')}
                    sx={{ color: '#fff', '.MuiOutlinedInput-notchedOutline': { borderColor: 'rgba(255,255,255,0.3)' }, '.MuiSvgIcon-root': { color: '#fff' } }}
                  >
                    {myApartments.map(apt => (
                      <MenuItem key={apt.apartmentId} value={apt.apartmentId}>{apt.title}</MenuItem>
                    ))}
                  </Select>
                </FormControl>
              )}
            </Box>
          )}

          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                {t('featuredPlanTitle')}
              </Typography>
              <Chip label="Top Tier" size="small" sx={{ backgroundColor: '#FFD700', color: '#0A2540', fontWeight: 'bold' }} />
            </Box>

            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                  {featuredDuration === '30 Days' ? '29.99' : '9.99'}
                </Typography>
              </Box>
              <Typography variant="body2" sx={{ opacity: 0.7, mb: 2 }}>
                {t('oneTimePaymentFor', { duration: featuredDuration === '7 Days' ? t('oneTimeFor7') : t('oneTimeFor30') })}
              </Typography>

              <List sx={{ mt: 2 }}>
                {featuredFeatures.map((feature, idx) => (
                  <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1.5 }}>
                    <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#FFD700' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                      {feature.icon}
                    </ListItemIcon>
                    <ListItemText primary={feature.text} primaryTypographyProps={{ sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } }} />
                  </ListItem>
                ))}
              </List>

              <Button
                fullWidth variant="contained" onClick={handlePromoteFeature} disabled={loading}
                sx={{
                  mt: 2, backgroundColor: '#FFD700', color: '#0A2540', fontWeight: 'bold', borderRadius: '24px', py: 1.5, textTransform: 'none', fontSize: '1rem',
                  '&:hover': { backgroundColor: '#F0CA00' }
                }}
              >
                {loading ? t('processing') : t('btnPromote', { duration: featuredDuration === '7 Days' ? t('oneTimeFor7') : t('oneTimeFor30') })}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Listing (Tab 2) */}
      {tabIndex === 2 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            {t('listingSubtitle')}
          </Typography>

          <Box sx={{ display: 'flex', borderRadius: '30px', border: '1px solid rgba(255,255,255,0.3)', mb: 4, p: '2px' }}>
            {[1, 3, 5].map((n) => (
              <Button
                key={n}
                fullWidth
                sx={{
                  borderRadius: '28px',
                  color: listingCount === n ? '#fff' : 'rgba(255,255,255,0.6)',
                  backgroundColor: listingCount === n ? 'rgba(255,255,255,0.15)' : 'transparent',
                  textTransform: 'none',
                  '&:hover': { backgroundColor: 'rgba(255,255,255,0.1)' }
                }}
                onClick={() => setListingCount(n)}
              >
                {n === 1 ? t('listing1') : t('listingN', { count: n })}
              </Button>
            ))}
          </Box>

          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                {t('listingPlanTitle')}
              </Typography>
              {listingCount >= 3 && (
                <Chip label={listingCount === 3 ? t('chipSaveTime') : t('chipBestValue')} size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
              )}
            </Box>

            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                  {(listingCount * 5).toFixed(0)}
                </Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>
                  {t('listingPriceNote', { count: listingCount })}
                </Typography>
              </Box>
              <Typography variant="body2" sx={{ opacity: 0.7, mb: 2 }}>
                {t('oneTimeListingNote')}
              </Typography>

              <List sx={{ mt: 2 }}>
                {listingFeatures.map((feature, idx) => (
                  <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1.5 }}>
                    <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#89D9F8' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                      {feature.icon}
                    </ListItemIcon>
                    <ListItemText primary={feature.text} primaryTypographyProps={{ sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } }} />
                  </ListItem>
                ))}
              </List>

              <Button
                fullWidth variant="contained" onClick={handlePublishListing} disabled={loading}
                sx={{
                  mt: 2, backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold', borderRadius: '24px', py: 1.5, textTransform: 'none', fontSize: '1rem',
                  '&:hover': { backgroundColor: '#6FC9F0' }
                }}
              >
                {loading ? t('processing') : listingCount === 1 ? t('btnPublish1') : t('btnPublishN', { count: listingCount })}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Boost (Tab 3) */}
      {tabIndex === 3 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 4 }}>
            {t('boostSubtitle')}
          </Typography>

          {/* Boost Profila card */}
          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '2px solid rgba(137,217,248,0.5)',
            overflow: 'hidden',
            mb: 3,
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <RocketLaunchIcon sx={{ color: '#89D9F8' }} />
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>{t('boostProfileTitle')}</Typography>
              </Box>
              <Chip label={t('boostChip7Days')} size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
            </Box>
            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 2 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>2</Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>{t('boostPer7Days')}</Typography>
              </Box>
              <List dense>
                {boostProfileFeatures.map((feature, idx) => (
                  <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1 }}>
                    <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#89D9F8' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                      {feature.icon}
                    </ListItemIcon>
                    <ListItemText primary={feature.text} primaryTypographyProps={{ sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } }} />
                  </ListItem>
                ))}
              </List>
              <Button
                fullWidth variant="contained" onClick={handleBoostProfile} disabled={loading}
                sx={{ mt: 1, backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold', borderRadius: '24px', py: 1.5, textTransform: 'none', fontSize: '1rem', '&:hover': { backgroundColor: '#6FC9F0' } }}
              >
                {loading ? t('processing') : t('btnActivateBoost')}
              </Button>
            </Box>
          </Box>

          {/* Priority Inbox card */}
          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '2px solid rgba(255,215,0,0.4)',
            overflow: 'hidden',
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                <MarkEmailReadIcon sx={{ color: '#FFD700' }} />
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>{t('priorityInboxTitle')}</Typography>
              </Box>
              <Chip label={t('priorityChip30Days')} size="small" sx={{ backgroundColor: '#FFD700', color: '#0A2540', fontWeight: 'bold' }} />
            </Box>
            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 2 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>2</Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>{t('priorityPer30Days')}</Typography>
              </Box>
              <List dense>
                {priorityInboxFeatures.map((feature, idx) => (
                  <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1 }}>
                    <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#FFD700' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                      {feature.icon}
                    </ListItemIcon>
                    <ListItemText primary={feature.text} primaryTypographyProps={{ sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } }} />
                  </ListItem>
                ))}
              </List>
              <Button
                fullWidth variant="contained" onClick={handlePriorityInbox} disabled={loading}
                sx={{ mt: 1, backgroundColor: '#FFD700', color: '#0A2540', fontWeight: 'bold', borderRadius: '24px', py: 1.5, textTransform: 'none', fontSize: '1rem', '&:hover': { backgroundColor: '#F0CA00' } }}
              >
                {loading ? t('processing') : t('btnActivatePriority')}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Tokens (Tab 4) */}
      {tabIndex === 4 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            {t('tokensSubtitle')}
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr', gap: 2 }}>
            {[
              { tokens: 10,  price: 2.99,  label: 'Starter Pack', icon: '💎' },
              { tokens: 50,  price: 9.99,  label: 'Power User',   icon: '🔥', popular: true },
              { tokens: 150, price: 24.99, label: 'Elite Bundle', icon: '👑' }
            ].map((pack) => (
              <Box key={pack.tokens} sx={{
                background: pack.popular ? 'rgba(137, 217, 248, 0.15)' : 'rgba(255,255,255,0.1)',
                backdropFilter: 'blur(10px)',
                borderRadius: '16px',
                border: pack.popular ? '2px solid #89D9F8' : '1px solid rgba(255,255,255,0.2)',
                p: 2,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                transition: 'transform 0.2s',
                '&:hover': { transform: 'scale(1.02)' }
              }}>
                <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
                  <Typography variant="h4">{pack.icon}</Typography>
                  <Box>
                    <Typography variant="h6" fontWeight="bold">{t('tokensCount', { count: pack.tokens })}</Typography>
                    <Typography variant="body2" sx={{ opacity: 0.7 }}>{pack.label}</Typography>
                  </Box>
                </Box>
                <Box sx={{ textAlign: 'right' }}>
                  <Typography variant="h5" fontWeight="bold">€{pack.price}</Typography>
                  <Button
                    size="small"
                    variant="contained"
                    onClick={() => handleSubscribe(`tokens-${pack.tokens}`)}
                    disabled={loading}
                    sx={{
                      mt: 1,
                      borderRadius: '12px',
                      backgroundColor: pack.popular ? '#89D9F8' : '#fff',
                      color: '#0A2540',
                      textTransform: 'none',
                      fontWeight: 'bold',
                    }}
                  >
                    {loading ? '...' : t('btnBuy')}
                  </Button>
                </Box>
              </Box>
            ))}
          </Box>
          <Typography variant="body2" sx={{ textAlign: 'center', mt: 4, opacity: 0.6 }}>
            {t('tokensFootnote')}
          </Typography>
        </>
      )}

      <WithdrawalWaiverModal
        open={waiverOpen}
        planName={waiverPlanName}
        amount={waiverAmount}
        onConfirm={handleWaiverConfirm}
        onCancel={() => setWaiverOpen(false)}
      />
    </Box>
  );
};

export default PricingPage;

import React, { useState } from 'react';
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
  Tabs
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
import { paymentsApi } from '../shared/api/paymentsApi';
import { useNotifications } from '../shared/context/NotificationContext';

const analyticsFeatures = [
  { icon: <AutoGraphIcon fontSize="small" />, text: "Advanced search behavior insights", highlighted: true },
  { icon: <CheckIcon fontSize="small" />, text: "Detailed apartment view tracking" },
  { icon: <CheckIcon fontSize="small" />, text: "Message response analytics" },
  { icon: <CheckIcon fontSize="small" />, text: "ML-powered price predictions (Landlords)" },
];

const listingFeatures = [
  { icon: <HomeIcon fontSize="small" />, text: "Objavi oglas za stan ili kuću", highlighted: true },
  { icon: <CheckIcon fontSize="small" />, text: "Oglas aktivan 30 dana" },
  { icon: <CheckIcon fontSize="small" />, text: "Neograničen broj fotografija" },
  { icon: <CheckIcon fontSize="small" />, text: "Direktne poruke od zainteresovanih stanara" },
  { icon: <CheckIcon fontSize="small" />, text: "Upravljanje prijavama stanara" },
];

const boostProfileFeatures = [
  { icon: <RocketLaunchIcon fontSize="small" />, text: "Tvoj profil se prikazuje prvi u pretrazi cimera", highlighted: true },
  { icon: <CheckIcon fontSize="small" />, text: "Vidljiv korisnicima koji odgovaraju tvojim preferencijama" },
  { icon: <CheckIcon fontSize="small" />, text: "Boost aktivan 7 dana" },
  { icon: <CheckIcon fontSize="small" />, text: "Do 3× više pregleda profila" },
];

const priorityInboxFeatures = [
  { icon: <MarkEmailReadIcon fontSize="small" />, text: "Notifikacija ko je pregledao tvoj oglas", highlighted: true },
  { icon: <CheckIcon fontSize="small" />, text: "Poruke od verifikovanih korisnika označene prioritetom" },
  { icon: <CheckIcon fontSize="small" />, text: "Aktivan 30 dana" },
  { icon: <CheckIcon fontSize="small" />, text: "Uvid u broj pregleda po danu" },
];

const featuredFeatures = [
  { icon: <StarIcon fontSize="small" />, text: "Rank at the top of search results", highlighted: true },
  { icon: <VisibilityIcon fontSize="small" />, text: "Get up to 5x more profile/apartment views" },
  { icon: <CheckIcon fontSize="small" />, text: "Special 'Featured' badge on your listing" },
  { icon: <CheckIcon fontSize="small" />, text: "Priority in email recommendations" },
];

const PricingPage: React.FC = () => {
  const navigate = useNavigate();
  const { addNotification } = useNotifications();
  const [tabIndex, setTabIndex] = useState(0);
  
  // State for Analytics
  const [analyticsCycle, setAnalyticsCycle] = useState<'Monthly' | 'Yearly'>('Yearly');
  
  // State for Featured Apartment
  const [featuredDuration, setFeaturedDuration] = useState<'7 Days' | '30 Days'>('7 Days');

  const [listingCount, setListingCount] = useState(1);
  const [loading, setLoading] = useState(false);

  const comingSoon = () => {
    addNotification({ title: 'Coming Soon', message: 'Online payment will be available soon. Contact us at info@turentaj.com to arrange payment.', type: 'info' });
  };

  const handleSubscribeAnalytics = comingSoon;
  const handleBoostProfile = comingSoon;
  const handlePriorityInbox = comingSoon;
  const handlePublishListing = comingSoon;
  const handlePromoteFeature = comingSoon;

  const handleSubscribe = async (planId: string) => {
    setLoading(true);
    try {
      const formFields = await paymentsApi.createPayment(
        planId,
        `${window.location.origin}/payment-success`,
        `${window.location.origin}/payment-failure`
      );

      // Dynamically build and submit a form to Monri's hosted payment page
      const form = document.createElement('form');
      form.method = 'POST';
      form.action = formFields.formAction;

      const fields: Record<string, string> = {
        authenticity_token: formFields.authenticityToken,
        order_number: formFields.orderNumber,
        amount: String(formFields.amount),
        currency: formFields.currency,
        order_info: formFields.orderInfo,
        digest: formFields.digest,
        success_url_override: formFields.successUrl,
        failure_url_override: formFields.failureUrl,
        callback_url: formFields.callbackUrl,
        buyer_name: formFields.buyerName,
        buyer_email: formFields.buyerEmail,
        language: 'sr',
      };

      Object.entries(fields).forEach(([name, value]) => {
        const input = document.createElement('input');
        input.type = 'hidden';
        input.name = name;
        input.value = value;
        form.appendChild(input);
      });

      document.body.appendChild(form);
      form.submit();
    } catch (error: any) {
      addNotification({
        title: 'Greška',
        message: error.response?.data?.error || 'Nije moguće pokrenuti plaćanje',
        type: 'error'
      });
      setLoading(false);
    }
  };

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
          Premium Services
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
        <Tab label="Analytics" />
        <Tab label="Promote" />
        <Tab label="Oglas" />
        <Tab label="Boost" />
        <Tab label="Tokens" />
      </Tabs>

      {/* Content for Analytics (Tab 0) */}
      {tabIndex === 0 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            Otključaj moćnu analitiku i uvide
          </Typography>

          {/* Toggle */}
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
              Mesečno
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
              Godišnje
            </Button>
          </Box>

          {/* Card */}
          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                TuRentaj Analitika {analyticsCycle === 'Monthly' ? '(Mesečno)' : '(Godišnje)'}
              </Typography>
              {analyticsCycle === 'Yearly' && (
                 <Chip label="Uštedi ~16%" size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
              )}
            </Box>
            
            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                  {analyticsCycle === 'Yearly' ? '4.15' : '4.99'}
                </Typography>
                <Typography variant="body2" sx={{ ml: 0.5, opacity: 0.8 }}>/mesečno</Typography>
              </Box>
              
              {analyticsCycle === 'Yearly' && (
                <Typography variant="body2" sx={{ opacity: 0.7, mb: 2 }}>€49.99 naplata jednom godišnje</Typography>
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
                {loading ? 'Obrađuje se...' : 'Pretplati se'}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Content for Featured Listing (Tab 1) */}
      {tabIndex === 1 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            Poboljšaj vidljivost svog oglasa
          </Typography>

          {/* Toggle */}
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
              7 Dana
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
              30 Dana
            </Button>
          </Box>

          {/* Card */}
          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                Istaknut Oglas
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
                Jednokratna uplata za {featuredDuration === '7 Days' ? '7 dana' : '30 dana'} promocije
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
                {loading ? 'Obrađuje se...' : `Promoviši na ${featuredDuration === '7 Days' ? '7 dana' : '30 dana'}`}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Content for Listing (Tab 2) */}
      {tabIndex === 2 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            Objavi oglas i pronađi stanare brzo i lako
          </Typography>

          {/* Quantity selector */}
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
                {n === 1 ? '1 oglas' : `${n} oglasa`}
              </Button>
            ))}
          </Box>

          {/* Card */}
          <Box sx={{
            background: 'rgba(255,255,255,0.1)',
            backdropFilter: 'blur(10px)',
            borderRadius: '16px',
            border: '1px solid rgba(255,255,255,0.2)',
            overflow: 'hidden'
          }}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', p: 2, borderBottom: '1px solid rgba(255,255,255,0.1)' }}>
              <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
                Oglašavanje stana
              </Typography>
              {listingCount >= 3 && (
                <Chip label={listingCount === 3 ? 'Uštedi vremena' : 'Najpovoljnije'} size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
              )}
            </Box>

            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
                  {(listingCount * 5).toFixed(0)}
                </Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>
                  ({listingCount} × €5 / oglas)
                </Typography>
              </Box>
              <Typography variant="body2" sx={{ opacity: 0.7, mb: 2 }}>
                Jednokratna uplata — oglas aktivan 30 dana
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
                {loading ? 'Obrađuje se...' : `Objavi ${listingCount === 1 ? 'oglas' : `${listingCount} oglasa`}`}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Content for Boost (Tab 3) */}
      {tabIndex === 3 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 4 }}>
            Povećaj svoju vidljivost i dobij pravi uvid u interes
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
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Boost Profila Cimera</Typography>
              </Box>
              <Chip label="7 dana" size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
            </Box>
            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 2 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>2</Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>/ 7 dana</Typography>
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
                {loading ? 'Obrađuje se...' : 'Aktiviraj Boost'}
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
                <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>Priority Inbox</Typography>
              </Box>
              <Chip label="30 dana" size="small" sx={{ backgroundColor: '#FFD700', color: '#0A2540', fontWeight: 'bold' }} />
            </Box>
            <Box sx={{ p: 2 }}>
              <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 2 }}>
                <Typography variant="h6" sx={{ mr: 0.5 }}>€</Typography>
                <Typography variant="h3" sx={{ fontWeight: 'bold' }}>2</Typography>
                <Typography variant="body2" sx={{ ml: 1, opacity: 0.8 }}>/ 30 dana</Typography>
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
                {loading ? 'Obrađuje se...' : 'Aktiviraj Priority Inbox'}
              </Button>
            </Box>
          </Box>
        </>
      )}

      {/* Content for Tokens (Tab 4) */}
      {tabIndex === 4 && (
        <>
          <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
            Kupi tokene za Super-Like i direktne poruke
          </Typography>

          <Box sx={{ display: 'grid', gridTemplateColumns: '1fr', gap: 2 }}>
            {[
              { tokens: 10, price: 2.99, label: 'Starter Pack', icon: '💎' },
              { tokens: 50, price: 9.99, label: 'Power User', icon: '🔥', popular: true },
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
                    <Typography variant="h6" fontWeight="bold">{pack.tokens} Tokena</Typography>
                    <Typography variant="body2" sx={{ opacity: 0.7 }}>{pack.label}</Typography>
                  </Box>
                </Box>
                <Box sx={{ textAlign: 'right' }}>
                  <Typography variant="h5" fontWeight="bold">€{pack.price}</Typography>
                  <Button 
                    size="small" 
                    variant="contained"
                    onClick={() => {
                      setLoading(true);
                      paymentsApi.initiatePaytenCheckout(`${pack.tokens} Tokena`, pack.price)
                        .then(res => { if (res.checkoutUrl) window.location.href = res.checkoutUrl; })
                        .catch(() => {
                           addNotification({ title: 'Error', message: 'Failed to initiate checkout', type: 'error' });
                           setLoading(false);
                        });
                    }}
                    sx={{ 
                      mt: 1, 
                      borderRadius: '12px', 
                      backgroundColor: pack.popular ? '#89D9F8' : '#fff',
                      color: '#0A2540',
                      textTransform: 'none',
                      fontWeight: 'bold'
                    }}
                  >
                    Kupi
                  </Button>
                </Box>
              </Box>
            ))}
          </Box>
          <Typography variant="body2" sx={{ textAlign: 'center', mt: 4, opacity: 0.6 }}>
            Tokeni se koriste za "Super-Like" cimera i slanje poruka stanodavcima pre zvanične prijave.
          </Typography>
        </>
      )}
    </Box>
  );
};

export default PricingPage;

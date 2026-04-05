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
} from '@mui/material';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import CheckIcon from '@mui/icons-material/Check';
import AutoGraphIcon from '@mui/icons-material/AutoGraph';
import { useNavigate } from 'react-router-dom';
import { paymentsApi } from '../shared/api/paymentsApi';

const features = [
  { icon: <AutoGraphIcon fontSize="small" />, text: "AI-guided conversations", highlighted: true },
  { icon: <CheckIcon fontSize="small" />, text: "AI-guided journaling with curated conversation topics" },
  { icon: <CheckIcon fontSize="small" />, text: "Unlimited journal entries and transcript history" },
  { icon: <CheckIcon fontSize="small" />, text: "Daily reminders and smart scheduling" },
  { icon: <CheckIcon fontSize="small" />, text: "Weekly streaks and progress insights" }
];

const SubscriptionPage: React.FC = () => {
  const navigate = useNavigate();
  const [billingCycle, setBillingCycle] = useState<'Monthly' | 'Yearly'>('Yearly');
  const [loading, setLoading] = useState(false);

  const handleUpgrade = async () => {
    setLoading(true);
    try {
      // Typically we call the new backend via paymentsApi or axios
      const amount = billingCycle === 'Yearly' ? 134.99 : 14.99;
      
      const response = await paymentsApi.initiatePaytenCheckout(billingCycle, amount);
      
      if (response && response.checkoutUrl) {
         window.location.href = response.checkoutUrl;
      }
    } catch (error) {
      console.error('Checkout failed', error);
    } finally {
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
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 4, position: 'relative' }}>
        <IconButton sx={{ color: '#fff', position: 'absolute', left: -10 }} onClick={() => navigate(-1)}>
          <ArrowBackIcon />
        </IconButton>
        <Typography variant="h6" sx={{ flex: 1, textAlign: 'center', fontWeight: 'bold' }}>
          Subscription
        </Typography>
      </Box>

      {/* Title */}
      <Typography variant="body1" sx={{ textAlign: 'center', mb: 3 }}>
        Choose your subscription
      </Typography>

      {/* Toggle */}
      <Box sx={{
        display: 'flex',
        borderRadius: '30px',
        border: '1px solid rgba(255,255,255,0.3)',
        mb: 4,
        p: '2px'
      }}>
        <Button
          fullWidth
          sx={{
            borderRadius: '28px',
            color: billingCycle === 'Monthly' ? '#fff' : 'rgba(255,255,255,0.6)',
            backgroundColor: billingCycle === 'Monthly' ? 'rgba(255,255,255,0.15)' : 'transparent',
            textTransform: 'none',
            '&:hover': { backgroundColor: billingCycle === 'Monthly' ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.05)' }
          }}
          onClick={() => setBillingCycle('Monthly')}
        >
          Monthly
        </Button>
        <Button
          fullWidth
          sx={{
            borderRadius: '28px',
            color: billingCycle === 'Yearly' ? '#fff' : 'rgba(255,255,255,0.6)',
            backgroundColor: billingCycle === 'Yearly' ? 'rgba(255,255,255,0.15)' : 'transparent',
            textTransform: 'none',
            '&:hover': { backgroundColor: billingCycle === 'Yearly' ? 'rgba(255,255,255,0.25)' : 'rgba(255,255,255,0.05)' }
          }}
          onClick={() => setBillingCycle('Yearly')}
        >
          Yearly
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
            TuRentaj Premium {billingCycle}
          </Typography>
          {billingCycle === 'Yearly' && (
             <Chip label="50% off" size="small" sx={{ backgroundColor: '#89D9F8', color: '#0A2540', fontWeight: 'bold' }} />
          )}
        </Box>
        
        <Box sx={{ p: 2 }}>
          <Box sx={{ display: 'flex', alignItems: 'baseline', mb: 1 }}>
            <Typography variant="h6" sx={{ mr: 0.5 }}>$</Typography>
            <Typography variant="h3" sx={{ fontWeight: 'bold' }}>
              {billingCycle === 'Yearly' ? '11.25' : '14.99'}
            </Typography>
            <Typography variant="body2" sx={{ ml: 0.5, opacity: 0.8 }}>/month</Typography>
          </Box>
          
          {billingCycle === 'Yearly' && (
            <>
              <Typography variant="body2" sx={{ opacity: 0.7, mb: 0.5 }}>$134.99 billed yearly</Typography>
              <Typography variant="body2" sx={{ color: '#89D9F8', fontSize: '0.85rem', mb: 2 }}>
                50% off for first 6 months for annual subscriptions
              </Typography>
            </>
          )}

          <List sx={{ mt: 2 }}>
            {features.map((feature, idx) => (
              <ListItem key={idx} disablePadding sx={{ alignItems: 'flex-start', mb: 1.5 }}>
                <ListItemIcon sx={{ minWidth: 28, color: feature.highlighted ? '#89D9F8' : 'rgba(255,255,255,0.6)', mt: 0.3 }}>
                  {feature.icon}
                </ListItemIcon>
                <ListItemText 
                  primary={feature.text} 
                  primaryTypographyProps={{ 
                    sx: { fontSize: '0.9rem', color: feature.highlighted ? '#fff' : 'rgba(255,255,255,0.8)', fontWeight: feature.highlighted ? 'bold' : 'normal' } 
                  }} 
                />
              </ListItem>
            ))}
          </List>

          <Button
            fullWidth
            variant="contained"
            onClick={handleUpgrade}
            disabled={loading}
            sx={{
              mt: 2,
              backgroundColor: '#89D9F8',
              color: '#0A2540',
              fontWeight: 'bold',
              borderRadius: '24px',
              py: 1.5,
              textTransform: 'none',
              fontSize: '1rem',
              '&:hover': {
                backgroundColor: '#6FC9F0'
              }
            }}
          >
            {loading ? 'Processing...' : 'Upgrade'}
          </Button>
        </Box>
      </Box>
    </Box>
  );
};

export default SubscriptionPage;

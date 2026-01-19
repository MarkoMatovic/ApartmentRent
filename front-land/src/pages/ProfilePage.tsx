import React, { useState, useEffect } from 'react';
import {
  Container,
  Typography,
  Box,
  Paper,
  TextField,
  Button,
  Avatar,
  Grid,
  Alert,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  IconButton,
  CircularProgress
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Cancel';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';
import { authApi } from '../shared/api/auth';
import { apiClient } from '../shared/api/client';

const ProfilePage: React.FC = () => {
  const { t } = useTranslation('common');
  const { user, updateUser } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [deactivateDialogOpen, setDeactivateDialogOpen] = useState(false);

  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phoneNumber: user?.phoneNumber || '',
    dateOfBirth: user?.dateOfBirth || '',
    profilePicture: user?.profilePicture || ''
  });

  const [imagePreview, setImagePreview] = useState(user?.profilePicture || '');

  useEffect(() => {
    if (user) {
      setFormData({
        firstName: user.firstName || '',
        lastName: user.lastName || '',
        email: user.email || '',
        phoneNumber: user.phoneNumber || '',
        dateOfBirth: user.dateOfBirth || '',
        profilePicture: user.profilePicture || ''
      });
      setImagePreview(user.profilePicture || '');
    }
  }, [user]);

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({ ...prev, [name]: value }));
  };

  const handleImageChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) {
        setError('Image size must be less than 5MB');
        return;
      }

      const reader = new FileReader();
      reader.onloadend = () => {
        const base64String = reader.result as string;
        setImagePreview(base64String);
        setFormData(prev => ({ ...prev, profilePicture: base64String }));
      };
      reader.readAsDataURL(file);
    }
  };


  const handleSaveProfile = async () => {
    setIsUpdating(true);
    setError('');
    setSuccess('');

    try {
      if (!user?.userId) {
        setError('User ID not found');
        return;
      }

      const updateData: any = {};
      if (formData.firstName !== user.firstName) updateData.firstName = formData.firstName;
      if (formData.lastName !== user.lastName) updateData.lastName = formData.lastName;
      if (formData.email !== user.email) updateData.email = formData.email;
      if (formData.phoneNumber !== user.phoneNumber) updateData.phoneNumber = formData.phoneNumber;
      if (formData.profilePicture !== user.profilePicture) updateData.profilePicture = formData.profilePicture;
      if (formData.dateOfBirth && formData.dateOfBirth !== user.dateOfBirth) {
        updateData.dateOfBirth = formData.dateOfBirth;
      }

      const response = await apiClient.put(`/api/v1/auth/update-profile/${user.userId}`, updateData);

      if (updateUser) {
        updateUser(response.data);
      }

      setSuccess('Profile updated successfully!');
      setIsEditing(false);
      setTimeout(() => setSuccess(''), 3000);
    } catch (error: any) {
      setError(error.response?.data || 'Failed to update profile');
    } finally {
      setIsUpdating(false);
    }
  };

  const handleDeactivateAccount = async () => {
    setIsUpdating(true);
    setError('');

    try {
      if (!user?.userGuid) {
        setError('User GUID not found');
        return;
      }

      await authApi.deactivateUser(user.userGuid);
      setSuccess('Account deactivated. You will be logged out.');
      setTimeout(() => {
        localStorage.removeItem('authToken');
        localStorage.removeItem('user');
        window.location.href = '/login';
      }, 2000);
    } catch (error: any) {
      setError(error.response?.data || 'Failed to deactivate account');
    } finally {
      setIsUpdating(false);
      setDeactivateDialogOpen(false);
    }
  };

  if (!user) {
    return (
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Paper sx={{ p: 4, textAlign: 'center' }}>
          <Typography>Please log in to view your profile</Typography>
        </Paper>
      </Container>
    );
  }

  return (
    <Container maxWidth="lg" sx={{ py: 4 }}>
      <Typography variant="h4" component="h1" gutterBottom>
        {t('profile', 'Profile')}
      </Typography>

      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {success && <Alert severity="success" sx={{ mb: 2 }}>{success}</Alert>}

      <Paper sx={{ p: 4 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 3 }}>
          <Typography variant="h6">Personal Information</Typography>
          {!isEditing ? (
            <Button
              startIcon={<EditIcon />}
              variant="outlined"
              onClick={() => setIsEditing(true)}
            >
              Edit Profile
            </Button>
          ) : (
            <Box>
              <Button
                startIcon={<SaveIcon />}
                variant="contained"
                onClick={handleSaveProfile}
                disabled={isUpdating}
                sx={{ mr: 1 }}
              >
                Save
              </Button>
              <Button
                startIcon={<CancelIcon />}
                variant="outlined"
                onClick={() => {
                  setIsEditing(false);
                  setFormData({
                    firstName: user.firstName || '',
                    lastName: user.lastName || '',
                    email: user.email || '',
                    phoneNumber: user.phoneNumber || '',
                    dateOfBirth: user.dateOfBirth || '',
                    profilePicture: user.profilePicture || ''
                  });
                  setImagePreview(user.profilePicture || '');
                }}
                disabled={isUpdating}
              >
                Cancel
              </Button>
            </Box>
          )}
        </Box>

        <Grid container spacing={3}>
          <Grid item xs={12} sm={4} sx={{ textAlign: 'center' }}>
            <Box sx={{ position: 'relative', display: 'inline-block' }}>
              <Avatar
                src={imagePreview || undefined}
                alt={`${user.firstName} ${user.lastName}`}
                sx={{ width: 150, height: 150, mb: 2 }}
              >
                {user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
              </Avatar>
              {isEditing && (
                <IconButton
                  component="label"
                  sx={{
                    position: 'absolute',
                    bottom: 20,
                    right: 0,
                    bgcolor: 'primary.main',
                    color: 'white',
                    '&:hover': { bgcolor: 'primary.dark' }
                  }}
                >
                  <PhotoCameraIcon />
                  <input
                    type="file"
                    hidden
                    accept="image/*"
                    onChange={handleImageChange}
                  />
                </IconButton>
              )}
            </Box>
            <Typography variant="body2" color="text.secondary">
              {isEditing ? 'Click camera to change photo' : ''}
            </Typography>
          </Grid>

          <Grid item xs={12} sm={8}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="First Name"
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Last Name"
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label="Email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Phone Number"
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label="Date of Birth"
                  name="dateOfBirth"
                  type="date"
                  value={formData.dateOfBirth ? new Date(formData.dateOfBirth).toISOString().split('T')[0] : ''}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                  InputLabelProps={{ shrink: true }}
                />
              </Grid>
            </Grid>
          </Grid>
        </Grid>


        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="h6" gutterBottom color="error">Danger Zone</Typography>
          <Button
            variant="outlined"
            color="error"
            onClick={() => setDeactivateDialogOpen(true)}
          >
            Deactivate Account
          </Button>
        </Box>
      </Paper>

      <Dialog open={deactivateDialogOpen} onClose={() => setDeactivateDialogOpen(false)}>
        <DialogTitle>Deactivate Account</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to deactivate your account? You won't be able to log in until you contact support to reactivate it.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeactivateDialogOpen(false)}>Cancel</Button>
          <Button onClick={handleDeactivateAccount} color="error" variant="contained" disabled={isUpdating}>
            {isUpdating ? <CircularProgress size={24} /> : 'Deactivate'}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default ProfilePage;

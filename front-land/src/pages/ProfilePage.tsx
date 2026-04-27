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
  CircularProgress,
  Switch,
  FormControlLabel
} from '@mui/material';
import DownloadIcon from '@mui/icons-material/Download';
import EditIcon from '@mui/icons-material/Edit';
import PhotoCameraIcon from '@mui/icons-material/PhotoCamera';
import SaveIcon from '@mui/icons-material/Save';
import CancelIcon from '@mui/icons-material/Cancel';
import VisibilityOffIcon from '@mui/icons-material/VisibilityOff';
import Tooltip from '@mui/material/Tooltip';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../shared/context/AuthContext';
import { authApi } from '../shared/api/auth';
import { apiClient } from '../shared/api/client';

const ProfilePage: React.FC = () => {
  const { t } = useTranslation(['common', 'profile']);
  const { user, updateUser } = useAuth();
  const [isEditing, setIsEditing] = useState(false);
  const [isUpdating, setIsUpdating] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');
  const [deactivateDialogOpen, setDeactivateDialogOpen] = useState(false);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [changePasswordDialogOpen, setChangePasswordDialogOpen] = useState(false);
  const [passwordForm, setPasswordForm] = useState({ oldPassword: '', newPassword: '', confirmPassword: '' });
  const [passwordError, setPasswordError] = useState('');
  const [passwordUpdating, setPasswordUpdating] = useState(false);

  const handleDeleteAccount = async () => {
    setIsUpdating(true);
    setError('');

    try {
      if (!user?.userGuid) {
        setError('User GUID not found');
        return;
      }

      await authApi.deleteUser({ userGuid: user.userGuid });
      setSuccess('Account deleted. You will be logged out.');
      setTimeout(() => {
        sessionStorage.removeItem('authToken');
        sessionStorage.removeItem('refreshToken');
        sessionStorage.removeItem('user');
        window.location.href = '/login';
      }, 2000);
    } catch (error: any) {
      setError(error.response?.data || 'Failed to delete account');
    } finally {
      setIsUpdating(false);
      setDeleteDialogOpen(false);
    }
  };

  const [formData, setFormData] = useState({
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    email: user?.email || '',
    phoneNumber: user?.phoneNumber || '',
    dateOfBirth: user?.dateOfBirth || '',
    profilePicture: user?.profilePicture || ''
  });

  const [privacySettings, setPrivacySettings] = useState({
    analyticsConsent: user?.analyticsConsent ?? true,
    chatHistoryConsent: user?.chatHistoryConsent ?? true,
    profileVisibility: user?.profileVisibility ?? true,
    isIncognito: user?.isIncognito ?? false,
  });
  const [privacyUpdating, setPrivacyUpdating] = useState(false);

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
      setPrivacySettings({
        analyticsConsent: user.analyticsConsent ?? true,
        chatHistoryConsent: user.chatHistoryConsent ?? true,
        profileVisibility: user.profileVisibility ?? true,
        isIncognito: user.isIncognito ?? false,
      });
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
        setError(t('profile:imageSizeError'));
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
        sessionStorage.removeItem('authToken');
        sessionStorage.removeItem('refreshToken');
        sessionStorage.removeItem('user');
        window.location.href = '/login';
      }, 2000);
    } catch (error: any) {
      setError(error.response?.data || 'Failed to deactivate account');
    } finally {
      setDeactivateDialogOpen(false);
    }
  };


  const handlePrivacyChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, checked } = e.target;
    setPrivacySettings(prev => ({ ...prev, [name]: checked }));
  };

  const handleSavePrivacy = async () => {
    if (!user?.userId) return;
    setPrivacyUpdating(true);
    try {
      const updatedUser = await authApi.updatePrivacySettings(user.userId, privacySettings);
      if (updateUser) {
        updateUser(updatedUser);
      }
      setSuccess('Privacy settings updated successfully');
      setTimeout(() => setSuccess(''), 3000);
    } catch (err) {
      setError('Failed to update privacy settings');
    } finally {
      setPrivacyUpdating(false);
    }
  };

  const handleExportData = async () => {
    if (!user?.userId) return;
    try {
      const data = await authApi.exportUserData(user.userId);
      const dataStr = "data:text/json;charset=utf-8," + encodeURIComponent(JSON.stringify(data, null, 2));
      const downloadAnchorNode = document.createElement('a');
      downloadAnchorNode.setAttribute("href", dataStr);
      downloadAnchorNode.setAttribute("download", `landlord_data_${user.userId}_${new Date().toISOString().split('T')[0]}.json`);
      document.body.appendChild(downloadAnchorNode);
      downloadAnchorNode.click();
      downloadAnchorNode.remove();
      setSuccess('Data exported successfully');
      setTimeout(() => setSuccess(''), 3000);
    } catch (error) {
      setError('Failed to export data');
    }
  };

  const handleChangePassword = async () => {
    setPasswordError('');
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      setPasswordError('Passwords do not match');
      return;
    }
    if (passwordForm.newPassword.length < 6) {
      setPasswordError('New password must be at least 6 characters');
      return;
    }
    if (!user?.userGuid) return;
    setPasswordUpdating(true);
    try {
      await authApi.changePassword({
        userId: user.userGuid,
        oldPassword: passwordForm.oldPassword,
        newPassword: passwordForm.newPassword,
      });
      setSuccess('Password changed successfully');
      setChangePasswordDialogOpen(false);
      setPasswordForm({ oldPassword: '', newPassword: '', confirmPassword: '' });
      setTimeout(() => setSuccess(''), 3000);
    } catch (err: any) {
      setPasswordError(err.response?.data || 'Failed to change password');
    } finally {
      setPasswordUpdating(false);
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
          <Typography variant="h6">{t('profile:personalInfo')}</Typography>
          {!isEditing ? (
            <Button
              startIcon={<EditIcon />}
              variant="outlined"
              onClick={() => setIsEditing(true)}
            >
              {t('profile:editProfile')}
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
                {t('common:save')}
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
                {t('common:cancel')}
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
              {isEditing ? t('profile:clickToChangePhoto') : ''}
            </Typography>
          </Grid>

          <Grid item xs={12} sm={8}>
            <Grid container spacing={2}>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={t('profile:firstName')}
                  name="firstName"
                  value={formData.firstName}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={t('profile:lastName')}
                  name="lastName"
                  value={formData.lastName}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12}>
                <TextField
                  fullWidth
                  label={t('profile:email')}
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
                  label={t('profile:phoneNumber')}
                  name="phoneNumber"
                  value={formData.phoneNumber}
                  onChange={handleInputChange}
                  disabled={!isEditing}
                />
              </Grid>
              <Grid item xs={12} sm={6}>
                <TextField
                  fullWidth
                  label={t('profile:dateOfBirth')}
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
          <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 2 }}>
            <Typography variant="h6">{t('profile:privacyData')}</Typography>
            <Button
              variant="outlined"
              startIcon={<DownloadIcon />}
              onClick={handleExportData}
            >
              {t('profile:exportData')}
            </Button>
          </Box>
          <Grid container spacing={2}>
            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={privacySettings.analyticsConsent}
                    onChange={handlePrivacyChange}
                    name="analyticsConsent"
                  />
                }
                label={t('profile:allowAnalytics')}
              />
            </Grid>
            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={privacySettings.chatHistoryConsent}
                    onChange={handlePrivacyChange}
                    name="chatHistoryConsent"
                  />
                }
                label={t('profile:saveChatHistory')}
              />
            </Grid>
            <Grid item xs={12}>
              <FormControlLabel
                control={
                  <Switch
                    checked={privacySettings.profileVisibility}
                    onChange={handlePrivacyChange}
                    name="profileVisibility"
                  />
                }
                label={t('profile:profileVisibility')}
              />
            </Grid>
            <Grid item xs={12}>
              <Tooltip
                title={!(user?.hasPersonalAnalytics || user?.hasLandlordAnalytics) ? 'Incognito Mode is a premium feature' : ''}
                arrow
              >
                <span>
                  <FormControlLabel
                    control={
                      <Switch
                        checked={privacySettings.isIncognito}
                        onChange={handlePrivacyChange}
                        name="isIncognito"
                        color="default"
                        disabled={!(user?.hasPersonalAnalytics || user?.hasLandlordAnalytics)}
                      />
                    }
                    label={
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                        <VisibilityOffIcon fontSize="small" sx={{ color: privacySettings.isIncognito ? 'text.primary' : 'text.disabled' }} />
                        <Box>
                          <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5 }}>
                            <span>{t('profile:incognitoMode')}</span>
                            {!(user?.hasPersonalAnalytics || user?.hasLandlordAnalytics) && (
                              <Box component="span" sx={{ fontSize: 11, color: 'warning.main', fontWeight: 600, ml: 0.5 }}>
                                PREMIUM
                              </Box>
                            )}
                          </Box>
                          <Box component="span" sx={{ fontSize: 12, color: 'text.secondary', display: 'block' }}>
                            {t('profile:incognitoDesc')}
                          </Box>
                        </Box>
                      </Box>
                    }
                  />
                </span>
              </Tooltip>
            </Grid>
            <Grid item xs={12}>
              <Button
                variant="contained"
                onClick={handleSavePrivacy}
                disabled={privacyUpdating}
                startIcon={<SaveIcon />}
              >
                {privacyUpdating ? t('common:loading') : t('profile:savePrivacy')}
              </Button>
            </Grid>
          </Grid>
        </Box>


        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="h6" gutterBottom>{t('profile:security', { defaultValue: 'Security' })}</Typography>
          <Button
            variant="outlined"
            onClick={() => setChangePasswordDialogOpen(true)}
          >
            {t('profile:changePassword', { defaultValue: 'Change Password' })}
          </Button>
        </Box>

        <Box sx={{ mt: 4, pt: 3, borderTop: 1, borderColor: 'divider' }}>
          <Typography variant="h6" gutterBottom color="error">{t('profile:dangerZone')}</Typography>
          <Button
            variant="outlined"
            color="error"
            onClick={() => setDeactivateDialogOpen(true)}
            sx={{ mr: 2 }}
          >
            {t('profile:deactivateAccount')}
          </Button>
          <Button
            variant="contained"
            color="error"
            onClick={() => setDeleteDialogOpen(true)}
          >
            {t('profile:deleteAccount')}
          </Button>
        </Box>
      </Paper>

      <Dialog open={changePasswordDialogOpen} onClose={() => { setChangePasswordDialogOpen(false); setPasswordError(''); }}>
        <DialogTitle>{t('profile:changePassword', { defaultValue: 'Change Password' })}</DialogTitle>
        <DialogContent sx={{ pt: 2, minWidth: 360 }}>
          {passwordError && <Alert severity="error" sx={{ mb: 2 }}>{passwordError}</Alert>}
          <TextField
            fullWidth
            label={t('profile:currentPassword', { defaultValue: 'Current Password' })}
            type="password"
            value={passwordForm.oldPassword}
            onChange={(e) => setPasswordForm(p => ({ ...p, oldPassword: e.target.value }))}
            sx={{ mb: 2 }}
          />
          <TextField
            fullWidth
            label={t('profile:newPassword', { defaultValue: 'New Password' })}
            type="password"
            value={passwordForm.newPassword}
            onChange={(e) => setPasswordForm(p => ({ ...p, newPassword: e.target.value }))}
            sx={{ mb: 2 }}
          />
          <TextField
            fullWidth
            label={t('profile:confirmNewPassword', { defaultValue: 'Confirm New Password' })}
            type="password"
            value={passwordForm.confirmPassword}
            onChange={(e) => setPasswordForm(p => ({ ...p, confirmPassword: e.target.value }))}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={() => { setChangePasswordDialogOpen(false); setPasswordError(''); }}>{t('common:cancel')}</Button>
          <Button onClick={handleChangePassword} variant="contained" disabled={passwordUpdating}>
            {passwordUpdating ? <CircularProgress size={24} /> : t('common:save')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deactivateDialogOpen} onClose={() => setDeactivateDialogOpen(false)}>
        <DialogTitle>{t('profile:deactivateConfirmTitle')}</DialogTitle>
        <DialogContent>
          <Typography>
            {t('profile:deactivateConfirmMsg')}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeactivateDialogOpen(false)}>{t('common:cancel')}</Button>
          <Button onClick={handleDeactivateAccount} color="error" variant="contained" disabled={isUpdating}>
            {isUpdating ? <CircularProgress size={24} /> : t('profile:deactivate')}
          </Button>
        </DialogActions>
      </Dialog>

      <Dialog open={deleteDialogOpen} onClose={() => setDeleteDialogOpen(false)}>
        <DialogTitle>{t('profile:deleteConfirmTitle')}</DialogTitle>
        <DialogContent>
          <Typography>
            {t('profile:deleteConfirmMsg')}
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteDialogOpen(false)}>{t('common:cancel')}</Button>
          <Button onClick={handleDeleteAccount} color="error" variant="contained" disabled={isUpdating}>
            {isUpdating ? <CircularProgress size={24} /> : t('profile:deleteForever')}
          </Button>
        </DialogActions>
      </Dialog>
    </Container>
  );
};

export default ProfilePage;

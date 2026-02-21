import React from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    Box,
    Typography,
    Avatar,
    Rating,
    Divider,
    CircularProgress
} from '@mui/material';
import { useQuery } from '@tanstack/react-query';
import { usersApi } from '../../shared/api/usersApi';
import EmailIcon from '@mui/icons-material/Email';
import PhoneIcon from '@mui/icons-material/Phone';
import StarIcon from '@mui/icons-material/Star';

interface LandlordProfileModalProps {
    open: boolean;
    onClose: () => void;
    userId: number;
}

const LandlordProfileModal: React.FC<LandlordProfileModalProps> = ({ open, onClose, userId }) => {
    const { data: profile, isLoading } = useQuery({
        queryKey: ['user-profile', userId],
        queryFn: () => usersApi.getProfile(userId),
        enabled: open && !!userId,
    });

    return (
        <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
            <DialogTitle>
                <Typography variant="h5" component="div">
                    Landlord Profile
                </Typography>
            </DialogTitle>
            <DialogContent>
                {isLoading ? (
                    <Box display="flex" justifyContent="center" py={4}>
                        <CircularProgress />
                    </Box>
                ) : profile ? (
                    <Box>
                        {/* Profile Header */}
                        <Box display="flex" alignItems="center" gap={2} mb={3}>
                            <Avatar
                                src={profile.profilePicture || undefined}
                                sx={{ width: 80, height: 80 }}
                            >
                                {profile.firstName?.[0]}{profile.lastName?.[0]}
                            </Avatar>
                            <Box flex={1}>
                                <Typography variant="h6">
                                    {profile.firstName} {profile.lastName}
                                </Typography>
                                <Typography variant="body2" color="text.secondary">
                                    {profile.roleName || 'Landlord'}
                                </Typography>
                            </Box>
                        </Box>

                        {/* Rating Section */}
                        {profile.averageRating !== null && profile.averageRating !== undefined && (
                            <Box mb={3}>
                                <Box display="flex" alignItems="center" gap={1} mb={1}>
                                    <Rating
                                        value={profile.averageRating}
                                        precision={0.1}
                                        readOnly
                                        icon={<StarIcon fontSize="inherit" />}
                                    />
                                    <Typography variant="body2" color="text.secondary">
                                        {profile.averageRating.toFixed(1)} ({profile.reviewCount} {profile.reviewCount === 1 ? 'review' : 'reviews'})
                                    </Typography>
                                </Box>
                            </Box>
                        )}

                        {profile.reviewCount === 0 && (
                            <Box mb={3}>
                                <Typography variant="body2" color="text.secondary">
                                    No reviews yet
                                </Typography>
                            </Box>
                        )}

                        <Divider sx={{ my: 2 }} />

                        {/* Contact Information */}
                        <Box>
                            <Typography variant="subtitle2" gutterBottom fontWeight="bold">
                                Contact Information
                            </Typography>

                            {profile.email && (
                                <Box display="flex" alignItems="center" gap={1} mb={1}>
                                    <EmailIcon fontSize="small" color="action" />
                                    <Typography variant="body2">{profile.email}</Typography>
                                </Box>
                            )}

                            {profile.phoneNumber && (
                                <Box display="flex" alignItems="center" gap={1}>
                                    <PhoneIcon fontSize="small" color="action" />
                                    <Typography variant="body2">{profile.phoneNumber}</Typography>
                                </Box>
                            )}
                        </Box>
                    </Box>
                ) : (
                    <Typography color="error">Failed to load profile</Typography>
                )}
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
};

export default LandlordProfileModal;

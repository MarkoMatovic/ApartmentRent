import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  Box,
  Container,
  Typography,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Paper,
  Chip,
  IconButton,
  Menu,
  MenuItem,
  TextField,
  InputAdornment,
} from '@mui/material';
import { Search as SearchIcon, MoreVert as MoreVertIcon, Visibility as VisibilityIcon, Cancel as CancelIcon } from '@mui/icons-material';
import { useState } from 'react';
import { applicationsService } from '../api/applicationsService';
import { useAuthStore } from '@/features/auth/store/authStore';
import { LoadingSpinner } from '@/shared/components/ui/LoadingSpinner';
import { ErrorAlert } from '@/shared/components/ui/ErrorAlert';
import { EmptyState } from '@/shared/components/ui/EmptyState';
import { AppLayout } from '@/shared/components/layout/AppLayout';
import { format } from 'date-fns';
import { ApartmentApplicationWithDetailsDto } from '../types/applications';

const ApplicationsPage = () => {
  const navigate = useNavigate();
  const { userId } = useAuthStore();
  const queryClient = useQueryClient();
  const [anchorEl, setAnchorEl] = useState<null | HTMLElement>(null);
  const [selectedApplication, setSelectedApplication] = useState<ApartmentApplicationWithDetailsDto | null>(null);
  const [searchQuery, setSearchQuery] = useState('');

  const {
    data: applications,
    isLoading,
    error,
    refetch,
  } = useQuery({
    queryKey: ['user-applications', userId],
    queryFn: () => applicationsService.getUserApplications(userId || 0),
    enabled: !!userId && userId > 0,
  });

  const cancelMutation = useMutation({
    mutationFn: (applicationId: number) => applicationsService.cancelApplication(applicationId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['user-applications', userId] });
      setAnchorEl(null);
    },
  });

  const handleMenuOpen = (event: React.MouseEvent<HTMLElement>, application: ApartmentApplicationWithDetailsDto) => {
    setAnchorEl(event.currentTarget);
    setSelectedApplication(application);
  };

  const handleMenuClose = () => {
    setAnchorEl(null);
    setSelectedApplication(null);
  };

  const handleViewApartment = (apartmentId: number) => {
    navigate(`/apartments/${apartmentId}`);
    handleMenuClose();
  };

  const handleCancelApplication = () => {
    if (selectedApplication) {
      cancelMutation.mutate(selectedApplication.applicationId);
    }
  };

  const formatDate = (dateString: string) => {
    try {
      const date = new Date(dateString);
      return format(date, 'MM/dd/yyyy');
    } catch {
      return dateString;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status?.toLowerCase()) {
      case 'pending':
        return '#ff9800'; // Orange
      case 'approved':
        return '#4caf50'; // Green
      case 'rejected':
        return '#f44336'; // Red
      default:
        return '#9e9e9e'; // Grey
    }
  };

  if (!userId || userId === 0) {
    return (
      <AppLayout>
        <ErrorAlert
          message="User ID is not available. Please ensure you are logged in correctly."
          title="Authentication Error"
        />
      </AppLayout>
    );
  }

  if (isLoading) {
    return (
      <AppLayout>
        <LoadingSpinner fullScreen />
      </AppLayout>
    );
  }

  if (error) {
    return (
      <AppLayout>
        <ErrorAlert
          message={error instanceof Error ? error.message : 'Failed to load applications'}
          onRetry={() => refetch()}
        />
      </AppLayout>
    );
  }

  const filteredApplications = applications?.filter((app) => {
    if (!searchQuery) return true;
    const query = searchQuery.toLowerCase();
    return (
      app.apartment?.title?.toLowerCase().includes(query) ||
      app.apartment?.address?.toLowerCase().includes(query) ||
      app.apartment?.city?.toLowerCase().includes(query)
    );
  }) || [];

  return (
    <AppLayout showSearch>
      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Typography variant="h4" component="h1" gutterBottom sx={{ fontWeight: 'bold', mb: 3 }}>
          My Apartment Applications
        </Typography>

        {!applications || applications.length === 0 ? (
          <EmptyState message="No applications found" />
        ) : (
          <>
            <Box sx={{ mb: 3 }}>
              <TextField
                fullWidth
                placeholder="Search apartments..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                InputProps={{
                  startAdornment: (
                    <InputAdornment position="start">
                      <SearchIcon />
                    </InputAdornment>
                  ),
                }}
                sx={{ maxWidth: 400 }}
              />
            </Box>

            <TableContainer component={Paper} elevation={2}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell sx={{ fontWeight: 'bold' }}>Apartment</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Address</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Rent</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Applied Date</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Status</TableCell>
                    <TableCell sx={{ fontWeight: 'bold' }}>Actions</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {filteredApplications.map((application) => (
                    <TableRow key={application.applicationId} hover>
                      <TableCell>
                        {application.apartment?.title || 'N/A'}
                      </TableCell>
                      <TableCell>
                        {application.apartment?.address || 'N/A'}
                      </TableCell>
                      <TableCell>
                        {application.apartment?.rent ? `€${application.apartment.rent.toFixed(0)}` : 'N/A'}
                      </TableCell>
                      <TableCell>
                        {formatDate(application.applicationDate)}
                      </TableCell>
                      <TableCell>
                        <Chip
                          label={application.status || 'Pending'}
                          sx={{
                            bgcolor: getStatusColor(application.status || 'pending'),
                            color: 'white',
                            fontWeight: 'bold',
                          }}
                          size="small"
                        />
                      </TableCell>
                      <TableCell>
                        <IconButton
                          size="small"
                          onClick={(e) => handleMenuOpen(e, application)}
                        >
                          <MoreVertIcon />
                        </IconButton>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>

            <Menu
              anchorEl={anchorEl}
              open={Boolean(anchorEl)}
              onClose={handleMenuClose}
            >
              <MenuItem
                onClick={() => selectedApplication && handleViewApartment(selectedApplication.apartmentId)}
              >
                <VisibilityIcon sx={{ mr: 1, fontSize: 20 }} />
                View Apartment
              </MenuItem>
              <MenuItem
                onClick={handleCancelApplication}
                sx={{ color: 'error.main' }}
              >
                <CancelIcon sx={{ mr: 1, fontSize: 20 }} />
                Cancel Application
              </MenuItem>
            </Menu>
          </>
        )}
      </Container>
    </AppLayout>
  );
};

export default ApplicationsPage;


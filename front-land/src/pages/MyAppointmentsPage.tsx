import React from 'react';
import {
    Container,
    Typography,
    Box,
    Card,
    CardContent,
    Chip,
    Button,
    Grid,
    CircularProgress,
    Alert,
} from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { appointmentsApi } from '../shared/api/appointments';
import { AppointmentStatus } from '../shared/types/appointment';
import { format, parseISO } from 'date-fns';
import { CalendarToday as CalendarIcon, LocationOn as LocationIcon } from '@mui/icons-material';

const MyAppointmentsPage: React.FC = () => {
    const { t } = useTranslation(['common', 'apartments']);
    const navigate = useNavigate();
    const queryClient = useQueryClient();

    const { data: appointments, isLoading } = useQuery({
        queryKey: ['my-appointments'],
        queryFn: appointmentsApi.getMyAppointments,
    });

    const cancelMutation = useMutation({
        mutationFn: appointmentsApi.cancel,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
        },
    });

    const getStatusColor = (status: AppointmentStatus) => {
        switch (status) {
            case AppointmentStatus.Confirmed:
                return 'success';
            case AppointmentStatus.Pending:
                return 'warning';
            case AppointmentStatus.Cancelled:
            case AppointmentStatus.Rejected:
                return 'error';
            case AppointmentStatus.Completed:
                return 'info';
            default:
                return 'default';
        }
    };

    const getStatusLabel = (status: AppointmentStatus) => {
        const statusKey = AppointmentStatus[status].toLowerCase();
        return t(`apartments:status.${statusKey}`, { defaultValue: AppointmentStatus[status] });
    };

    if (isLoading) {
        return (
            <Container maxWidth="lg" sx={{ py: 4, display: 'flex', justifyContent: 'center' }}>
                <CircularProgress />
            </Container>
        );
    }

    return (
        <Container maxWidth="lg" sx={{ py: 4 }}>
            <Typography variant="h4" component="h1" gutterBottom>
                {t('apartments:myAppointments', { defaultValue: 'My Appointments' })}
            </Typography>

            {appointments && appointments.length > 0 ? (
                <Grid container spacing={3} sx={{ mt: 2 }}>
                    {appointments.map((appointment) => (
                        <Grid item xs={12} md={6} key={appointment.appointmentId}>
                            <Card>
                                <CardContent>
                                    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', mb: 2 }}>
                                        <Typography variant="h6" component="h2">
                                            {appointment.apartmentTitle}
                                        </Typography>
                                        <Chip
                                            label={getStatusLabel(appointment.status)}
                                            color={getStatusColor(appointment.status)}
                                            size="small"
                                        />
                                    </Box>

                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 1 }}>
                                        <LocationIcon fontSize="small" color="action" />
                                        <Typography variant="body2" color="text.secondary">
                                            {appointment.apartmentAddress}
                                        </Typography>
                                    </Box>

                                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mb: 2 }}>
                                        <CalendarIcon fontSize="small" color="action" />
                                        <Typography variant="body2" color="text.secondary">
                                            {format(parseISO(appointment.appointmentDate), 'PPP p')}
                                        </Typography>
                                    </Box>

                                    {appointment.landlordName && (
                                        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
                                            {t('apartments:appointmentWith', { defaultValue: 'With' })}: {appointment.landlordName}
                                        </Typography>
                                    )}

                                    {appointment.tenantNotes && (
                                        <Box sx={{ mt: 2, p: 1.5, bgcolor: 'background.default', borderRadius: 1 }}>
                                            <Typography variant="caption" color="text.secondary">
                                                {t('apartments:appointmentNotes', { defaultValue: 'Notes' })}:
                                            </Typography>
                                            <Typography variant="body2">{appointment.tenantNotes}</Typography>
                                        </Box>
                                    )}

                                    {appointment.landlordNotes && (
                                        <Box sx={{ mt: 2, p: 1.5, bgcolor: 'info.light', borderRadius: 1 }}>
                                            <Typography variant="caption" color="text.secondary">
                                                {t('common:landlordNotes', { defaultValue: 'Landlord Notes' })}:
                                            </Typography>
                                            <Typography variant="body2">{appointment.landlordNotes}</Typography>
                                        </Box>
                                    )}

                                    <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            onClick={() => navigate(`/apartments/${appointment.apartmentId}`)}
                                        >
                                            {t('common:viewDetails', { defaultValue: 'View Apartment' })}
                                        </Button>
                                        {appointment.status === AppointmentStatus.Pending && (
                                            <Button
                                                variant="outlined"
                                                color="error"
                                                size="small"
                                                onClick={() => cancelMutation.mutate(appointment.appointmentId)}
                                                disabled={cancelMutation.isPending}
                                            >
                                                {t('apartments:cancelAppointment', { defaultValue: 'Cancel' })}
                                            </Button>
                                        )}
                                    </Box>
                                </CardContent>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            ) : (
                <Alert severity="info" sx={{ mt: 3 }}>
                    {t('apartments:noAppointments', { defaultValue: 'No appointments yet' })}
                </Alert>
            )}
        </Container>
    );
};

export default MyAppointmentsPage;

import React, { useState } from 'react';
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
    TextField,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
} from '@mui/material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { appointmentsApi } from '../shared/api/appointments';
import { AppointmentStatus, AppointmentDto } from '../shared/types/appointment';
import { format, parseISO } from 'date-fns';
import { CalendarToday as CalendarIcon, LocationOn as LocationIcon, Person as PersonIcon } from '@mui/icons-material';

const LandlordAppointmentsPage: React.FC = () => {
    const { t } = useTranslation(['common', 'apartments']);
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [selectedAppointment, setSelectedAppointment] = useState<AppointmentDto | null>(null);
    const [landlordNotes, setLandlordNotes] = useState('');
    const [actionType, setActionType] = useState<'confirm' | 'reject' | null>(null);

    const { data: appointments, isLoading } = useQuery({
        queryKey: ['landlord-appointments'],
        queryFn: appointmentsApi.getLandlordAppointments,
    });

    const updateStatusMutation = useMutation({
        mutationFn: ({ id, status, notes }: { id: number; status: AppointmentStatus; notes?: string }) =>
            appointmentsApi.updateStatus(id, { status, landlordNotes: notes }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['landlord-appointments'] });
            handleCloseDialog();
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

    const handleOpenDialog = (appointment: AppointmentDto, action: 'confirm' | 'reject') => {
        setSelectedAppointment(appointment);
        setActionType(action);
        setLandlordNotes('');
    };

    const handleCloseDialog = () => {
        setSelectedAppointment(null);
        setActionType(null);
        setLandlordNotes('');
    };

    const handleConfirmAction = () => {
        if (!selectedAppointment || !actionType) return;

        const status = actionType === 'confirm' ? AppointmentStatus.Confirmed : AppointmentStatus.Rejected;
        updateStatusMutation.mutate({
            id: selectedAppointment.appointmentId,
            status,
            notes: landlordNotes || undefined,
        });
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
                {t('apartments:landlordAppointments', { defaultValue: 'Appointment Requests' })}
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
                                        <PersonIcon fontSize="small" color="action" />
                                        <Typography variant="body2" color="text.secondary">
                                            {appointment.tenantName} ({appointment.tenantEmail})
                                        </Typography>
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

                                    {appointment.tenantNotes && (
                                        <Box sx={{ mt: 2, p: 1.5, bgcolor: 'background.default', borderRadius: 1 }}>
                                            <Typography variant="caption" color="text.secondary">
                                                {t('common:tenantNotes', { defaultValue: 'Tenant Notes' })}:
                                            </Typography>
                                            <Typography variant="body2">{appointment.tenantNotes}</Typography>
                                        </Box>
                                    )}

                                    {appointment.status === AppointmentStatus.Pending && (
                                        <Box sx={{ mt: 2, display: 'flex', gap: 1 }}>
                                            <Button
                                                variant="contained"
                                                color="success"
                                                size="small"
                                                onClick={() => handleOpenDialog(appointment, 'confirm')}
                                            >
                                                {t('apartments:confirmAppointmentAction', { defaultValue: 'Confirm' })}
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                color="error"
                                                size="small"
                                                onClick={() => handleOpenDialog(appointment, 'reject')}
                                            >
                                                {t('apartments:rejectAppointment', { defaultValue: 'Reject' })}
                                            </Button>
                                        </Box>
                                    )}
                                </CardContent>
                            </Card>
                        </Grid>
                    ))}
                </Grid>
            ) : (
                <Alert severity="info" sx={{ mt: 3 }}>
                    {t('apartments:noAppointments', { defaultValue: 'No appointment requests yet' })}
                </Alert>
            )}

            {/* Confirm/Reject Dialog */}
            <Dialog open={!!selectedAppointment} onClose={handleCloseDialog} maxWidth="sm" fullWidth>
                <DialogTitle>
                    {actionType === 'confirm'
                        ? t('apartments:confirmAppointmentAction', { defaultValue: 'Confirm Appointment' })
                        : t('apartments:rejectAppointment', { defaultValue: 'Reject Appointment' })}
                </DialogTitle>
                <DialogContent>
                    <TextField
                        fullWidth
                        multiline
                        rows={3}
                        label={t('common:landlordNotes', { defaultValue: 'Notes (optional)' })}
                        value={landlordNotes}
                        onChange={(e) => setLandlordNotes(e.target.value)}
                        sx={{ mt: 2 }}
                    />
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleCloseDialog}>{t('common:cancel', { defaultValue: 'Cancel' })}</Button>
                    <Button
                        onClick={handleConfirmAction}
                        variant="contained"
                        color={actionType === 'confirm' ? 'success' : 'error'}
                        disabled={updateStatusMutation.isPending}
                    >
                        {updateStatusMutation.isPending ? <CircularProgress size={24} /> : t('common:confirm', { defaultValue: 'Confirm' })}
                    </Button>
                </DialogActions>
            </Dialog>
        </Container>
    );
};

export default LandlordAppointmentsPage;

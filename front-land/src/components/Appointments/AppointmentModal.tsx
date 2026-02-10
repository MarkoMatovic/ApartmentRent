import React, { useState } from 'react';
import {
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    Button,
    TextField,
    Box,
    Typography,
    Grid,
    Chip,
    CircularProgress,
    Alert,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appointmentsApi } from '../../shared/api/appointments';
import type { AvailableSlotDto } from '../../shared/types/appointment';
import { format, parseISO } from 'date-fns';

interface AppointmentModalProps {
    open: boolean;
    onClose: () => void;
    apartmentId: number;
    apartmentTitle: string;
}

const AppointmentModal: React.FC<AppointmentModalProps> = ({
    open,
    onClose,
    apartmentId,
    apartmentTitle,
}) => {
    const { t } = useTranslation(['common', 'apartments']);
    const queryClient = useQueryClient();
    const [selectedDate, setSelectedDate] = useState<Date | null>(new Date());
    const [selectedSlot, setSelectedSlot] = useState<AvailableSlotDto | null>(null);
    const [notes, setNotes] = useState('');

    // Fetch available slots when date changes
    const { data: slots, isLoading: slotsLoading } = useQuery({
        queryKey: ['available-slots', apartmentId, selectedDate?.toISOString()],
        queryFn: () =>
            selectedDate
                ? appointmentsApi.getAvailableSlots(apartmentId, format(selectedDate, 'yyyy-MM-dd'))
                : Promise.resolve([]),
        enabled: !!selectedDate && open,
    });

    // Create appointment mutation
    const createMutation = useMutation({
        mutationFn: appointmentsApi.create,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['my-appointments'] });
            queryClient.invalidateQueries({ queryKey: ['available-slots'] });
            onClose();
            setSelectedSlot(null);
            setNotes('');
        },
    });

    const handleSubmit = () => {
        if (!selectedSlot) return;

        createMutation.mutate({
            apartmentId,
            appointmentDate: selectedSlot.startTime,
            tenantNotes: notes || undefined,
        });
    };

    const handleClose = () => {
        setSelectedSlot(null);
        setNotes('');
        onClose();
    };

    return (
        <LocalizationProvider dateAdapter={AdapterDateFns}>
            <Dialog open={open} onClose={handleClose} maxWidth="md" fullWidth>
                <DialogTitle>
                    {t('apartments:scheduleViewing', { defaultValue: 'Schedule Viewing' })}
                </DialogTitle>
                <DialogContent>
                    <Box sx={{ mt: 2 }}>
                        <Typography variant="subtitle1" gutterBottom>
                            {apartmentTitle}
                        </Typography>

                        {/* Date Picker */}
                        <Box sx={{ mt: 3, mb: 3 }}>
                            <DatePicker
                                label={t('apartments:selectDate', { defaultValue: 'Select Date' })}
                                value={selectedDate}
                                onChange={(newValue) => {
                                    setSelectedDate(newValue);
                                    setSelectedSlot(null);
                                }}
                                minDate={new Date()}
                                slotProps={{
                                    textField: {
                                        fullWidth: true,
                                    },
                                }}
                            />
                        </Box>

                        {/* Time Slots */}
                        {selectedDate && (
                            <Box>
                                <Typography variant="subtitle2" gutterBottom>
                                    {t('apartments:availableSlots', { defaultValue: 'Available Time Slots' })}
                                </Typography>

                                {slotsLoading ? (
                                    <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
                                        <CircularProgress />
                                    </Box>
                                ) : slots && slots.length > 0 ? (
                                    <Grid container spacing={1} sx={{ mt: 1 }}>
                                        {slots.map((slot, index) => (
                                            <Grid item xs={6} sm={4} md={3} key={index}>
                                                <Chip
                                                    label={format(parseISO(slot.startTime), 'HH:mm')}
                                                    onClick={() => slot.isAvailable && setSelectedSlot(slot)}
                                                    color={
                                                        selectedSlot?.startTime === slot.startTime
                                                            ? 'primary'
                                                            : slot.isAvailable
                                                                ? 'default'
                                                                : 'error'
                                                    }
                                                    variant={
                                                        selectedSlot?.startTime === slot.startTime ? 'filled' : 'outlined'
                                                    }
                                                    disabled={!slot.isAvailable}
                                                    sx={{
                                                        width: '100%',
                                                        cursor: slot.isAvailable ? 'pointer' : 'not-allowed',
                                                    }}
                                                />
                                            </Grid>
                                        ))}
                                    </Grid>
                                ) : (
                                    <Alert severity="info" sx={{ mt: 2 }}>
                                        {t('apartments:noSlotsAvailable', {
                                            defaultValue: 'No slots available for this date',
                                        })}
                                    </Alert>
                                )}
                            </Box>
                        )}

                        {/* Notes */}
                        {selectedSlot && (
                            <Box sx={{ mt: 3 }}>
                                <TextField
                                    fullWidth
                                    multiline
                                    rows={3}
                                    label={t('apartments:appointmentNotes', {
                                        defaultValue: 'Notes (optional)',
                                    })}
                                    value={notes}
                                    onChange={(e) => setNotes(e.target.value)}
                                    placeholder={t('apartments:appointmentNotesPlaceholder', {
                                        defaultValue: 'Any special requests or questions...',
                                    })}
                                />
                            </Box>
                        )}

                        {/* Error */}
                        {createMutation.isError && (
                            <Alert severity="error" sx={{ mt: 2 }}>
                                {t('common:error', { defaultValue: 'An error occurred. Please try again.' })}
                            </Alert>
                        )}
                    </Box>
                </DialogContent>
                <DialogActions>
                    <Button onClick={handleClose} disabled={createMutation.isPending}>
                        {t('common:cancel', { defaultValue: 'Cancel' })}
                    </Button>
                    <Button
                        onClick={handleSubmit}
                        variant="contained"
                        disabled={!selectedSlot || createMutation.isPending}
                    >
                        {createMutation.isPending ? (
                            <CircularProgress size={24} />
                        ) : (
                            t('apartments:confirmAppointment', { defaultValue: 'Confirm Appointment' })
                        )}
                    </Button>
                </DialogActions>
            </Dialog>
        </LocalizationProvider>
    );
};

export default AppointmentModal;

import React, { useState, useEffect } from 'react';
import {
    Container,
    Typography,
    Box,
    Card,
    CardContent,
    Button,
    Grid,
    CircularProgress,
    Alert,
    IconButton,
    Chip,
    Select,
    MenuItem,
    FormControl,
    InputLabel,
    Snackbar,
} from '@mui/material';
import { Add as AddIcon, Delete as DeleteIcon, AccessTime as ClockIcon, Save as SaveIcon } from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { appointmentsApi } from '../shared/api/appointments';
import type { LandlordAvailabilityDto, AvailabilitySlotInput } from '../shared/types/appointment';

const DAYS_OF_WEEK = [
    { value: 0 },
    { value: 1 },
    { value: 2 },
    { value: 3 },
    { value: 4 },
    { value: 5 },
    { value: 6 },
];

const TIME_OPTIONS: string[] = [];
for (let h = 7; h <= 21; h++) {
    for (const m of [0, 30]) {
        const hStr = String(h).padStart(2, '0');
        const mStr = String(m).padStart(2, '0');
        TIME_OPTIONS.push(`${hStr}:${mStr}:00`);
    }
}

function formatTime(t: string): string {
    const [h, m] = t.split(':');
    const hour = parseInt(h, 10);
    const ampm = hour >= 12 ? 'PM' : 'AM';
    const hour12 = hour % 12 || 12;
    return `${hour12}:${m} ${ampm}`;
}

const LandlordAvailabilityPage: React.FC = () => {
    const { t } = useTranslation('appointments');
    const queryClient = useQueryClient();
    const [slots, setSlots] = useState<AvailabilitySlotInput[]>([]);
    const [loaded, setLoaded] = useState(false);
    const [snackbar, setSnackbar] = useState<{ open: boolean; message: string; severity: 'success' | 'error' }>({
        open: false,
        message: '',
        severity: 'success',
    });

    const { data: availabilityData, isLoading } = useQuery({
        queryKey: ['my-availability'],
        queryFn: appointmentsApi.getMyAvailability,
        enabled: !loaded,
    });

    useEffect(() => {
        if (availabilityData && !loaded) {
            setSlots(
                availabilityData.map((d: LandlordAvailabilityDto) => ({
                    dayOfWeek: d.dayOfWeek,
                    startTime: d.startTime,
                    endTime: d.endTime,
                }))
            );
            setLoaded(true);
        }
    }, [availabilityData, loaded]);

    const saveMutation = useMutation({
        mutationFn: () => appointmentsApi.setMyAvailability({ slots }),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['my-availability'] });
            setSnackbar({ open: true, message: t('availability.saved'), severity: 'success' });
        },
        onError: () => {
            setSnackbar({ open: true, message: t('availability.saveFailed'), severity: 'error' });
        },
    });

    const addSlot = () => {
        setSlots((prev) => [
            ...prev,
            { dayOfWeek: 1, startTime: '09:00:00', endTime: '17:00:00' },
        ]);
    };

    const removeSlot = (index: number) => {
        setSlots((prev) => prev.filter((_, i) => i !== index));
    };

    const updateSlot = (index: number, field: keyof AvailabilitySlotInput, value: number | string) => {
        setSlots((prev) =>
            prev.map((slot, i) => (i === index ? { ...slot, [field]: value } : slot))
        );
    };

    // Group slots by day for display only
    const groupedByDay = DAYS_OF_WEEK.map((day) => ({
        ...day,
        label: t(`days.${day.value}`),
        slots: slots
            .map((s, i) => ({ slot: s, index: i }))
            .filter(({ slot }) => slot.dayOfWeek === day.value),
    }));

    if (isLoading) {
        return (
            <Box display="flex" justifyContent="center" alignItems="center" minHeight="60vh">
                <CircularProgress />
            </Box>
        );
    }

    return (
        <Container maxWidth="md" sx={{ py: 4 }}>
            <Box display="flex" alignItems="center" justifyContent="space-between" mb={3}>
                <Box display="flex" alignItems="center" gap={1}>
                    <ClockIcon color="primary" />
                    <Typography variant="h5" fontWeight={600}>
                        {t('availability.title')}
                    </Typography>
                </Box>
                <Button
                    variant="contained"
                    startIcon={saveMutation.isPending ? <CircularProgress size={18} color="inherit" /> : <SaveIcon />}
                    onClick={() => saveMutation.mutate()}
                    disabled={saveMutation.isPending}
                >
                    {saveMutation.isPending ? t('availability.saving') : t('availability.saveChanges')}
                </Button>
            </Box>

            <Alert severity="info" sx={{ mb: 3 }}>
                {t('availability.info')}
            </Alert>

            {/* Slots editor */}
            <Card variant="outlined" sx={{ mb: 3 }}>
                <CardContent>
                    <Box display="flex" justifyContent="space-between" alignItems="center" mb={2}>
                        <Typography variant="subtitle1" fontWeight={600}>
                            {t('availability.slots')}
                        </Typography>
                        <Button startIcon={<AddIcon />} onClick={addSlot} size="small" variant="outlined">
                            {t('availability.addSlot')}
                        </Button>
                    </Box>
                    {slots.length === 0 && (
                        <Typography color="text.secondary" textAlign="center" py={3}>
                            {t('availability.noSlots')}
                        </Typography>
                    )}
                    {slots.map((slot, index) => (
                        <Box
                            key={index}
                            sx={{
                                display: 'flex',
                                alignItems: 'center',
                                gap: 2,
                                mb: 2,
                                p: 2,
                                borderRadius: 2,
                                bgcolor: 'action.hover',
                            }}
                        >
                            <FormControl size="small" sx={{ minWidth: 130 }}>
                                <InputLabel>{t('availability.day')}</InputLabel>
                                <Select
                                    value={slot.dayOfWeek}
                                    label={t('availability.day')}
                                    onChange={(e) => updateSlot(index, 'dayOfWeek', Number(e.target.value))}
                                >
                                    {DAYS_OF_WEEK.map((d) => (
                                        <MenuItem key={d.value} value={d.value}>
                                            {t(`days.${d.value}`)}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <FormControl size="small" sx={{ minWidth: 110 }}>
                                <InputLabel>{t('availability.from')}</InputLabel>
                                <Select
                                    value={slot.startTime}
                                    label={t('availability.from')}
                                    onChange={(e) => updateSlot(index, 'startTime', e.target.value)}
                                >
                                    {TIME_OPTIONS.map((tOp) => (
                                        <MenuItem key={tOp} value={tOp}>
                                            {formatTime(tOp)}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <Typography color="text.secondary">–</Typography>
                            <FormControl size="small" sx={{ minWidth: 110 }}>
                                <InputLabel>{t('availability.to')}</InputLabel>
                                <Select
                                    value={slot.endTime}
                                    label={t('availability.to')}
                                    onChange={(e) => updateSlot(index, 'endTime', e.target.value)}
                                >
                                    {TIME_OPTIONS.filter((tOp) => tOp > slot.startTime).map((tOp) => (
                                        <MenuItem key={tOp} value={tOp}>
                                            {formatTime(tOp)}
                                        </MenuItem>
                                    ))}
                                </Select>
                            </FormControl>
                            <IconButton color="error" onClick={() => removeSlot(index)} size="small">
                                <DeleteIcon />
                            </IconButton>
                        </Box>
                    ))}
                </CardContent>
            </Card>

            {/* Preview grouped by day */}
            {slots.length > 0 && (
                <Card variant="outlined">
                    <CardContent>
                        <Typography variant="subtitle1" fontWeight={600} mb={2}>
                            {t('availability.weeklyPreview')}
                        </Typography>
                        <Grid container spacing={1}>
                            {groupedByDay
                                .filter((day) => day.slots.length > 0)
                                .map((day) => (
                                    <Grid item xs={12} sm={6} key={day.value}>
                                        <Box
                                            sx={{
                                                p: 1.5,
                                                borderRadius: 2,
                                                border: '1px solid',
                                                borderColor: 'divider',
                                            }}
                                        >
                                            <Typography variant="body2" fontWeight={600} mb={0.5}>
                                                {day.label}
                                            </Typography>
                                            {day.slots.map(({ slot, index: sIdx }) => (
                                                <Chip
                                                    key={sIdx}
                                                    label={`${formatTime(slot.startTime)} – ${formatTime(slot.endTime)}`}
                                                    size="small"
                                                    color="primary"
                                                    variant="outlined"
                                                    sx={{ mr: 0.5, mb: 0.5 }}
                                                />
                                            ))}
                                        </Box>
                                    </Grid>
                                ))}
                        </Grid>
                    </CardContent>
                </Card>
            )}

            <Snackbar
                open={snackbar.open}
                autoHideDuration={3000}
                onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
                anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
            >
                <Alert
                    severity={snackbar.severity}
                    onClose={() => setSnackbar((s) => ({ ...s, open: false }))}
                >
                    {snackbar.message}
                </Alert>
            </Snackbar>
        </Container>
    );
};

export default LandlordAvailabilityPage;

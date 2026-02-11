import React, { useState } from 'react';
import { useAuth } from '../shared/context/AuthContext';
import {
    Box,
    Typography,
    Paper,
    Table,
    TableBody,
    TableCell,
    TableContainer,
    TableHead,
    TableRow,
    Chip,
    IconButton,
    Button,
    Dialog,
    DialogTitle,
    DialogContent,
    DialogActions,
    TextField,
    MenuItem,
    CircularProgress,
    Alert,
} from '@mui/material';
import {
    Visibility as ViewIcon,
    CheckCircle as ResolveIcon,
    Delete as DeleteIcon,
} from '@mui/icons-material';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { format, parseISO } from 'date-fns';
import { reportsApi } from '../shared/api/reports';
import { ReportedMessage } from '../shared/types/message';

const ReportsPage: React.FC = () => {
    const { t } = useTranslation(['common', 'chat']);
    const { user } = useAuth();
    const queryClient = useQueryClient();
    const [selectedReport, setSelectedReport] = useState<ReportedMessage | null>(null);
    const [reviewDialogOpen, setReviewDialogOpen] = useState(false);
    const [adminNotes, setAdminNotes] = useState('');
    const [statusFilter, setStatusFilter] = useState<string>('All');

    const { data: reports, isLoading, error } = useQuery<ReportedMessage[]>({
        queryKey: ['abuse-reports'],
        queryFn: () => reportsApi.getAllReports(),
    });

    const reviewMutation = useMutation({
        mutationFn: ({ reportId, notes }: { reportId: number; notes: string }) =>
            reportsApi.reviewReport(reportId, user!.userId, notes),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['abuse-reports'] });
            setReviewDialogOpen(false);
            setSelectedReport(null);
            setAdminNotes('');
        },
    });

    const resolveMutation = useMutation({
        mutationFn: ({ reportId, notes }: { reportId: number; notes: string }) =>
            reportsApi.resolveReport(reportId, user!.userId, notes),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['abuse-reports'] });
            setReviewDialogOpen(false);
            setSelectedReport(null);
            setAdminNotes('');
        },
    });

    const deleteMutation = useMutation({
        mutationFn: reportsApi.deleteReport,
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['abuse-reports'] });
        },
    });

    const handleViewReport = (report: ReportedMessage) => {
        setSelectedReport(report);
        setReviewDialogOpen(true);
        setAdminNotes(report.adminNotes || '');
    };

    const handleReview = () => {
        if (selectedReport) {
            reviewMutation.mutate({ reportId: selectedReport.reportId, notes: adminNotes });
        }
    };

    const handleResolve = () => {
        if (selectedReport) {
            resolveMutation.mutate({ reportId: selectedReport.reportId, notes: adminNotes });
        }
    };

    const handleDelete = (reportId: number) => {
        if (window.confirm(t('chat:confirmDeleteReport'))) {
            deleteMutation.mutate(reportId);
        }
    };

    const getStatusColor = (status: string) => {
        switch (status) {
            case 'Pending':
                return 'warning';
            case 'Reviewed':
                return 'info';
            case 'Resolved':
                return 'success';
            default:
                return 'default';
        }
    };

    const filteredReports = reports?.filter((report: ReportedMessage) =>
        statusFilter === 'All' || report.status === statusFilter
    ) || [];

    if (isLoading) {
        return (
            <Box sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', height: '100vh' }}>
                <CircularProgress />
            </Box>
        );
    }

    if (error) {
        return (
            <Box sx={{ p: 3 }}>
                <Alert severity="error">{t('common:errorLoadingData')}</Alert>
            </Box>
        );
    }

    return (
        <Box sx={{ p: 3 }}>
            <Typography variant="h4" gutterBottom>
                {t('chat:abuseReports')}
            </Typography>

            {/* Filters */}
            <Box sx={{ mb: 3, display: 'flex', gap: 2, alignItems: 'center' }}>
                <TextField
                    select
                    label={t('chat:status')}
                    value={statusFilter}
                    onChange={(e) => setStatusFilter(e.target.value)}
                    sx={{ minWidth: 200 }}
                >
                    <MenuItem value="All">{t('common:all')}</MenuItem>
                    <MenuItem value="Pending">{t('chat:pending')}</MenuItem>
                    <MenuItem value="Reviewed">{t('chat:reviewed')}</MenuItem>
                    <MenuItem value="Resolved">{t('chat:resolved')}</MenuItem>
                </TextField>
                <Typography variant="body2" color="text.secondary">
                    {t('chat:totalReports', { count: filteredReports.length })}
                </Typography>
            </Box>

            {/* Reports Table */}
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>{t('chat:reportId')}</TableCell>
                            <TableCell>{t('chat:reportedUser')}</TableCell>
                            <TableCell>{t('chat:reportedBy')}</TableCell>
                            <TableCell>{t('chat:reason')}</TableCell>
                            <TableCell>{t('chat:status')}</TableCell>
                            <TableCell>{t('chat:createdDate')}</TableCell>
                            <TableCell>{t('common:actions')}</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {filteredReports.length === 0 ? (
                            <TableRow>
                                <TableCell colSpan={7} align="center">
                                    <Typography color="text.secondary">
                                        {t('chat:noReports')}
                                    </Typography>
                                </TableCell>
                            </TableRow>
                        ) : (
                            filteredReports.map((report) => (
                                <TableRow key={report.reportId}>
                                    <TableCell>{report.reportId}</TableCell>
                                    <TableCell>{report.reportedUserId}</TableCell>
                                    <TableCell>{report.reportedByUserId}</TableCell>
                                    <TableCell>
                                        <Typography variant="body2" noWrap sx={{ maxWidth: 200 }}>
                                            {report.reason}
                                        </Typography>
                                    </TableCell>
                                    <TableCell>
                                        <Chip
                                            label={report.status}
                                            color={getStatusColor(report.status)}
                                            size="small"
                                        />
                                    </TableCell>
                                    <TableCell>
                                        {format(parseISO(report.createdDate), 'MMM d, yyyy HH:mm')}
                                    </TableCell>
                                    <TableCell>
                                        <IconButton
                                            size="small"
                                            onClick={() => handleViewReport(report)}
                                            title={t('common:view')}
                                        >
                                            <ViewIcon />
                                        </IconButton>
                                        <IconButton
                                            size="small"
                                            onClick={() => handleDelete(report.reportId)}
                                            title={t('common:delete')}
                                            color="error"
                                        >
                                            <DeleteIcon />
                                        </IconButton>
                                    </TableCell>
                                </TableRow>
                            ))
                        )}
                    </TableBody>
                </Table>
            </TableContainer>

            {/* Review Dialog */}
            <Dialog open={reviewDialogOpen} onClose={() => setReviewDialogOpen(false)} maxWidth="md" fullWidth>
                <DialogTitle>{t('chat:reportDetails')}</DialogTitle>
                <DialogContent>
                    {selectedReport && (
                        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2, mt: 1 }}>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:reportId')}
                                </Typography>
                                <Typography variant="body1">{selectedReport.reportId}</Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:messageId')}
                                </Typography>
                                <Typography variant="body1">{selectedReport.messageId}</Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:reportedUser')}
                                </Typography>
                                <Typography variant="body1">{selectedReport.reportedUserId}</Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:reportedBy')}
                                </Typography>
                                <Typography variant="body1">{selectedReport.reportedByUserId}</Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:reason')}
                                </Typography>
                                <Typography variant="body1">{selectedReport.reason}</Typography>
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:status')}
                                </Typography>
                                <Chip
                                    label={selectedReport.status}
                                    color={getStatusColor(selectedReport.status)}
                                    size="small"
                                />
                            </Box>
                            <Box>
                                <Typography variant="subtitle2" color="text.secondary">
                                    {t('chat:createdDate')}
                                </Typography>
                                <Typography variant="body1">
                                    {format(parseISO(selectedReport.createdDate), 'MMM d, yyyy HH:mm')}
                                </Typography>
                            </Box>
                            {selectedReport.reviewedDate && (
                                <Box>
                                    <Typography variant="subtitle2" color="text.secondary">
                                        {t('chat:reviewedDate')}
                                    </Typography>
                                    <Typography variant="body1">
                                        {format(parseISO(selectedReport.reviewedDate), 'MMM d, yyyy HH:mm')}
                                    </Typography>
                                </Box>
                            )}
                            <TextField
                                fullWidth
                                multiline
                                rows={4}
                                label={t('chat:adminNotes')}
                                value={adminNotes}
                                onChange={(e) => setAdminNotes(e.target.value)}
                            />
                        </Box>
                    )}
                </DialogContent>
                <DialogActions>
                    <Button onClick={() => setReviewDialogOpen(false)}>{t('common:cancel')}</Button>
                    {selectedReport?.status === 'Pending' && (
                        <Button
                            onClick={handleReview}
                            variant="contained"
                            color="info"
                            disabled={reviewMutation.isPending}
                        >
                            {t('chat:markAsReviewed')}
                        </Button>
                    )}
                    {selectedReport?.status !== 'Resolved' && (
                        <Button
                            onClick={handleResolve}
                            variant="contained"
                            color="success"
                            disabled={resolveMutation.isPending}
                            startIcon={<ResolveIcon />}
                        >
                            {t('chat:resolve')}
                        </Button>
                    )}
                </DialogActions>
            </Dialog>
        </Box>
    );
};

export default ReportsPage;

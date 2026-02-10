import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Button, Chip, Stack } from '@mui/material';
import { ApartmentApplication } from '../shared/types/application';
import { applicationsApi } from '../shared/api/applicationsApi';
import { useNotifications } from '../shared/context/NotificationContext';

const LandlordApplicationsPage: React.FC = () => {
    const [applications, setApplications] = useState<ApartmentApplication[]>([]);
    const { addNotification } = useNotifications();

    useEffect(() => {
        loadApplications();
    }, []);

    const loadApplications = async () => {
        try {
            const data = await applicationsApi.getLandlordApplications();
            setApplications(data);
        } catch (error) {
            console.error('Failed to load applications', error);
        }
    };

    const handleStatusUpdate = async (id: number, status: string) => {
        try {
            await applicationsApi.updateStatus(id, { status });
            addNotification({
                title: 'Status Updated',
                message: `Application ${status}`,
                type: 'success'
            });
            loadApplications();
        } catch (error) {
            addNotification({
                title: 'Error',
                message: 'Failed to update status',
                type: 'error'
            });
        }
    };

    const getStatusColor = (status: string) => {
        switch (status.toLowerCase()) {
            case 'approved': return 'success';
            case 'rejected': return 'error';
            default: return 'warning';
        }
    };

    return (
        <Container maxWidth="lg" sx={{ mt: 4, mb: 4 }}>
            <Typography variant="h4" gutterBottom>Received Applications</Typography>
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Apartment</TableCell>
                            <TableCell>Applicant</TableCell>
                            <TableCell>Date</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>Actions</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {applications.map((app) => (
                            <TableRow key={app.applicationId}>
                                <TableCell>{app.apartment?.title}</TableCell>
                                <TableCell>
                                    {app.user?.firstName} {app.user?.lastName} <br />
                                    <Typography variant="caption" color="textSecondary">{app.user?.email}</Typography>
                                </TableCell>
                                <TableCell>{new Date(app.applicationDate).toLocaleDateString()}</TableCell>
                                <TableCell>
                                    <Chip label={app.status} color={getStatusColor(app.status)} size="small" />
                                </TableCell>
                                <TableCell>
                                    {app.status === 'Pending' && (
                                        <Stack direction="row" spacing={1}>
                                            <Button
                                                variant="contained"
                                                color="success"
                                                size="small"
                                                onClick={() => handleStatusUpdate(app.applicationId, 'Approved')}
                                            >
                                                Approve
                                            </Button>
                                            <Button
                                                variant="outlined"
                                                color="error"
                                                size="small"
                                                onClick={() => handleStatusUpdate(app.applicationId, 'Rejected')}
                                            >
                                                Reject
                                            </Button>
                                        </Stack>
                                    )}
                                </TableCell>
                            </TableRow>
                        ))}
                        {applications.length === 0 && (
                            <TableRow>
                                <TableCell colSpan={5} align="center">No applications received.</TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                </Table>
            </TableContainer>
        </Container>
    );
};

export default LandlordApplicationsPage;

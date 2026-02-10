import React, { useEffect, useState } from 'react';
import { Container, Typography, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Chip } from '@mui/material';
import { ApartmentApplication } from '../shared/types/application';
import { applicationsApi } from '../shared/api/applicationsApi';
import { useNavigate } from 'react-router-dom';

const TenantApplicationsPage: React.FC = () => {
    const [applications, setApplications] = useState<ApartmentApplication[]>([]);
    const navigate = useNavigate();

    useEffect(() => {
        loadApplications();
    }, []);

    const loadApplications = async () => {
        try {
            const data = await applicationsApi.getTenantApplications();
            setApplications(data);
        } catch (error) {
            console.error('Failed to load applications', error);
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
            <Typography variant="h4" gutterBottom>My Applications</Typography>
            <TableContainer component={Paper}>
                <Table>
                    <TableHead>
                        <TableRow>
                            <TableCell>Apartment</TableCell>
                            <TableCell>Location</TableCell>
                            <TableCell>Price</TableCell>
                            <TableCell>Date Applied</TableCell>
                            <TableCell>Status</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {applications.map((app) => (
                            <TableRow key={app.applicationId} hover sx={{ cursor: 'pointer' }} onClick={() => navigate(`/apartments/${app.apartmentId}`)}>
                                <TableCell>{app.apartment?.title}</TableCell>
                                <TableCell>{app.apartment?.city}</TableCell>
                                <TableCell>${app.apartment?.rent}</TableCell>
                                <TableCell>{new Date(app.applicationDate).toLocaleDateString()}</TableCell>
                                <TableCell>
                                    <Chip label={app.status} color={getStatusColor(app.status)} size="small" />
                                </TableCell>
                            </TableRow>
                        ))}
                        {applications.length === 0 && (
                            <TableRow>
                                <TableCell colSpan={5} align="center">No applications found.</TableCell>
                            </TableRow>
                        )}
                    </TableBody>
                </Table>
            </TableContainer>
        </Container>
    );
};

export default TenantApplicationsPage;

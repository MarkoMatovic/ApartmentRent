import React, { useState } from 'react';
import {
    Container,
    Paper,
    TextField,
    Button,
    Typography,
    Box,
    FormControl,
    InputLabel,
    Select,
    MenuItem,
    Grid,
    FormControlLabel,
    Checkbox,
    Alert,
    IconButton,
    Card,
    CardMedia,
    CardActions,
} from '@mui/material';
import { Delete as DeleteIcon, Add as AddIcon, DragIndicator as DragIndicatorIcon } from '@mui/icons-material';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { apartmentsApi } from '../shared/api/apartments';
import { ApartmentInputDto, ApartmentType } from '../shared/types/apartment';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDateFns } from '@mui/x-date-pickers/AdapterDateFns';
import {
    DndContext,
    closestCenter,
    KeyboardSensor,
    PointerSensor,
    useSensor,
    useSensors,
    DragEndEvent,
} from '@dnd-kit/core';
import {
    arrayMove,
    SortableContext,
    sortableKeyboardCoordinates,
    useSortable,
} from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';

interface SortableImageItemProps {
    url: string;
    index: number;
    onRemove: () => void;
    t: (key: string, options?: any) => string;
}

const SortableImageItem: React.FC<SortableImageItemProps> = ({ url, index, onRemove, t }) => {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({ id: `image-${index}` });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    };

    return (
        <Grid item xs={12} sm={6} md={4} ref={setNodeRef} style={style}>
            <Card sx={{ position: 'relative' }}>
                <Box
                    {...attributes}
                    {...listeners}
                    sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 2,
                        cursor: 'grab',
                        bgcolor: 'rgba(255, 255, 255, 0.9)',
                        borderRadius: 1,
                        p: 0.5,
                        '&:active': {
                            cursor: 'grabbing',
                        },
                        '&:hover': {
                            bgcolor: 'rgba(255, 255, 255, 1)',
                        },
                    }}
                >
                    <DragIndicatorIcon color="action" />
                </Box>
                <CardMedia
                    component="img"
                    height="200"
                    image={url}
                    alt={`Apartment image ${index + 1}`}
                    sx={{ objectFit: 'cover' }}
                    onError={(e: any) => {
                        e.target.src = 'https://via.placeholder.com/400x300?text=Image+Not+Found';
                    }}
                />
                <CardActions>
                    <IconButton
                        size="small"
                        color="error"
                        onClick={onRemove}
                    >
                        <DeleteIcon />
                    </IconButton>
                    <Typography variant="caption" sx={{ ml: 1 }}>
                        {t('apartments:image', { defaultValue: 'Image' })} {index + 1}
                    </Typography>
                </CardActions>
            </Card>
        </Grid>
    );
};

interface SortableFileItemProps {
    file: File;
    index: number;
    onRemove: () => void;
}

const SortableFileItem: React.FC<SortableFileItemProps> = ({ file, index, onRemove }) => {
    const {
        attributes,
        listeners,
        setNodeRef,
        transform,
        transition,
        isDragging,
    } = useSortable({ id: `file-${index}` });

    const style = {
        transform: CSS.Transform.toString(transform),
        transition,
        opacity: isDragging ? 0.5 : 1,
    };

    return (
        <Grid item xs={12} sm={6} md={4} ref={setNodeRef} style={style}>
            <Card variant="outlined" sx={{ position: 'relative' }}>
                <Box
                    {...attributes}
                    {...listeners}
                    sx={{
                        position: 'absolute',
                        top: 8,
                        left: 8,
                        zIndex: 2,
                        cursor: 'grab',
                        bgcolor: 'rgba(255, 255, 255, 0.9)',
                        borderRadius: 1,
                        p: 0.5,
                        '&:active': {
                            cursor: 'grabbing',
                        },
                        '&:hover': {
                            bgcolor: 'rgba(255, 255, 255, 1)',
                        },
                    }}
                >
                    <DragIndicatorIcon color="action" fontSize="small" />
                </Box>
                <CardMedia
                    component="img"
                    height="120"
                    image={URL.createObjectURL(file)}
                    alt={file.name}
                    sx={{ objectFit: 'cover' }}
                />
                <CardActions>
                    <IconButton
                        size="small"
                        color="error"
                        onClick={onRemove}
                    >
                        <DeleteIcon />
                    </IconButton>
                    <Typography variant="caption" noWrap sx={{ ml: 1, flex: 1 }}>
                        {file.name}
                    </Typography>
                </CardActions>
            </Card>
        </Grid>
    );
};

const CreateApartmentPage: React.FC = () => {
    const { t } = useTranslation(['common', 'apartments']);
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);

    const [formData, setFormData] = useState<ApartmentInputDto>({
        title: '',
        description: '',
        rent: 0,
        address: '',
        city: '',
        postalCode: '',
        availableFrom: undefined,
        availableUntil: undefined,
        numberOfRooms: undefined,
        rentIncludeUtilities: false,
        latitude: undefined,
        longitude: undefined,
        sizeSquareMeters: undefined,
        apartmentType: ApartmentType.Studio,
        isFurnished: false,
        hasBalcony: false,
        hasElevator: false,
        hasParking: false,
        hasInternet: false,
        hasAirCondition: false,
        isPetFriendly: false,
        isSmokingAllowed: false,
        depositAmount: undefined,
        minimumStayMonths: undefined,
        maximumStayMonths: undefined,
        isImmediatelyAvailable: false,
        isLookingForRoommate: false,
        imageUrls: [],
    });

    const [imageUrlInput, setImageUrlInput] = useState('');
    const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
    const [uploading, setUploading] = useState(false);

    const sensors = useSensors(
        useSensor(PointerSensor),
        useSensor(KeyboardSensor, {
            coordinateGetter: sortableKeyboardCoordinates,
        })
    );

    const createMutation = useMutation({
        mutationFn: (data: ApartmentInputDto) => apartmentsApi.create(data),
        onSuccess: () => {
            setSuccess(true);
            queryClient.invalidateQueries({ queryKey: ['apartments'] });
            setTimeout(() => {
                navigate('/my-apartments');
            }, 2000);
        },
        onError: (err: any) => {
            let errorMessage = 'Failed to create apartment listing';

            if (err.response?.data?.message) {
                errorMessage = err.response.data.message;
            } else if (err.response?.data) {
                errorMessage = typeof err.response.data === 'string'
                    ? err.response.data
                    : JSON.stringify(err.response.data);
            } else if (err.message) {
                errorMessage = err.message;
            }

            setError(errorMessage);
        },
    });

    const handleChange = (field: keyof ApartmentInputDto, value: any) => {
        setFormData({ ...formData, [field]: value });
    };

    const handleAddImageUrl = () => {
        if (imageUrlInput.trim()) {
            const newImageUrls = [...(formData.imageUrls || []), imageUrlInput.trim()];
            setFormData({ ...formData, imageUrls: newImageUrls });
            setImageUrlInput('');
        }
    };

    const handleRemoveImage = (index: number) => {
        const newImageUrls = formData.imageUrls?.filter((_, i) => i !== index) || [];
        setFormData({ ...formData, imageUrls: newImageUrls });
    };

    const handleDragEnd = (event: DragEndEvent) => {
        const { active, over } = event;

        if (over && active.id !== over.id) {
            const activeId = active.id.toString();
            const overId = over.id.toString();

            // Handle image URLs reordering
            if (activeId.startsWith('image-') && overId.startsWith('image-') && formData.imageUrls) {
                const oldIndex = parseInt(activeId.replace('image-', ''));
                const newIndex = parseInt(overId.replace('image-', ''));

                const newImageUrls = arrayMove(formData.imageUrls, oldIndex, newIndex);
                setFormData({ ...formData, imageUrls: newImageUrls });
            }
            // Handle selected files reordering
            else if (activeId.startsWith('file-') && overId.startsWith('file-')) {
                const oldIndex = parseInt(activeId.replace('file-', ''));
                const newIndex = parseInt(overId.replace('file-', ''));

                setSelectedFiles(prev => arrayMove(prev, oldIndex, newIndex));
            }
        }
    };

    const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
        if (event.target.files) {
            const filesArray = Array.from(event.target.files);
            setSelectedFiles(prev => [...prev, ...filesArray]);
        }
    };

    const handleRemoveSelectedFile = (index: number) => {
        setSelectedFiles(prev => prev.filter((_, i) => i !== index));
    };

    const handleUploadImages = async () => {
        if (selectedFiles.length === 0) return;

        setUploading(true);
        setError('');

        try {
            const uploadedUrls = await apartmentsApi.uploadImages(selectedFiles);
            const newImageUrls = [...(formData.imageUrls || []), ...uploadedUrls];
            setFormData({ ...formData, imageUrls: newImageUrls });
            setSelectedFiles([]);
        } catch (err: any) {
            let errorMessage = 'Failed to upload images';

            if (err.response?.data) {
                errorMessage = typeof err.response.data === 'string'
                    ? err.response.data
                    : err.response.data.message || JSON.stringify(err.response.data);
            } else if (err.message) {
                errorMessage = err.message;
            }

            setError(errorMessage);
        } finally {
            setUploading(false);
        }
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        // Basic validation
        if (!formData.title || !formData.address || !formData.city || formData.rent <= 0) {
            setError(t('apartments:fillRequiredFields', { defaultValue: 'Please fill all required fields' }));
            return;
        }

        // If there are selected files that haven't been uploaded yet, upload them first
        if (selectedFiles.length > 0) {
            setUploading(true);
            setError('');
            try {
                const uploadedUrls = await apartmentsApi.uploadImages(selectedFiles);
                const newImageUrls = [...(formData.imageUrls || []), ...uploadedUrls];
                setFormData({ ...formData, imageUrls: newImageUrls });
                setSelectedFiles([]);
                setUploading(false);
                
                // Now submit with updated imageUrls
                createMutation.mutate({ ...formData, imageUrls: newImageUrls });
                return;
            } catch (err: any) {
                let errorMessage = 'Failed to upload images';
                if (err.response?.data) {
                    errorMessage = typeof err.response.data === 'string'
                        ? err.response.data
                        : err.response.data.message || JSON.stringify(err.response.data);
                } else if (err.message) {
                    errorMessage = err.message;
                }
                setError(errorMessage);
                setUploading(false);
                return;
            }
        }

        createMutation.mutate(formData);
    };

    if (success) {
        return (
            <Container maxWidth="md" sx={{ py: 8 }}>
                <Paper sx={{ p: 4 }}>
                    <Alert severity="success" sx={{ mb: 2 }}>
                        {t('apartments:apartmentCreated', { defaultValue: 'Apartment listing created successfully!' })}
                    </Alert>
                </Paper>
            </Container>
        );
    }

    return (
        <LocalizationProvider dateAdapter={AdapterDateFns}>
            <Container maxWidth="md" sx={{ py: 4 }}>
                <Paper elevation={3} sx={{ p: 4 }}>
                    <Typography variant="h4" component="h1" gutterBottom align="center">
                        {t('apartments:createApartment', { defaultValue: 'Create Apartment Listing' })}
                    </Typography>

                    {error && (
                        <Alert severity="error" sx={{ mb: 2 }}>
                            {error}
                        </Alert>
                    )}

                    <Box component="form" onSubmit={handleSubmit} sx={{ mt: 3 }}>
                        <Grid container spacing={2}>
                            {/* Basic Information */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom>
                                    {t('apartments:basicInfo', { defaultValue: 'Basic Information' })}
                                </Typography>
                            </Grid>

                            <Grid item xs={12}>
                                <TextField
                                    fullWidth
                                    required
                                    label={t('apartments:title')}
                                    value={formData.title}
                                    onChange={(e) => handleChange('title', e.target.value)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:description')}
                                    multiline
                                    rows={4}
                                    value={formData.description}
                                    onChange={(e) => handleChange('description', e.target.value)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    required
                                    label={t('apartments:address')}
                                    value={formData.address}
                                    onChange={(e) => handleChange('address', e.target.value)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    required
                                    label={t('apartments:city', { defaultValue: 'City' })}
                                    value={formData.city}
                                    onChange={(e) => handleChange('city', e.target.value)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:postalCode', { defaultValue: 'Postal Code' })}
                                    value={formData.postalCode}
                                    onChange={(e) => handleChange('postalCode', e.target.value)}
                                    margin="normal"
                                />
                            </Grid>

                            {/* Image Upload Section */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                                    {t('apartments:images', { defaultValue: 'Images' })}
                                </Typography>
                            </Grid>

                            <Grid item xs={12}>
                                {/* File Upload */}
                                <Box sx={{ mb: 3 }}>
                                    <Button
                                        variant="outlined"
                                        component="label"
                                        startIcon={<AddIcon />}
                                        fullWidth
                                        sx={{ mb: 2, py: 2 }}
                                    >
                                        {t('apartments:selectFiles', { defaultValue: 'Select Images from Computer' })}
                                        <input
                                            type="file"
                                            hidden
                                            multiple
                                            accept="image/*"
                                            onChange={handleFileSelect}
                                        />
                                    </Button>

                                    {/* Selected Files Preview */}
                                    {selectedFiles.length > 0 && (
                                        <Box sx={{ mb: 2 }}>
                                            <Typography variant="subtitle2" gutterBottom>
                                                {t('apartments:selectedFiles', { defaultValue: 'Selected Files' })}: {selectedFiles.length}
                                                <Typography component="span" variant="caption" sx={{ ml: 1, color: 'text.secondary' }}>
                                                    ({t('apartments:dragToReorder', { defaultValue: 'Drag to reorder' })})
                                                </Typography>
                                            </Typography>
                                            <DndContext
                                                sensors={sensors}
                                                collisionDetection={closestCenter}
                                                onDragEnd={handleDragEnd}
                                            >
                                                <SortableContext
                                                    items={selectedFiles.map((_, index) => `file-${index}`)}
                                                >
                                            <Grid container spacing={1}>
                                                {selectedFiles.map((file, index) => (
                                                            <SortableFileItem
                                                                key={`file-${index}`}
                                                                file={file}
                                                                index={index}
                                                                onRemove={() => handleRemoveSelectedFile(index)}
                                                            />
                                                        ))}
                                                    </Grid>
                                                </SortableContext>
                                            </DndContext>
                                            <Button
                                                variant="contained"
                                                color="primary"
                                                onClick={handleUploadImages}
                                                disabled={uploading}
                                                fullWidth
                                                sx={{ mt: 2 }}
                                            >
                                                {uploading ? t('apartments:uploading', { defaultValue: 'Uploading...' }) : t('apartments:uploadImages', { defaultValue: 'Upload Images' })}
                                            </Button>
                                        </Box>
                                    )}
                                </Box>

                                {/* Manual URL Input */}
                                <Typography variant="subtitle2" gutterBottom>
                                    {t('apartments:orEnterUrl', { defaultValue: 'Or enter image URL manually' })}
                                </Typography>
                                <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
                                    <TextField
                                        fullWidth
                                        label={t('apartments:imageUrl', { defaultValue: 'Image URL' })}
                                        value={imageUrlInput}
                                        onChange={(e) => setImageUrlInput(e.target.value)}
                                        placeholder="https://example.com/image.jpg"
                                        onKeyPress={(e) => {
                                            if (e.key === 'Enter') {
                                                e.preventDefault();
                                                handleAddImageUrl();
                                            }
                                        }}
                                    />
                                    <Button
                                        variant="contained"
                                        onClick={handleAddImageUrl}
                                        startIcon={<AddIcon />}
                                        sx={{ minWidth: '120px' }}
                                    >
                                        {t('apartments:addImage', { defaultValue: 'Add' })}
                                    </Button>
                                </Box>

                                {/* Uploaded Images */}
                                {formData.imageUrls && formData.imageUrls.length > 0 && (
                                    <Box>
                                        <Typography variant="subtitle2" gutterBottom>
                                            {t('apartments:uploadedImages', { defaultValue: 'Uploaded Images' })}: {formData.imageUrls.length}
                                            <Typography component="span" variant="caption" sx={{ ml: 1, color: 'text.secondary' }}>
                                                ({t('apartments:dragToReorder', { defaultValue: 'Drag to reorder' })})
                                            </Typography>
                                        </Typography>
                                        <DndContext
                                            sensors={sensors}
                                            collisionDetection={closestCenter}
                                            onDragEnd={handleDragEnd}
                                        >
                                            <SortableContext
                                                items={formData.imageUrls.map((_, index) => `image-${index}`)}
                                            >
                                        <Grid container spacing={2}>
                                            {formData.imageUrls.map((url, index) => (
                                                        <SortableImageItem
                                                            key={`image-${index}`}
                                                            url={url}
                                                            index={index}
                                                            onRemove={() => handleRemoveImage(index)}
                                                            t={t}
                                                        />
                                                    ))}
                                                </Grid>
                                            </SortableContext>
                                        </DndContext>
                                    </Box>
                                )}
                            </Grid>

                            {/* Price Information */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                                    {t('apartments:priceInfo', { defaultValue: 'Price Information' })}
                                </Typography>
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    required
                                    label={`${t('apartments:rent')} (€)`}
                                    type="number"
                                    value={formData.rent || ''}
                                    onChange={(e) => handleChange('rent', parseFloat(e.target.value))}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={`${t('apartments:deposit')} (€)`}
                                    type="number"
                                    value={formData.depositAmount || ''}
                                    onChange={(e) => handleChange('depositAmount', e.target.value ? parseFloat(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.rentIncludeUtilities || false}
                                            onChange={(e) => handleChange('rentIncludeUtilities', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:utilitiesIncluded')}
                                />
                            </Grid>

                            {/* Availability */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                                    {t('apartments:availability', { defaultValue: 'Availability' })}
                                </Typography>
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <DatePicker
                                    label={t('apartments:availableFrom')}
                                    value={formData.availableFrom ? new Date(formData.availableFrom) : null}
                                    onChange={(newValue) => handleChange('availableFrom', newValue?.toISOString().split('T')[0])}
                                    slotProps={{
                                        textField: {
                                            fullWidth: true,
                                            margin: 'normal',
                                        },
                                    }}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <DatePicker
                                    label={t('apartments:availableUntil')}
                                    value={formData.availableUntil ? new Date(formData.availableUntil) : null}
                                    onChange={(newValue) => handleChange('availableUntil', newValue?.toISOString().split('T')[0])}
                                    slotProps={{
                                        textField: {
                                            fullWidth: true,
                                            margin: 'normal',
                                        },
                                    }}
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.isImmediatelyAvailable || false}
                                            onChange={(e) => handleChange('isImmediatelyAvailable', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:immediatelyAvailable')}
                                />
                            </Grid>

                            <Grid item xs={12}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.isLookingForRoommate || false}
                                            onChange={(e) => handleChange('isLookingForRoommate', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:lookingForRoommate', { defaultValue: 'Looking for Roommate' })}
                                />
                            </Grid>

                            {/* Apartment Details */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                                    {t('apartments:details')}
                                </Typography>
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <FormControl fullWidth margin="normal">
                                    <InputLabel>{t('apartments:type')}</InputLabel>
                                    <Select
                                        value={formData.apartmentType}
                                        onChange={(e) => handleChange('apartmentType', e.target.value as ApartmentType)}
                                        label={t('apartments:type')}
                                    >
                                        <MenuItem value={ApartmentType.Studio}>{t('apartments:studio', { defaultValue: 'Studio' })}</MenuItem>
                                        <MenuItem value={ApartmentType.OneRoom}>{t('apartments:oneRoom', { defaultValue: '1 Room' })}</MenuItem>
                                        <MenuItem value={ApartmentType.TwoRoom}>{t('apartments:twoRoom', { defaultValue: '2 Rooms' })}</MenuItem>
                                        <MenuItem value={ApartmentType.ThreeRoom}>{t('apartments:threeRoom', { defaultValue: '3 Rooms' })}</MenuItem>
                                        <MenuItem value={ApartmentType.FourRoom}>{t('apartments:fourRoom', { defaultValue: '4+ Rooms' })}</MenuItem>
                                        <MenuItem value={ApartmentType.House}>{t('apartments:house', { defaultValue: 'House' })}</MenuItem>
                                    </Select>
                                </FormControl>
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:rooms')}
                                    type="number"
                                    value={formData.numberOfRooms || ''}
                                    onChange={(e) => handleChange('numberOfRooms', e.target.value ? parseInt(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:sizeSquareMeters', { defaultValue: 'Size (m²)' })}
                                    type="number"
                                    value={formData.sizeSquareMeters || ''}
                                    onChange={(e) => handleChange('sizeSquareMeters', e.target.value ? parseInt(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:minimumStay', { defaultValue: 'Minimum Stay (months)' })}
                                    type="number"
                                    value={formData.minimumStayMonths || ''}
                                    onChange={(e) => handleChange('minimumStayMonths', e.target.value ? parseInt(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>

                            <Grid item xs={12} sm={6}>
                                <TextField
                                    fullWidth
                                    label={t('apartments:maximumStay', { defaultValue: 'Maximum Stay (months)' })}
                                    type="number"
                                    value={formData.maximumStayMonths || ''}
                                    onChange={(e) => handleChange('maximumStayMonths', e.target.value ? parseInt(e.target.value) : undefined)}
                                    margin="normal"
                                />
                            </Grid>

                            {/* Features */}
                            <Grid item xs={12}>
                                <Typography variant="h6" gutterBottom sx={{ mt: 2 }}>
                                    {t('apartments:features')}
                                </Typography>
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.isFurnished || false}
                                            onChange={(e) => handleChange('isFurnished', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:furnished')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.hasBalcony || false}
                                            onChange={(e) => handleChange('hasBalcony', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:balcony')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.hasElevator || false}
                                            onChange={(e) => handleChange('hasElevator', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:elevator')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.hasParking || false}
                                            onChange={(e) => handleChange('hasParking', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:parking')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.hasInternet || false}
                                            onChange={(e) => handleChange('hasInternet', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:internet')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.hasAirCondition || false}
                                            onChange={(e) => handleChange('hasAirCondition', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:airCondition')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.isPetFriendly || false}
                                            onChange={(e) => handleChange('isPetFriendly', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:petFriendly')}
                                />
                            </Grid>

                            <Grid item xs={12} sm={6} md={4}>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.isSmokingAllowed || false}
                                            onChange={(e) => handleChange('isSmokingAllowed', e.target.checked)}
                                        />
                                    }
                                    label={t('apartments:smokingAllowed')}
                                />
                            </Grid>
                        </Grid>

                        <Box sx={{ mt: 3, display: 'flex', gap: 2 }}>
                            <Button
                                type="submit"
                                variant="contained"
                                color="secondary"
                                size="large"
                                disabled={createMutation.isPending}
                            >
                                {createMutation.isPending ? t('loading') : t('apartments:createApartment', { defaultValue: 'Create Listing' })}
                            </Button>
                            <Button
                                variant="outlined"
                                size="large"
                                onClick={() => navigate('/my-apartments')}
                            >
                                {t('common:cancel')}
                            </Button>
                        </Box>
                    </Box>
                </Paper>
            </Container>
        </LocalizationProvider>
    );
};

export default CreateApartmentPage;

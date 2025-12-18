import { useState } from 'react';
import {
  Box,
  TextField,
  Select,
  MenuItem,
  FormControl,
  InputLabel,
  Chip,
  Popover,
  Typography,
  Button,
  Stack,
  IconButton,
  Paper,
  Divider,
} from '@mui/material';
import {
  LocationOn as LocationIcon,
  CalendarToday as CalendarIcon,
  AttachMoney as MoneyIcon,
  FilterList as FilterIcon,
  Close as CloseIcon,
} from '@mui/icons-material';

export interface ApartmentFilters {
  city: string;
  moveInDate: string;
  moveOutDate: string;
  minPrice: number | '';
  maxPrice: number | '';
  apartmentType: 'all' | 'studio' | 'apartment' | 'room';
  bedrooms: number | 'all';
  minSize: number | '';
  furnished: boolean | 'all';
}

interface ApartmentsFiltersProps {
  filters: ApartmentFilters;
  onFiltersChange: (filters: ApartmentFilters) => void;
}

export const ApartmentsFilters = ({ filters, onFiltersChange }: ApartmentsFiltersProps) => {
  const [moreFiltersAnchor, setMoreFiltersAnchor] = useState<HTMLButtonElement | null>(null);

  const handleFilterChange = (key: keyof ApartmentFilters, value: any) => {
    onFiltersChange({
      ...filters,
      [key]: value,
    });
  };

  const handleClearFilter = (key: keyof ApartmentFilters) => {
    const defaultValue: any = {
      city: '',
      moveInDate: '',
      moveOutDate: '',
      minPrice: '',
      maxPrice: '',
      apartmentType: 'all',
      bedrooms: 'all',
      minSize: '',
      furnished: 'all',
    };
    handleFilterChange(key, defaultValue[key]);
  };

  const activeFiltersCount = [
    filters.city,
    filters.moveInDate,
    filters.moveOutDate,
    filters.minPrice,
    filters.maxPrice,
    filters.apartmentType !== 'all',
    filters.bedrooms !== 'all',
    filters.minSize,
    filters.furnished !== 'all',
  ].filter(Boolean).length;

  const openMoreFilters = Boolean(moreFiltersAnchor);

  return (
    <Box
      sx={{
        position: 'sticky',
        top: { xs: 64, sm: 72 },
        zIndex: 1000,
        bgcolor: 'background.paper',
        borderBottom: 1,
        borderColor: 'divider',
        py: 2,
      }}
    >
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          gap: 1.5,
          alignItems: 'center',
          px: { xs: 2, sm: 3 },
          maxWidth: '1400px',
          mx: 'auto',
        }}
      >
        {/* City/Location Filter */}
        <TextField
          placeholder="City or location"
          size="small"
          value={filters.city}
          onChange={(e) => handleFilterChange('city', e.target.value)}
          InputProps={{
            startAdornment: <LocationIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />,
            endAdornment: filters.city && (
              <IconButton
                size="small"
                onClick={() => handleClearFilter('city')}
                sx={{ p: 0.5 }}
              >
                <CloseIcon fontSize="small" />
              </IconButton>
            ),
          }}
          sx={{
            minWidth: { xs: '100%', sm: 200 },
            '& .MuiOutlinedInput-root': {
              borderRadius: 2,
            },
          }}
        />

        {/* Move-in Date */}
        <TextField
          type="date"
          label="Move-in"
          size="small"
          value={filters.moveInDate}
          onChange={(e) => handleFilterChange('moveInDate', e.target.value)}
          InputLabelProps={{ shrink: true }}
          InputProps={{
            startAdornment: <CalendarIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />,
          }}
          sx={{
            minWidth: { xs: '100%', sm: 150 },
            '& .MuiOutlinedInput-root': {
              borderRadius: 2,
            },
          }}
        />

        {/* Move-out Date */}
        <TextField
          type="date"
          label="Move-out"
          size="small"
          value={filters.moveOutDate}
          onChange={(e) => handleFilterChange('moveOutDate', e.target.value)}
          InputLabelProps={{ shrink: true }}
          InputProps={{
            startAdornment: <CalendarIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />,
          }}
          sx={{
            minWidth: { xs: '100%', sm: 150 },
            '& .MuiOutlinedInput-root': {
              borderRadius: 2,
            },
          }}
        />

        {/* Price Range */}
        <Box sx={{ display: 'flex', gap: 1, alignItems: 'center', minWidth: { xs: '100%', sm: 200 } }}>
          <TextField
            type="number"
            placeholder="Min price"
            size="small"
            value={filters.minPrice}
            onChange={(e) => handleFilterChange('minPrice', e.target.value ? Number(e.target.value) : '')}
            InputProps={{
              startAdornment: <MoneyIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />,
            }}
            sx={{
              flex: 1,
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
          />
          <Typography variant="body2" color="text.secondary">
            -
          </Typography>
          <TextField
            type="number"
            placeholder="Max price"
            size="small"
            value={filters.maxPrice}
            onChange={(e) => handleFilterChange('maxPrice', e.target.value ? Number(e.target.value) : '')}
            InputProps={{
              startAdornment: <MoneyIcon sx={{ color: 'text.secondary', mr: 1, fontSize: 20 }} />,
            }}
            sx={{
              flex: 1,
              '& .MuiOutlinedInput-root': {
                borderRadius: 2,
              },
            }}
          />
        </Box>

        {/* Apartment Type */}
        <FormControl size="small" sx={{ minWidth: { xs: '100%', sm: 150 } }}>
          <InputLabel>Type</InputLabel>
          <Select
            value={filters.apartmentType}
            label="Type"
            onChange={(e) => handleFilterChange('apartmentType', e.target.value)}
            sx={{
              borderRadius: 2,
            }}
          >
            <MenuItem value="all">All</MenuItem>
            <MenuItem value="studio">Studio</MenuItem>
            <MenuItem value="apartment">Apartment</MenuItem>
            <MenuItem value="room">Room</MenuItem>
          </Select>
        </FormControl>

        {/* More Filters Button */}
        <Button
          variant="outlined"
          startIcon={<FilterIcon />}
          onClick={(e) => setMoreFiltersAnchor(e.currentTarget)}
          sx={{
            borderRadius: 2,
            textTransform: 'none',
            borderColor: 'divider',
            minWidth: { xs: '100%', sm: 'auto' },
          }}
        >
          More filters
          {activeFiltersCount > 0 && (
            <Chip
              label={activeFiltersCount}
              size="small"
              sx={{
                ml: 1,
                height: 20,
                minWidth: 20,
                fontSize: '0.75rem',
              }}
            />
          )}
        </Button>

        {/* More Filters Popover */}
        <Popover
          open={openMoreFilters}
          anchorEl={moreFiltersAnchor}
          onClose={() => setMoreFiltersAnchor(null)}
          anchorOrigin={{
            vertical: 'bottom',
            horizontal: 'left',
          }}
          transformOrigin={{
            vertical: 'top',
            horizontal: 'left',
          }}
        >
          <Paper sx={{ p: 3, minWidth: 300 }}>
            <Typography variant="h6" gutterBottom>
              More Filters
            </Typography>
            <Divider sx={{ my: 2 }} />

            {/* Bedrooms */}
            <FormControl fullWidth sx={{ mb: 2 }}>
              <InputLabel>Bedrooms</InputLabel>
              <Select
                value={filters.bedrooms}
                label="Bedrooms"
                onChange={(e) => handleFilterChange('bedrooms', e.target.value === 'all' ? 'all' : Number(e.target.value))}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value={1}>1</MenuItem>
                <MenuItem value={2}>2</MenuItem>
                <MenuItem value={3}>3</MenuItem>
                <MenuItem value={4}>4+</MenuItem>
              </Select>
            </FormControl>

            {/* Min Size */}
            <TextField
              fullWidth
              type="number"
              label="Min size (m²)"
              size="small"
              value={filters.minSize}
              onChange={(e) => handleFilterChange('minSize', e.target.value ? Number(e.target.value) : '')}
              sx={{ mb: 2 }}
            />

            {/* Furnished */}
            <FormControl fullWidth>
              <InputLabel>Furnished</InputLabel>
              <Select
                value={filters.furnished}
                label="Furnished"
                onChange={(e) => handleFilterChange('furnished', e.target.value === 'all' ? 'all' : e.target.value === 'true')}
              >
                <MenuItem value="all">All</MenuItem>
                <MenuItem value="true">Furnished</MenuItem>
                <MenuItem value="false">Unfurnished</MenuItem>
              </Select>
            </FormControl>
          </Paper>
        </Popover>
      </Box>
    </Box>
  );
};


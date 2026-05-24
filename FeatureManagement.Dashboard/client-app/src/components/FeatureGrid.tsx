import React, { useMemo, useState } from 'react';
import {
    Box,
    Switch,
    Button,
    Chip,
    Card,
    CardHeader,
    CardContent,
    Typography,
    Stack,
    Divider,
    Skeleton,
    ToggleButton,
    ToggleButtonGroup,
    TextField,
    InputAdornment
} from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import { FeatureFlag } from '../types/featureFlags';

interface FeatureGridProps {
    flags: FeatureFlag[];
    isLoading: boolean;
    onToggle: (flag: FeatureFlag) => void;
    onEdit: (flag: FeatureFlag) => void;
}

export default function FeatureGrid({ flags, isLoading, onToggle, onEdit }: FeatureGridProps) {
    const [search, setSearch] = useState('');
    const [status, setStatus] = useState<'all' | 'enabled' | 'disabled'>('all');

    const loadingCards = Array.from({ length: 6 }, (_, i) => i);
    const filteredFlags = useMemo(() => {
        const searchValue = search.trim().toLowerCase();
        return flags.filter((flag) => {
            const isEnabled = flag.enabledFor.length > 0;
            const matchesStatus =
                status === 'all' ||
                (status === 'enabled' && isEnabled) ||
                (status === 'disabled' && !isEnabled);
            const matchesSearch = !searchValue || flag.name.toLowerCase().includes(searchValue);
            return matchesStatus && matchesSearch;
        });
    }, [flags, search, status]);

    return (
        <Card variant="outlined" sx={{ bgcolor: 'background.paper' }}>
            <CardHeader
                title="Feature Flags"
                subheader={<Typography variant="body2" color="text.secondary">Manage rollout logic and activation state by feature.</Typography>}
            />
            <CardContent sx={{ pt: 0 }}>
                <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5} sx={{ mb: 2 }}>
                    <TextField
                        size="small"
                        fullWidth
                        label="Search by feature name"
                        value={search}
                        onChange={(e) => setSearch(e.target.value)}
                        slotProps={{
                            input: {
                                startAdornment: (
                                    <InputAdornment position="start">
                                        <SearchIcon fontSize="small" />
                                    </InputAdornment>
                                )
                            }
                        }}
                    />
                    <ToggleButtonGroup
                        exclusive
                        value={status}
                        onChange={(_, next) => { if (next) setStatus(next); }}
                        size="small"
                    >
                        <ToggleButton value="all">All</ToggleButton>
                        <ToggleButton value="enabled">Enabled</ToggleButton>
                        <ToggleButton value="disabled">Disabled</ToggleButton>
                    </ToggleButtonGroup>
                </Stack>

                <Box
                    sx={{
                        display: 'grid',
                        gridTemplateColumns: {
                            xs: '1fr',
                            sm: 'repeat(2, minmax(0, 1fr))',
                            xl: 'repeat(3, minmax(0, 1fr))'
                        },
                        gap: 2
                    }}
                >
                    {isLoading && loadingCards.map((idx) => (
                        <Card key={idx} variant="outlined" sx={{ borderStyle: 'dashed' }}>
                            <CardContent>
                                <Skeleton variant="text" width="70%" height={34} />
                                <Skeleton variant="text" width="40%" />
                                <Divider sx={{ my: 1.5 }} />
                                <Stack direction="row" spacing={1}>
                                    <Skeleton variant="rounded" width={72} height={24} />
                                    <Skeleton variant="rounded" width={82} height={24} />
                                </Stack>
                            </CardContent>
                        </Card>
                    ))}

                    {!isLoading && filteredFlags.map((flag) => {
                        const isEnabled = flag.enabledFor.length > 0;
                        return (
                            <Card key={flag.name} variant="outlined" sx={{ bgcolor: 'background.paper' }}>
                                <CardContent>
                                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                                        <Box sx={{ minWidth: 0 }}>
                                            <Typography variant="h6" sx={{ fontWeight: 700 }} noWrap title={flag.name}>
                                                {flag.name}
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                {isEnabled ? 'Enabled' : 'Disabled'}
                                            </Typography>
                                        </Box>
                                        <Switch
                                            color="success"
                                            checked={isEnabled}
                                            onChange={() => onToggle(flag)}
                                        />
                                    </Stack>

                                    <Divider sx={{ mb: 1.5 }} />

                                    <Stack direction="row" spacing={1} sx={{ mb: 1.5, flexWrap: 'wrap' }}>
                                        <Chip
                                            size="small"
                                            label={flag.requirementType === 0 ? 'Logic: ANY' : 'Logic: ALL'}
                                            variant="outlined"
                                        />
                                        <Chip
                                            size="small"
                                            label={`Rules: ${flag.enabledFor.length}`}
                                            variant="outlined"
                                            color={flag.enabledFor.length > 0 ? 'primary' : 'default'}
                                        />
                                    </Stack>

                                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                                        <Button variant="outlined" size="small" onClick={() => onEdit(flag)}>
                                            Configure
                                        </Button>
                                    </Stack>
                                </CardContent>
                            </Card>
                        );
                    })}
                </Box>

                {!isLoading && filteredFlags.length === 0 && (
                    <Box sx={{ py: 8, textAlign: 'center' }}>
                        <Typography variant="h6">No matching feature flags</Typography>
                        <Typography variant="body2" color="text.secondary">
                            Try changing filters or create a new feature flag.
                        </Typography>
                    </Box>
                )}
            </CardContent>
        </Card>
    );
}
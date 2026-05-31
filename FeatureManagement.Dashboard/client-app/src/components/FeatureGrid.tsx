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
    readonly flags: FeatureFlag[];
    readonly isLoading: boolean;
    readonly onToggle: (flag: FeatureFlag) => void;
    readonly onEdit: (flag: FeatureFlag) => void;
    readonly onHistory: (flag: FeatureFlag) => void;
    readonly onDelete: (flag: FeatureFlag) => void;
    readonly onActivity: (flag: FeatureFlag) => void;
    readonly onExperiment?: (flag: FeatureFlag) => void;
}

export default function FeatureGrid({ flags, isLoading, onToggle, onEdit, onHistory, onDelete, onActivity, onExperiment }: FeatureGridProps) {
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
            const tagsSearch = flag.tags.join(' ').toLowerCase();
            const ownerSearch = flag.owner.toLowerCase();
            const matchesSearch =
                !searchValue ||
                flag.name.toLowerCase().includes(searchValue) ||
                ownerSearch.includes(searchValue) ||
                tagsSearch.includes(searchValue);
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
                            <Card
                                key={flag.name}
                                variant="outlined"
                                onClick={() => onEdit(flag)}
                                sx={{ bgcolor: 'background.paper', cursor: 'pointer' }}
                            >
                                <CardContent>
                                    <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'flex-start', mb: 1 }}>
                                        <Box sx={{ minWidth: 0 }}>
                                            <Typography
                                                variant="h6"
                                                sx={{ fontWeight: 700 }}
                                                noWrap
                                                title={flag.name}
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    onEdit(flag);
                                                }}
                                            >
                                                {flag.name}
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                {isEnabled ? 'Enabled' : 'Disabled'}
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                Owner: {flag.owner || 'Unassigned'}
                                            </Typography>
                                        </Box>
                                        <Switch
                                            color="success"
                                            checked={isEnabled}
                                            onClick={(event) => event.stopPropagation()}
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
                                        {flag.tags.map((tag) => (
                                            <Chip
                                                key={`${flag.name}-${tag}`}
                                                size="small"
                                                label={`Tag: ${tag}`}
                                                variant="outlined"
                                            />
                                        ))}
                                    </Stack>

                                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                                        <Button
                                            variant="outlined"
                                            size="small"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                onEdit(flag);
                                            }}
                                        >
                                            Configure
                                        </Button>
                                        <Button
                                            variant="text"
                                            size="small"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                onHistory(flag);
                                            }}
                                        >
                                            History
                                        </Button>
                                        <Button
                                            variant="text"
                                            size="small"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                onActivity(flag);
                                            }}
                                        >
                                            Activity
                                        </Button>
                                        {onExperiment && (
                                            <Button
                                                variant="text"
                                                size="small"
                                                onClick={(event) => {
                                                    event.stopPropagation();
                                                    onExperiment(flag);
                                                }}
                                            >
                                                Experiment
                                            </Button>
                                        )}
                                        <Button
                                            variant="text"
                                            color="error"
                                            size="small"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                onDelete(flag);
                                            }}
                                        >
                                            Delete
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
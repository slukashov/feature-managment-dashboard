import React from 'react';
import {
    Box,
    Button,
    CircularProgress,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    List,
    ListItem,
    Stack,
    Typography,
    Chip
} from '@mui/material';
import { FeatureFlagActivityEntry } from '../types/featureFlags';

interface ActivityFeedDialogProps {
    readonly open: boolean;
    readonly featureName: string;
    readonly activityEntries: FeatureFlagActivityEntry[];
    readonly isLoading: boolean;
    readonly onClose: () => void;
}

const activityTypeColors: Record<string, 'default' | 'primary' | 'success' | 'warning' | 'error'> = {
    'Created': 'success',
    'Updated': 'primary',
    'Deleted': 'error',
    'Scheduled': 'warning',
    'RolledBack': 'warning'
};

const formatDate = (value: string): string => {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
};

export default function ActivityFeedDialog({
    open,
    featureName,
    activityEntries,
    isLoading,
    onClose
}: ActivityFeedDialogProps) {
    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>{`Activity Feed: ${featureName}`}</DialogTitle>
            <DialogContent dividers>
                {isLoading && (
                    <Box sx={{ py: 5, display: 'flex', justifyContent: 'center' }}>
                        <CircularProgress size={28} />
                    </Box>
                )}

                {!isLoading && activityEntries.length === 0 && (
                    <Typography color="text.secondary">No activity recorded for this feature flag.</Typography>
                )}

                {!isLoading && activityEntries.length > 0 && (
                    <List disablePadding>
                        {activityEntries.map((entry) => (
                            <ListItem key={entry.id} divider sx={{ py: 2, flexDirection: 'column', alignItems: 'flex-start' }}>
                                <Stack direction="row" spacing={1} sx={{ mb: 1, width: '100%', alignItems: 'center' }}>
                                    <Chip
                                        label={entry.activityType}
                                        color={activityTypeColors[entry.activityType] || 'default'}
                                        size="small"
                                        variant="outlined"
                                    />
                                    {entry.changeType && (
                                        <Chip
                                            label={entry.changeType}
                                            size="small"
                                            variant="outlined"
                                        />
                                    )}
                                    <Typography variant="caption" color="text.secondary" sx={{ ml: 'auto' }}>
                                        {formatDate(entry.changedAtUtc)}
                                    </Typography>
                                </Stack>
                                <Typography variant="body2" sx={{ mb: 0.5 }}>
                                    {entry.description}
                                </Typography>
                                <Typography variant="caption" color="text.secondary">
                                    By: {entry.changedBy}
                                </Typography>
                            </ListItem>
                        ))}
                    </List>
                )}
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}


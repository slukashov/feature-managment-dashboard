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
    ListItemText,
    Stack,
    Typography
} from '@mui/material';
import { FeatureFlagAuditLog } from '../types/featureFlags';

interface HistoryDialogProps {
    readonly open: boolean;
    readonly featureName: string;
    readonly auditEntries: FeatureFlagAuditLog[];
    readonly isLoading: boolean;
    readonly isRollingBack: boolean;
    readonly onClose: () => void;
    readonly onRollback: (targetVersion: number) => Promise<void>;
}

const actionLabels: Record<number, string> = {
    0: 'Created',
    1: 'Updated',
    2: 'Deleted',
    3: 'Rolled back'
};

const formatDate = (value: string): string => {
    const parsed = new Date(value);
    if (Number.isNaN(parsed.getTime())) {
        return value;
    }

    return parsed.toLocaleString();
};

export default function HistoryDialog({
    open,
    featureName,
    auditEntries,
    isLoading,
    isRollingBack,
    onClose,
    onRollback
}: HistoryDialogProps) {
    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>{`History: ${featureName}`}</DialogTitle>
            <DialogContent dividers>
                {isLoading && (
                    <Box sx={{ py: 5, display: 'flex', justifyContent: 'center' }}>
                        <CircularProgress size={28} />
                    </Box>
                )}

                {!isLoading && auditEntries.length === 0 && (
                    <Typography color="text.secondary">No audit records found for this feature flag.</Typography>
                )}

                {!isLoading && auditEntries.length > 0 && (
                    <List disablePadding>
                        {auditEntries.map((entry) => (
                            <ListItem key={entry.id} divider sx={{ py: 1.25 }}>
                                <ListItemText
                                    primary={
                                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} sx={{ alignItems: { sm: 'center' } }}>
                                            <Typography variant="body2" sx={{ fontWeight: 700 }}>
                                                Version {entry.snapshotVersion}
                                            </Typography>
                                            <Typography variant="body2" color="text.secondary">
                                                {actionLabels[entry.action] ?? 'Changed'}
                                            </Typography>
                                            <Typography variant="caption" color="text.secondary">
                                                {formatDate(entry.changedAtUtc)}
                                            </Typography>
                                        </Stack>
                                    }
                                    secondary={
                                        <Typography variant="caption" color="text.secondary">
                                            Changed by: {entry.changedBy || 'system'}
                                        </Typography>
                                    }
                                />
                                <Button
                                    variant="outlined"
                                    size="small"
                                    onClick={() => onRollback(entry.snapshotVersion)}
                                    disabled={isRollingBack}
                                >
                                    Rollback
                                </Button>
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


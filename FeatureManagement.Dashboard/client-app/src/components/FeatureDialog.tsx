import React, { useState, useEffect } from 'react';
import { Dialog, DialogTitle, DialogContent, DialogActions, TextField, MenuItem, Switch, Typography, Button, Box, Stack } from '@mui/material';
import RuleEditor from './RuleEditor';
import AddIcon from '@mui/icons-material/Add';
import { FeatureFlag, FeatureFilter } from '../types/featureFlags';

interface FeatureDialogProps {
    open: boolean;
    onClose: () => void;
    onSave: (flag: FeatureFlag, isNew: boolean) => void;
    initialData: FeatureFlag;
    isNew: boolean;
}

export default function FeatureDialog({ open, onClose, onSave, initialData, isNew }: FeatureDialogProps) {
    const [flag, setFlag] = useState<FeatureFlag>({ name: '', requirementType: 0, enabledFor: [] });

    useEffect(() => {
        if (open) setFlag(initialData);
    }, [open, initialData]);

    const handleSave = () => onSave(flag, isNew);

    const isMasterOn = flag.enabledFor.length > 0;

    const toggleMaster = (checked: boolean) => {
        setFlag({
            ...flag,
            enabledFor: checked ? [{ name: 'AlwaysOn', parametersJson: '{}' }] : []
        });
    };

    const updateRule = (index: number, updatedFilter: FeatureFilter) => {
        const newFilters = [...flag.enabledFor];
        newFilters[index] = updatedFilter;
        setFlag({ ...flag, enabledFor: newFilters });
    };

    const removeRule = (index: number) => {
        const newFilters = [...flag.enabledFor];
        newFilters.splice(index, 1);
        setFlag({ ...flag, enabledFor: newFilters });
    };

    const addRule = () => {
        setFlag({
            ...flag,
            enabledFor: [...flag.enabledFor, { name: 'Microsoft.Percentage', parametersJson: '{"Value":50}' }]
        });
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>{isNew ? 'Create New Feature' : 'Edit Feature Configuration'}</DialogTitle>
            <DialogContent dividers>
                <TextField
                    autoFocus margin="dense" label="Feature Name (Key)" fullWidth variant="outlined"
                    disabled={!isNew}
                    value={flag.name}
                    onChange={(e) => setFlag({ ...flag, name: e.target.value })}
                />

                <Box
                    sx={{
                        mt: 3,
                        p: 2,
                        borderRadius: 2,
                        border: '1px solid',
                        borderColor: 'divider',
                        bgcolor: 'action.hover'
                    }}
                >
                    <Stack direction="row" spacing={2} sx={{ justifyContent: 'space-between', alignItems: 'center' }}>
                        <Box>
                        <Typography variant="h6">Master Switch</Typography>
                            <Typography variant="body2" color="text.secondary">Enable or disable this feature entirely.</Typography>
                        </Box>
                        <Switch size="medium" color="success" checked={isMasterOn} onChange={(e) => toggleMaster(e.target.checked)} />
                    </Stack>
                </Box>

                {isMasterOn && (
                    <Box sx={{ mt: 4 }}>
                        <Typography variant="h6" gutterBottom>Advanced Targeting Rules</Typography>
                        <TextField
                            select margin="dense" label="Rule Requirement" fullWidth size="small"
                            value={flag.requirementType}
                            onChange={(e) => setFlag({ ...flag, requirementType: Number(e.target.value) })}
                            sx={{ mb: 3 }}
                        >
                            <MenuItem value={0}>Match ANY rule (OR)</MenuItem>
                            <MenuItem value={1}>Match ALL rules (AND)</MenuItem>
                        </TextField>

                        {flag.enabledFor.map((filter, index) => (
                            <RuleEditor
                                key={index} index={index} filter={filter}
                                onUpdate={updateRule} onRemove={removeRule}
                            />
                        ))}

                        <Button variant="outlined" startIcon={<AddIcon />} fullWidth onClick={addRule} sx={{ mt: 1 }}>
                            Add Rule
                        </Button>
                    </Box>
                )}
            </DialogContent>
            <DialogActions sx={{ px: 3, py: 2 }}>
                <Button onClick={onClose} color="inherit">Cancel</Button>
                <Button onClick={handleSave} variant="contained" disableElevation disabled={!flag.name}>
                    Save Changes
                </Button>
            </DialogActions>
        </Dialog>
    );
}
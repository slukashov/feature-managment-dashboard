import React from 'react';
import { TextField, MenuItem, IconButton, Card, CardContent, Stack } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import dayjs, { Dayjs } from 'dayjs';
import { FeatureFilter } from '../types/featureFlags';

interface RuleEditorProps {
    filter: FeatureFilter;
    index: number;
    onUpdate: (index: number, updatedFilter: FeatureFilter) => void;
    onRemove: (index: number) => void;
}

export default function RuleEditor({ filter, index, onUpdate, onRemove }: RuleEditorProps) {
    const params = (() => {
        try { return JSON.parse(filter.parametersJson || '{}'); }
        catch { return {}; }
    })();

    const updateName = (newName: string) => {
        let newParams = '{}';
        if (newName === 'Microsoft.Percentage') newParams = JSON.stringify({ Value: 50 });
        else if (newName === 'Microsoft.TimeWindow') newParams = JSON.stringify({ Start: '', End: '' });
        onUpdate(index, { ...filter, name: newName, parametersJson: newParams });
    };

    const updateParams = (newParamsObj: Record<string, any>) => {
        onUpdate(index, { ...filter, parametersJson: JSON.stringify(newParamsObj) });
    };

    const toPickerValue = (value?: string): Dayjs | null => {
        if (!value) return null;
        const parsed = dayjs(value);
        return parsed.isValid() ? parsed : null;
    };

    const toStorageValue = (value: Dayjs | null): string => {
        if (!value) return '';
        return value.format('YYYY-MM-DDTHH:mm');
    };

    const isTimeWindow = filter.name === 'Microsoft.TimeWindow';
    const isPercentage = filter.name === 'Microsoft.Percentage';
    const isCustom = filter.name !== 'AlwaysOn' && !isPercentage && !isTimeWindow;

    return (
        <Card variant="outlined" sx={{ mb: 2, bgcolor: 'background.paper' }}>
            <CardContent sx={{ '&:last-child': { pb: 2 } }}>
                <Stack direction="row" spacing={1} sx={{ mb: 2, alignItems: 'flex-start' }}>
                    <TextField
                        select
                        fullWidth
                        label="Rule Type"
                        size="small"
                        value={filter.name === 'AlwaysOn' ? 'AlwaysOn' : filter.name}
                        onChange={(e) => updateName(e.target.value)}
                    >
                        <MenuItem value="AlwaysOn">Simple: Always On (True)</MenuItem>
                        <MenuItem value="Microsoft.Percentage">Percentage Rollout</MenuItem>
                        <MenuItem value="Microsoft.TimeWindow">Time Window</MenuItem>
                        <MenuItem value="Custom">Custom / Advanced</MenuItem>
                    </TextField>
                    <IconButton color="error" onClick={() => onRemove(index)} size="small" title="Remove Rule">
                        <DeleteIcon />
                    </IconButton>
                </Stack>

                {isPercentage && (
                    <TextField
                        type="number"
                        fullWidth
                        label="Percentage (0-100)"
                        size="small"
                        slotProps={{ htmlInput: { min: 0, max: 100 } }}
                        value={params.Value !== undefined ? params.Value : 50}
                        onChange={(e) => updateParams({ Value: parseInt(e.target.value, 10) || 0 })}
                    />
                )}

                {isTimeWindow && (
                    <Stack direction={{ xs: 'column', md: 'row' }} spacing={2}>
                        <DateTimePicker
                            label="Start Date"
                            value={toPickerValue(params.Start)}
                            onChange={(value) => updateParams({ ...params, Start: toStorageValue(value) })}
                            slotProps={{ textField: { fullWidth: true, size: 'small' } }}
                        />
                        <DateTimePicker
                            label="End Date"
                            value={toPickerValue(params.End)}
                            onChange={(value) => updateParams({ ...params, End: toStorageValue(value) })}
                            slotProps={{ textField: { fullWidth: true, size: 'small' } }}
                        />
                    </Stack>
                )}

                {isCustom && (
                    <TextField
                        fullWidth
                        multiline
                        rows={3}
                        label="Parameters (JSON)"
                        size="small"
                        value={filter.parametersJson}
                        onChange={(e) => {
                            try {
                                updateParams(JSON.parse(e.target.value || '{}'));
                            } catch {
                                onUpdate(index, { ...filter, parametersJson: e.target.value });
                            }
                        }}
                    />
                )}
            </CardContent>
        </Card>
    );
}
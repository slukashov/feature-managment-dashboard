import React from 'react';
import { TextField, MenuItem, IconButton, Card, CardContent, Stack } from '@mui/material';
import DeleteIcon from '@mui/icons-material/Delete';
import { DateTimePicker } from '@mui/x-date-pickers/DateTimePicker';
import dayjs, { Dayjs } from 'dayjs';
import { FeatureFilter } from '../types/featureFlags';

interface RuleEditorProps {
    readonly filter: FeatureFilter;
    readonly index: number;
    readonly onUpdate: (index: number, updatedFilter: FeatureFilter) => void;
    readonly onRemove: (index: number) => void;
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
        else if (newName === 'Microsoft.Targeting') newParams = JSON.stringify({ Audience: { Users: [], Groups: [], Roles: [], IpRanges: [], CustomAttributes: {}, DefaultRolloutPercentage: 0 } });
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
    const isTargeting = filter.name === 'Microsoft.Targeting';
    const isAlwaysOn = filter.name === 'AlwaysOn';
    const isBuiltIn = isAlwaysOn || isPercentage || isTimeWindow || isTargeting;
    const isCustom = isBuiltIn === false;

    const targetingAudience = params?.Audience && typeof params.Audience === 'object' ? params.Audience : {};
    const targetingUsers = Array.isArray(targetingAudience.Users)
        ? targetingAudience.Users.filter((user: unknown): user is string => typeof user === 'string')
        : [];
    const targetingGroups = Array.isArray(targetingAudience.Groups)
        ? targetingAudience.Groups.filter((group: unknown) => group && typeof group === 'object')
        : [];
    const targetingRoles = Array.isArray(targetingAudience.Roles)
        ? targetingAudience.Roles.filter((role: unknown): role is string => typeof role === 'string')
        : [];
    const targetingIpRanges = Array.isArray(targetingAudience.IpRanges)
        ? targetingAudience.IpRanges.filter((ip: unknown): ip is string => typeof ip === 'string')
        : [];
    const targetingCustomAttributes = targetingAudience.CustomAttributes && typeof targetingAudience.CustomAttributes === 'object'
        ? targetingAudience.CustomAttributes
        : {};
    const targetingDefaultRollout = typeof targetingAudience.DefaultRolloutPercentage === 'number'
        ? targetingAudience.DefaultRolloutPercentage
        : 0;

    const updateTargetingAudience = (audiencePatch: Record<string, unknown>) => {
        updateParams({
            Audience: {
                Users: targetingUsers,
                Groups: targetingGroups,
                Roles: targetingRoles,
                IpRanges: targetingIpRanges,
                CustomAttributes: targetingCustomAttributes,
                DefaultRolloutPercentage: targetingDefaultRollout,
                ...targetingAudience,
                ...audiencePatch
            }
        });
    };

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
                        <MenuItem value="Microsoft.Targeting">Segment Targeting</MenuItem>
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
                        onChange={(e) => updateParams({ Value: Number.parseInt(e.target.value, 10) || 0 })}
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

                {isTargeting && (
                    <Stack spacing={2}>
                        <TextField
                            fullWidth
                            size="small"
                            label="Users (comma-separated)"
                            value={targetingUsers.join(', ')}
                            onChange={(e) => {
                                const users = e.target.value
                                    .split(',')
                                    .map((item) => item.trim())
                                    .filter(Boolean);
                                updateTargetingAudience({ Users: users });
                            }}
                        />

                        <TextField
                            type="number"
                            fullWidth
                            size="small"
                            label="Default Rollout Percentage (0-100)"
                            slotProps={{ htmlInput: { min: 0, max: 100 } }}
                            value={targetingDefaultRollout}
                            onChange={(e) => {
                                const value = Number.parseInt(e.target.value, 10);
                                updateTargetingAudience({
                                    DefaultRolloutPercentage: Number.isNaN(value) ? 0 : value
                                });
                            }}
                        />

                        <TextField
                            fullWidth
                            multiline
                            rows={3}
                            size="small"
                            label="Groups (JSON array)"
                            helperText='Example: [{"Name":"beta","RolloutPercentage":50}]'
                            value={JSON.stringify(targetingGroups)}
                            onChange={(e) => {
                                try {
                                    const groups = JSON.parse(e.target.value || '[]');
                                    if (Array.isArray(groups)) {
                                        updateTargetingAudience({ Groups: groups });
                                    }
                                } catch {
                                    onUpdate(index, {
                                        ...filter,
                                        parametersJson: JSON.stringify({
                                            Audience: {
                                                ...targetingAudience,
                                                Users: targetingUsers,
                                                Roles: targetingRoles,
                                                IpRanges: targetingIpRanges,
                                                CustomAttributes: targetingCustomAttributes,
                                                DefaultRolloutPercentage: targetingDefaultRollout,
                                                Groups: e.target.value
                                            }
                                        })
                                    });
                                }
                            }}
                        />

                        <TextField
                            fullWidth
                            size="small"
                            label="Roles (comma-separated)"
                            placeholder="e.g. admin, premium-user, beta-tester"
                            value={targetingRoles.join(', ')}
                            onChange={(e) => {
                                const roles = e.target.value
                                    .split(',')
                                    .map((item) => item.trim())
                                    .filter(Boolean);
                                updateTargetingAudience({ Roles: roles });
                            }}
                        />

                        <TextField
                            fullWidth
                            size="small"
                            label="IP Ranges (comma-separated)"
                            placeholder="e.g. 192.168.1.0/24, 10.0.0.0/8"
                            value={targetingIpRanges.join(', ')}
                            onChange={(e) => {
                                const ranges = e.target.value
                                    .split(',')
                                    .map((item) => item.trim())
                                    .filter(Boolean);
                                updateTargetingAudience({ IpRanges: ranges });
                            }}
                        />

                        <TextField
                            fullWidth
                            multiline
                            rows={2}
                            size="small"
                            label="Custom Attributes (JSON)"
                            placeholder='{"region":["US","EU"],"subscription":"premium"}'
                            value={JSON.stringify(targetingCustomAttributes)}
                            onChange={(e) => {
                                try {
                                    const attrs = JSON.parse(e.target.value || '{}');
                                    if (typeof attrs === 'object' && attrs !== null) {
                                        updateTargetingAudience({ CustomAttributes: attrs });
                                    }
                                } catch {
                                    // Invalid JSON, do nothing
                                }
                            }}
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
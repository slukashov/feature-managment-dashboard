import React, { useMemo, useState } from 'react';
import {
    Box,
    Button,
    Dialog,
    DialogActions,
    DialogContent,
    DialogTitle,
    Divider,
    FormControlLabel,
    MenuItem,
    Stack,
    Switch,
    TextField,
    Typography
} from '@mui/material';
import {
    FeatureFlagExperimentConfiguration,
    FeatureFlagExperimentOutcome,
    FeatureFlagExperimentRecommendation,
    FeatureFlagExperimentVariantAssignment
} from '../types/featureFlags';

interface ExperimentDialogProps {
    readonly open: boolean;
    readonly featureName: string;
    readonly onClose: () => void;
    readonly onConfigure: (name: string, configuration: FeatureFlagExperimentConfiguration) => Promise<boolean>;
    readonly onAssign: (name: string, subjectKey: string) => Promise<FeatureFlagExperimentVariantAssignment | null>;
    readonly onRecordOutcome: (name: string, outcome: FeatureFlagExperimentOutcome) => Promise<boolean>;
    readonly onLoadRecommendation: (name: string) => Promise<FeatureFlagExperimentRecommendation | null>;
}

const DEFAULT_CONFIGURATION: FeatureFlagExperimentConfiguration = {
    baselineVariant: 'A',
    challengerVariant: 'B',
    baselineTrafficPercentage: 50,
    challengerTrafficPercentage: 50,
    conversionMetricName: 'conversion',
    latencyMetricName: 'latency_ms',
    minimumSampleSize: 100,
    isActive: true
};

export default function ExperimentDialog({
    open,
    featureName,
    onClose,
    onConfigure,
    onAssign,
    onRecordOutcome,
    onLoadRecommendation
}: ExperimentDialogProps) {
    const [configuration, setConfiguration] = useState<FeatureFlagExperimentConfiguration>(DEFAULT_CONFIGURATION);
    const [subjectKey, setSubjectKey] = useState('');
    const [assignment, setAssignment] = useState<FeatureFlagExperimentVariantAssignment | null>(null);
    const [outcome, setOutcome] = useState<FeatureFlagExperimentOutcome>({
        variant: 'A',
        converted: false,
        hasError: false,
        latencyMs: 200
    });
    const [recommendation, setRecommendation] = useState<FeatureFlagExperimentRecommendation | null>(null);

    const trafficTotal = useMemo(
        () => configuration.baselineTrafficPercentage + configuration.challengerTrafficPercentage,
        [configuration.baselineTrafficPercentage, configuration.challengerTrafficPercentage]
    );

    const saveConfiguration = async () => {
        if (!featureName) return;
        const success = await onConfigure(featureName, configuration);
        if (success) {
            setOutcome((current) => ({ ...current, variant: configuration.baselineVariant }));
        }
    };

    const assignVariant = async () => {
        if (!featureName || !subjectKey.trim()) return;
        const result = await onAssign(featureName, subjectKey.trim());
        if (result) {
            setAssignment(result);
        }
    };

    const saveOutcome = async () => {
        if (!featureName) return;
        await onRecordOutcome(featureName, outcome);
    };

    const refreshRecommendation = async () => {
        if (!featureName) return;
        const result = await onLoadRecommendation(featureName);
        if (result) {
            setRecommendation(result);
        }
    };

    return (
        <Dialog open={open} onClose={onClose} maxWidth="md" fullWidth>
            <DialogTitle>Experiment Mode: {featureName || 'Feature'}</DialogTitle>
            <DialogContent dividers>
                <Stack spacing={2.5}>
                    <Box>
                        <Typography variant="h6" sx={{ mb: 1 }}>Experiment Configuration</Typography>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                            <TextField
                                label="Baseline Variant"
                                size="small"
                                value={configuration.baselineVariant}
                                onChange={(event) => setConfiguration({ ...configuration, baselineVariant: event.target.value })}
                                fullWidth
                            />
                            <TextField
                                label="Challenger Variant"
                                size="small"
                                value={configuration.challengerVariant}
                                onChange={(event) => setConfiguration({ ...configuration, challengerVariant: event.target.value })}
                                fullWidth
                            />
                        </Stack>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 1.5 }}>
                            <TextField
                                label="Baseline Traffic %"
                                type="number"
                                size="small"
                                value={configuration.baselineTrafficPercentage}
                                onChange={(event) => setConfiguration({ ...configuration, baselineTrafficPercentage: Number.parseInt(event.target.value, 10) || 0 })}
                                fullWidth
                            />
                            <TextField
                                label="Challenger Traffic %"
                                type="number"
                                size="small"
                                value={configuration.challengerTrafficPercentage}
                                onChange={(event) => setConfiguration({ ...configuration, challengerTrafficPercentage: Number.parseInt(event.target.value, 10) || 0 })}
                                fullWidth
                            />
                        </Stack>
                        <Typography variant="caption" color={trafficTotal === 100 ? 'text.secondary' : 'error.main'} sx={{ mt: 0.75, display: 'block' }}>
                            Total traffic: {trafficTotal}% (must equal 100%)
                        </Typography>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 1.5 }}>
                            <TextField
                                label="Conversion Metric"
                                size="small"
                                value={configuration.conversionMetricName}
                                onChange={(event) => setConfiguration({ ...configuration, conversionMetricName: event.target.value })}
                                fullWidth
                            />
                            <TextField
                                label="Latency Metric"
                                size="small"
                                value={configuration.latencyMetricName}
                                onChange={(event) => setConfiguration({ ...configuration, latencyMetricName: event.target.value })}
                                fullWidth
                            />
                        </Stack>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} sx={{ mt: 1.5, alignItems: 'center' }}>
                            <TextField
                                label="Minimum Sample Size"
                                type="number"
                                size="small"
                                value={configuration.minimumSampleSize}
                                onChange={(event) => setConfiguration({ ...configuration, minimumSampleSize: Number.parseInt(event.target.value, 10) || 1 })}
                                fullWidth
                            />
                            <FormControlLabel
                                control={
                                    <Switch
                                        checked={configuration.isActive}
                                        onChange={(event) => setConfiguration({ ...configuration, isActive: event.target.checked })}
                                    />
                                }
                                label="Active"
                            />
                            <Button variant="contained" onClick={saveConfiguration} disabled={trafficTotal !== 100 || !featureName}>
                                Save Config
                            </Button>
                        </Stack>
                    </Box>

                    <Divider />

                    <Box>
                        <Typography variant="h6" sx={{ mb: 1 }}>Variant Assignment</Typography>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                            <TextField
                                label="Subject Key"
                                size="small"
                                value={subjectKey}
                                onChange={(event) => setSubjectKey(event.target.value)}
                                fullWidth
                            />
                            <Button variant="outlined" onClick={assignVariant} disabled={!subjectKey.trim() || !featureName}>
                                Assign Variant
                            </Button>
                        </Stack>
                        {assignment && (
                            <Typography variant="body2" color="text.secondary" sx={{ mt: 1 }}>
                                Assigned variant: {assignment.variant} (bucket {assignment.bucket})
                            </Typography>
                        )}
                    </Box>

                    <Divider />

                    <Box>
                        <Typography variant="h6" sx={{ mb: 1 }}>Record Outcome Sample</Typography>
                        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                            <TextField
                                select
                                label="Variant"
                                size="small"
                                value={outcome.variant}
                                onChange={(event) => setOutcome({ ...outcome, variant: event.target.value })}
                                fullWidth
                            >
                                <MenuItem value={configuration.baselineVariant}>{configuration.baselineVariant || 'Baseline'}</MenuItem>
                                <MenuItem value={configuration.challengerVariant}>{configuration.challengerVariant || 'Challenger'}</MenuItem>
                            </TextField>
                            <TextField
                                label="Latency (ms)"
                                type="number"
                                size="small"
                                value={outcome.latencyMs}
                                onChange={(event) => setOutcome({ ...outcome, latencyMs: Number.parseFloat(event.target.value) || 0 })}
                                fullWidth
                            />
                            <FormControlLabel
                                control={<Switch checked={outcome.converted} onChange={(event) => setOutcome({ ...outcome, converted: event.target.checked })} />}
                                label="Converted"
                            />
                            <FormControlLabel
                                control={<Switch checked={outcome.hasError} onChange={(event) => setOutcome({ ...outcome, hasError: event.target.checked })} />}
                                label="Error"
                            />
                            <Button variant="outlined" onClick={saveOutcome} disabled={!featureName}>
                                Record
                            </Button>
                        </Stack>
                    </Box>

                    <Divider />

                    <Box>
                        <Stack direction="row" sx={{ justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                            <Typography variant="h6">Recommendation</Typography>
                            <Button variant="contained" onClick={refreshRecommendation} disabled={!featureName}>
                                Refresh
                            </Button>
                        </Stack>
                        {recommendation ? (
                            <Stack spacing={0.75}>
                                <Typography variant="body2">Status: {recommendation.status}</Typography>
                                <Typography variant="body2">Recommended: {recommendation.recommendedVariant ?? 'N/A'}</Typography>
                                <Typography variant="body2" color="text.secondary">{recommendation.reason}</Typography>
                                <Typography variant="subtitle2" sx={{ mt: 1 }}>Baseline ({recommendation.baseline.variant})</Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Sample {recommendation.baseline.sampleSize}, Conversion {(recommendation.baseline.conversionRate * 100).toFixed(2)}%, Error {(recommendation.baseline.errorRate * 100).toFixed(2)}%, Avg Latency {recommendation.baseline.averageLatencyMs.toFixed(1)}ms
                                </Typography>
                                <Typography variant="subtitle2">Challenger ({recommendation.challenger.variant})</Typography>
                                <Typography variant="body2" color="text.secondary">
                                    Sample {recommendation.challenger.sampleSize}, Conversion {(recommendation.challenger.conversionRate * 100).toFixed(2)}%, Error {(recommendation.challenger.errorRate * 100).toFixed(2)}%, Avg Latency {recommendation.challenger.averageLatencyMs.toFixed(1)}ms
                                </Typography>
                            </Stack>
                        ) : (
                            <Typography variant="body2" color="text.secondary">No recommendation loaded yet.</Typography>
                        )}
                    </Box>
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}


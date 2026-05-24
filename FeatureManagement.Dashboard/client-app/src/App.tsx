import React, { useState, useCallback, useMemo } from 'react';
import {
    Container,
    Typography,
    Button,
    Box,
    Snackbar,
    Alert,
    Card,
    CardContent,
    Stack,
    IconButton,
    CssBaseline
} from '@mui/material';
import { ThemeProvider, createTheme, alpha } from '@mui/material/styles';
import AddIcon from '@mui/icons-material/Add';
import LightModeIcon from '@mui/icons-material/LightMode';
import DarkModeIcon from '@mui/icons-material/DarkMode';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';
import FeatureGrid from './components/FeatureGrid';
import FeatureDialog from './components/FeatureDialog';
import { useFeatureFlags } from './hooks/useFeatureFlags';
import { FeatureFlag, NotificationSeverity } from './types/featureFlags';

const DEFAULT_FLAG: FeatureFlag = { name: '', requirementType: 0, enabledFor: [] };

interface ToastState {
    open: boolean;
    message: string;
    severity: NotificationSeverity;
}

export default function App() {
    const [mode, setMode] = useState<'light' | 'dark'>('light');

    const theme = useMemo(() => createTheme({
        palette: mode === 'dark'
            ? {
                mode: 'dark',
                primary: { main: '#7fb3ff' },
                success: { main: '#57c77a' },
                background: { default: '#0b1220', paper: '#111a2b' },
                text: { primary: '#e5e7eb', secondary: '#aab4c3' },
                divider: alpha('#e5e7eb', 0.12)
            }
            : {
                mode: 'light',
                primary: { main: '#356ce1' },
                success: { main: '#2e7d32' },
                background: { default: '#f3f6fb', paper: '#ffffff' }
            },
        shape: { borderRadius: 12 },
        components: {
            MuiCard: {
                styleOverrides: {
                    root: {
                        borderColor: mode === 'dark' ? alpha('#cbd5e1', 0.18) : alpha('#1e293b', 0.12),
                        backgroundImage: 'none'
                    }
                }
            },
            MuiDialog: {
                styleOverrides: {
                    paper: {
                        backgroundImage: 'none',
                        border: `1px solid ${mode === 'dark' ? alpha('#cbd5e1', 0.16) : alpha('#1e293b', 0.12)}`
                    }
                }
            },
            MuiOutlinedInput: {
                styleOverrides: {
                    root: {
                        backgroundColor: mode === 'dark' ? alpha('#0b1220', 0.45) : '#fff'
                    }
                }
            }
        }
    }), [mode]);

    // Global UI State
    const [dialogOpen, setDialogOpen] = useState<boolean>(false);
    const [editingFlag, setEditingFlag] = useState<FeatureFlag>(DEFAULT_FLAG);
    const [isNew, setIsNew] = useState<boolean>(true);

    // Notification State
    const [toast, setToast] = useState<ToastState>({ open: false, message: '', severity: 'success' });

    const showNotification = useCallback((message: string, severity: NotificationSeverity) => {
        setToast({ open: true, message, severity });
    }, []);

    // Custom Hook
    const { flags, isLoading, saveFlag, toggleFlag } = useFeatureFlags(showNotification);

    // Handlers
    const handleAddClick = () => {
        setEditingFlag(DEFAULT_FLAG);
        setIsNew(true);
        setDialogOpen(true);
    };

    const handleEditClick = (flag: FeatureFlag) => {
        setEditingFlag(flag);
        setIsNew(false);
        setDialogOpen(true);
    };

    const handleSave = async (flagData: FeatureFlag, isNewData: boolean) => {
        const success = await saveFlag(flagData, isNewData);
        if (success) setDialogOpen(false);
    };

    return (
        <ThemeProvider theme={theme}>
            <CssBaseline />
            <LocalizationProvider dateAdapter={AdapterDayjs}>
                <Box
                    sx={{
                        bgcolor: 'background.default',
                        minHeight: '100vh',
                        py: 4
                    }}
                >
                    <Container maxWidth="lg">
                        <Card elevation={0} variant="outlined" sx={{ mb: 3 }}>
                            <CardContent>
                                <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} sx={{ justifyContent: 'space-between' }}>
                                    <Box>
                                        <Typography variant="h4" sx={{ fontWeight: 700 }}>
                                            Feature Management
                                        </Typography>
                                        <Typography variant="body1" color="text.secondary">
                                            Control rollout and feature targeting dynamically.
                                        </Typography>
                                    </Box>
                                    <Stack direction="row" spacing={1} sx={{ alignItems: 'center' }}>
                                        <IconButton
                                            aria-label="toggle-theme"
                                            onClick={() => setMode((prev) => prev === 'light' ? 'dark' : 'light')}
                                            color="primary"
                                            sx={{
                                                border: '1px solid',
                                                borderColor: 'divider',
                                                bgcolor: 'background.paper'
                                            }}
                                        >
                                            {mode === 'light' ? <DarkModeIcon /> : <LightModeIcon />}
                                        </IconButton>
                                        <Button variant="contained" startIcon={<AddIcon />} disableElevation onClick={handleAddClick}>
                                            New Feature Flag
                                        </Button>
                                    </Stack>
                                </Stack>
                            </CardContent>
                        </Card>

                        <FeatureGrid
                            flags={flags}
                            isLoading={isLoading}
                            onToggle={toggleFlag}
                            onEdit={handleEditClick}
                        />

                        <FeatureDialog
                            open={dialogOpen}
                            onClose={() => setDialogOpen(false)}
                            onSave={handleSave}
                            initialData={editingFlag}
                            isNew={isNew}
                        />
                    </Container>

                    <Snackbar
                        open={toast.open}
                        autoHideDuration={4000}
                        onClose={() => setToast({ ...toast, open: false })}
                        anchorOrigin={{ vertical: 'bottom', horizontal: 'right' }}
                    >
                        <Alert onClose={() => setToast({ ...toast, open: false })} severity={toast.severity} sx={{ width: '100%' }}>
                            {toast.message}
                        </Alert>
                    </Snackbar>
                </Box>
            </LocalizationProvider>
        </ThemeProvider>
    );
}
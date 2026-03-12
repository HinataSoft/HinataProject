import { Box, Typography, Button, Card, CardContent, Alert, CircularProgress } from '@mui/material';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../stores/authStore';
import { useEffect, useState } from 'react';
import { oauthApi, OAuthSettings } from '../api/oauth';
import { usersApi } from '../api/users';
import axios from 'axios';

function Login() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const login = useAuthStore((state) => state.login);
  
  const [oauthSettings, setOauthSettings] = useState<OAuthSettings | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [processingCallback, setProcessingCallback] = useState(false);

  // Check if we're handling an OAuth callback
  const code = searchParams.get('code');
  const errorParam = searchParams.get('error');

  useEffect(() => {
    // Fetch OAuth settings from backend
    oauthApi.getSettings()
      .then(settings => {
        setOauthSettings(settings);
        setLoading(false);
      })
      .catch(err => {
        console.error('Failed to fetch OAuth settings:', err);
        setError('Failed to load OAuth configuration. Please try again later.');
        setLoading(false);
      });
  }, []);

  useEffect(() => {
    // Handle OAuth callback
    if (code && oauthSettings) {
      handleOAuthCallback(code);
    }
  }, [code, oauthSettings]);

  const handleOAuthCallback = async (authCode: string) => {
    setProcessingCallback(true);
    try {
      // Exchange authorization code for tokens
      // Note: This should ideally be done server-side to protect client_secret
      // For now, we send the code to the backend which will exchange it
      const response = await axios.post('/api/oauth/token', {
        code: authCode,
        redirect_uri: oauthSettings!.redirectUri,
      }, {
        headers: {
          'Content-Type': 'application/json',
        }
      });

      const { access_token, refresh_token } = response.data;
      
      // Store tokens
      if (access_token) {
        localStorage.setItem('access_token', access_token);
        if (refresh_token) {
          localStorage.setItem('refresh_token', refresh_token);
        }

        // Fetch current user from backend
        const user = await usersApi.getCurrentUser();
        login(access_token, user);
        navigate('/');
      }
    } catch (err) {
      console.error('OAuth callback error:', err);
      setError('Failed to complete authentication. Please try again.');
    } finally {
      setProcessingCallback(false);
    }
  };

  const handleOAuthLogin = () => {
    if (!oauthSettings) return;

    // Generate state for CSRF protection
    const state = crypto.randomUUID();
    localStorage.setItem('oauth_state', state);

    // Build authorization URL
    const params = new URLSearchParams({
      response_type: 'code',
      client_id: oauthSettings.clientId,
      redirect_uri: oauthSettings.redirectUri,
      scope: oauthSettings.scope,
      state: state,
    });

    // Redirect to OAuth provider
    window.location.href = `${oauthSettings.authorizationEndpoint}?${params.toString()}`;
  };

  // Show error from OAuth callback
  if (errorParam) {
    return (
      <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h5" gutterBottom>
              Login Failed
            </Typography>
            <Alert severity="error" sx={{ mb: 2 }}>
              {searchParams.get('error_description') || 'Authorization was denied.'}
            </Alert>
            <Button variant="contained" fullWidth onClick={() => navigate('/login')}>
              Try Again
            </Button>
          </CardContent>
        </Card>
      </Box>
    );
  }

  // Show loading state
  if (loading) {
    return (
      <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4, display: 'flex', justifyContent: 'center' }}>
        <CircularProgress />
      </Box>
    );
  }

  // Show processing state during OAuth callback
  if (processingCallback) {
    return (
      <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h5" gutterBottom>
              Completing Login...
            </Typography>
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
              <CircularProgress />
            </Box>
            <Alert severity="info">
              Please wait while we complete your authentication.
            </Alert>
          </CardContent>
        </Card>
      </Box>
    );
  }

  // Show error state
  if (error) {
    return (
      <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
        <Card>
          <CardContent>
            <Typography variant="h5" gutterBottom>
              Login Error
            </Typography>
            <Alert severity="error" sx={{ mb: 2 }}>
              {error}
            </Alert>
            <Button variant="contained" fullWidth onClick={() => window.location.reload()}>
              Try Again
            </Button>
          </CardContent>
        </Card>
      </Box>
    );
  }

  // Show OAuth login button
  return (
    <Box sx={{ maxWidth: 400, mx: 'auto', mt: 4 }}>
      <Card>
        <CardContent>
          <Typography variant="h5" gutterBottom>
            Login
          </Typography>
          <Alert severity="info" sx={{ mb: 2 }}>
            Sign in with your OAuth provider to access HinataProject.
          </Alert>
          <Button 
            variant="contained" 
            fullWidth 
            onClick={handleOAuthLogin}
            disabled={!oauthSettings}
          >
            Login with OAuth
          </Button>
        </CardContent>
      </Card>
    </Box>
  );
}

export default Login;

import { Routes, Route, Navigate, useNavigate } from 'react-router-dom';
import { Box, AppBar, Toolbar, Typography, Button, IconButton, Container } from '@mui/material';
import HomeIcon from '@mui/icons-material/Home';
import { useAuthStore } from './stores/authStore';
import Home from './pages/Home';
import Login from './pages/Login';
import NodeDetailPage from './pages/NodeDetailPage';

function App() {
  const { isAuthenticated, user, logout } = useAuthStore();
  const navigate = useNavigate();

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100vh' }}>
      <AppBar position="static">
        <Toolbar>
          <Typography variant="h6" component="div">
            HinataProject
          </Typography>
          <IconButton color="inherit" onClick={() => navigate('/')} title="Home" sx={{ ml: 1 }}>
            <HomeIcon />
          </IconButton>
          <Box sx={{ flexGrow: 1 }} />
          {isAuthenticated ? (
            <>
              {user && (
                <Typography
                  variant="body2"
                  sx={{
                    mr: 2,
                    px: 1.5,
                    py: 0.5,
                    borderRadius: 1,
                    backgroundColor: 'rgba(255, 255, 255, 0.15)',
                    fontWeight: 500,
                  }}
                >
                  {user.name}
                </Typography>
              )}
              <Button variant="outlined" color="inherit" size="small" onClick={logout}>
                Logout
              </Button>
            </>
          ) : (
            <Button variant="outlined" color="inherit" size="small" href="/login">
              Login
            </Button>
          )}
        </Toolbar>
      </AppBar>
      <Container component="main" sx={{ flexGrow: 1, py: 3 }}>
        <Routes>
          <Route path="/" element={<Home />} />
          <Route path="/login" element={<Login />} />
          <Route path="/callback" element={<Login />} />
          {/* /nodes redirects to root node */}
          <Route path="/nodes" element={<Navigate to="/nodes/root" replace />} />
          <Route path="/nodes/:id" element={<NodeDetailPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Container>
    </Box>
  );
}

export default App;

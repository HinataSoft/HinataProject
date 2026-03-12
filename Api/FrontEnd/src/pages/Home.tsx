import { Box, Typography, Button, Card, CardContent, Grid } from '@mui/material';
import { useNavigate } from 'react-router-dom';

function Home() {
  const navigate = useNavigate();

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Welcome to HinataProject
      </Typography>
      <Typography variant="body1" paragraph>
        A hierarchical content management system with workflow support.
      </Typography>
      <Grid container spacing={3} sx={{ mt: 2 }}>
        <Grid item xs={12} sm={6} md={4}>
          <Card>
            <CardContent>
              <Typography variant="h6">Browse Nodes</Typography>
              <Typography variant="body2" color="text.secondary" paragraph>
                Explore the node hierarchy and navigate through content.
              </Typography>
              <Button variant="contained" onClick={() => navigate('/nodes')}>
                Open Browser
              </Button>
            </CardContent>
          </Card>
        </Grid>
      </Grid>
    </Box>
  );
}

export default Home;

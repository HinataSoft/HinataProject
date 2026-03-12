import { useState } from 'react';
import { Box, Typography, CircularProgress, Alert, Button, Stack } from '@mui/material';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { nodesApi } from '../api/nodes';
import { useAuthStore } from '../stores/authStore';
import NodeBriefCard from '../components/NodeBriefCard';

const PAGE_SIZE = 10;

function Home() {
  const navigate = useNavigate();
  const { isAuthenticated } = useAuthStore();
  const [page, setPage] = useState(0);

  const rootIdQuery = useQuery({
    queryKey: ['rootId'],
    queryFn: () => nodesApi.getRootId(),
  });

  const rootNodeQuery = useQuery({
    queryKey: ['node', rootIdQuery.data],
    queryFn: () => nodesApi.getById(rootIdQuery.data!),
    enabled: !!rootIdQuery.data,
  });

  const assignedQuery = useQuery({
    queryKey: ['assignedToMe', page],
    queryFn: () => nodesApi.getAssignedToMe(page * PAGE_SIZE, PAGE_SIZE),
    enabled: isAuthenticated,
  });

  const totalPages = assignedQuery.data
    ? Math.ceil(assignedQuery.data.totalCount / PAGE_SIZE)
    : 0;

  return (
    <Box>
      <Typography variant="h4" gutterBottom>
        Welcome to HinataProject
      </Typography>
      <Typography variant="body1" paragraph>
        A hierarchical content management system with workflow support.
      </Typography>
      <Box sx={{ mt: 3 }}>
        {(rootIdQuery.isLoading || rootNodeQuery.isLoading) && <CircularProgress size={24} />}
        {rootNodeQuery.isError && (
          <Alert severity="error">Failed to load root node.</Alert>
        )}
        {rootNodeQuery.data && (
          <NodeBriefCard
            node={rootNodeQuery.data}
            onClick={(id) => navigate(`/nodes/${id}`)}
          />
        )}
      </Box>

      {isAuthenticated && (
        <Box sx={{ mt: 4 }}>
          <Typography variant="h5" gutterBottom>
            My Work
          </Typography>
          {assignedQuery.isLoading && <CircularProgress size={24} />}
          {assignedQuery.isError && (
            <Alert severity="error">Failed to load assigned nodes.</Alert>
          )}
          {assignedQuery.data && assignedQuery.data.items.length === 0 && (
            <Typography color="text.secondary">No nodes assigned to you.</Typography>
          )}
          {assignedQuery.data && assignedQuery.data.items.length > 0 && (
            <Stack spacing={1}>
              {assignedQuery.data.items.map((node) => (
                <NodeBriefCard
                  key={node.id}
                  node={node}
                  onClick={(id) => navigate(`/nodes/${id}`)}
                />
              ))}
              {totalPages > 1 && (
                <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 2, mt: 1 }}>
                  <Button
                    size="small"
                    disabled={page === 0}
                    onClick={() => setPage((p) => p - 1)}
                  >
                    Previous
                  </Button>
                  <Typography variant="body2" color="text.secondary">
                    Page {page + 1} of {totalPages}
                  </Typography>
                  <Button
                    size="small"
                    disabled={page + 1 >= totalPages}
                    onClick={() => setPage((p) => p + 1)}
                  >
                    Next
                  </Button>
                </Box>
              )}
            </Stack>
          )}
        </Box>
      )}
    </Box>
  );
}

export default Home;

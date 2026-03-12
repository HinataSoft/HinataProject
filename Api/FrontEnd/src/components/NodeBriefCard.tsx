import { Box, Card, CardActionArea, Chip, Typography } from '@mui/material';
import { Node, NodeListItem } from '../types';

type NodeBriefData = Pick<Node, 'id' | 'publicId' | 'caption' | 'type' | 'state'>;

interface NodeBriefCardProps {
  node: NodeBriefData | NodeListItem;
  onClick?: (id: string) => void;
}

function NodeBriefCard({ node, onClick }: NodeBriefCardProps) {
  const content = (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, p: 2 }}>
      {node.type && (
        <Chip
          label={node.type.name.toUpperCase()}
          size="small"
          sx={{
            bgcolor: node.type.color || 'grey.500',
            color: 'white',
            fontWeight: 'bold',
          }}
        />
      )}
      <Box
        component="span"
        sx={{
          px: 1,
          py: 0.5,
          bgcolor: 'grey.200',
          borderRadius: 1,
          fontSize: '0.85rem',
          fontWeight: 'bold',
          color: 'text.primary',
        }}
      >
        #{node.publicId}
      </Box>
      {node.state && (
        <Chip
          label={node.state.name}
          variant="outlined"
          size="small"
          color={
            'isFinalSuccess' in node.state && node.state.isFinalSuccess
              ? 'success'
              : 'isFinalFailure' in node.state && node.state.isFinalFailure
                ? 'error'
                : 'default'
          }
        />
      )}
      <Typography variant="body1" sx={{ fontWeight: 500 }}>
        {node.caption || `Node #${node.publicId}`}
      </Typography>
    </Box>
  );

  if (onClick) {
    return (
      <Card variant="outlined">
        <CardActionArea onClick={() => onClick(node.id)}>
          {content}
        </CardActionArea>
      </Card>
    );
  }

  return <Card variant="outlined">{content}</Card>;
}

export default NodeBriefCard;

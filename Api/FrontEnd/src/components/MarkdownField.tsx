import { useState } from 'react';
import {
  Box,
  Typography,
  IconButton,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Button,
  TextField,
} from '@mui/material';
import EditIcon from '@mui/icons-material/Edit';
import ReactMarkdown from 'react-markdown';

interface MarkdownFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  placeholder?: string;
  actions?: React.ReactNode;
}

function MarkdownField({ label, value, onChange, placeholder, actions }: MarkdownFieldProps) {
  const [editOpen, setEditOpen] = useState(false);
  const [draft, setDraft] = useState('');

  const handleOpen = () => {
    setDraft(value);
    setEditOpen(true);
  };

  const handleOk = () => {
    onChange(draft);
    setEditOpen(false);
  };

  const handleCancel = () => {
    setEditOpen(false);
  };

  return (
    <>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
        <Typography variant="h6">{label}</Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
          {actions}
          <IconButton size="small" onClick={handleOpen} title={`Edit ${label}`}>
            <EditIcon fontSize="small" />
          </IconButton>
        </Box>
      </Box>
      <Box
        sx={{
          minHeight: 60,
          p: 1.5,
          border: '1px solid',
          borderColor: 'divider',
          borderRadius: 1,
          '& img': { maxWidth: '100%' },
          '& pre': { overflow: 'auto', bgcolor: 'grey.100', p: 1, borderRadius: 1 },
          '& code': { bgcolor: 'grey.100', px: 0.5, borderRadius: 0.5, fontSize: '0.875em' },
          '& a': { color: 'primary.main' },
        }}
      >
        {value ? (
          <ReactMarkdown>{value}</ReactMarkdown>
        ) : (
          <Typography color="text.secondary" sx={{ fontStyle: 'italic' }}>
            {placeholder || 'Empty'}
          </Typography>
        )}
      </Box>

      <Dialog open={editOpen} onClose={handleCancel} fullWidth maxWidth="md">
        <DialogTitle>Edit {label}</DialogTitle>
        <DialogContent>
          <TextField
            autoFocus
            fullWidth
            multiline
            minRows={10}
            maxRows={30}
            value={draft}
            onChange={(e) => setDraft(e.target.value)}
            variant="outlined"
            placeholder={placeholder}
            sx={{ mt: 1 }}
          />
        </DialogContent>
        <DialogActions>
          <Button onClick={handleCancel}>Cancel</Button>
          <Button variant="contained" onClick={handleOk}>OK</Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export default MarkdownField;

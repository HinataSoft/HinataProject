import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Box,
  Typography,
  Button,
  Card,
  CardContent,
  Chip,
  CircularProgress,
  Alert,
  TextField,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Divider,
  IconButton,
  Grid,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  FormControl,
  InputLabel,
  Select,
  MenuItem,
  Menu,
  Dialog,
  DialogTitle,
  DialogContent,
  DialogActions,
  Checkbox,
  ListItemIcon,
} from '@mui/material';
import ContentCopyIcon from '@mui/icons-material/ContentCopy';
import ArrowBackIcon from '@mui/icons-material/ArrowBack';
import SaveIcon from '@mui/icons-material/Save';
import AddIcon from '@mui/icons-material/Add';
import MoreVertIcon from '@mui/icons-material/MoreVert';
import DeleteIcon from '@mui/icons-material/Delete';
import { nodesApi } from '../api/nodes';
import { commentsApi } from '../api/comments';
import { usersApi } from '../api/users';
import { Node, Comment, NodeListItem, UpdateNodeDto, CreateNodeDto, InheritedType, InheritedState, InheritedRole, InheritedWorkflow, WorkflowWithStates, WorkflowState } from '../types';
import { useAuthStore } from '../stores/authStore';
import CheckBoxIcon from '@mui/icons-material/CheckBox';
import MarkdownField from '../components/MarkdownField';

function NodeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isAdmin } = useAuthStore();
  const [newComment, setNewComment] = useState('');
  
  // Local state for editable fields
  const [description, setDescription] = useState('');
  const [manifest, setManifest] = useState('');
  const [summary, setSummary] = useState('');
  const [isSaving, setIsSaving] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);

  // Redirect from "root" to actual GUID
  useEffect(() => {
    if (id === 'root') {
      nodesApi.getRootId().then(rootId => {
        navigate(`/nodes/${rootId}`, { replace: true });
      });
    }
  }, [id, navigate]);

  // Local state for workflow selection
  const [selectedWorkflowId, setSelectedWorkflowId] = useState<string>('');
  
  // Local state for state selection
  const [selectedStateId, setSelectedStateId] = useState<string>('');

  // Local state for creating new child
  const [newChildType, setNewChildType] = useState<string>('');
  const [newChildCaption, setNewChildCaption] = useState('');
  const [createChildError, setCreateChildError] = useState<string | null>(null);

  // Menu state
  const [menuAnchor, setMenuAnchor] = useState<null | HTMLElement>(null);
  const menuOpen = Boolean(menuAnchor);

  // Edit Types dialog state
  const [editTypesOpen, setEditTypesOpen] = useState(false);
  const [addedTypes, setAddedTypes] = useState<{ typeId: string; name: string; kind: string; color: string; defaultWorkflowId?: string; isNew?: boolean }[]>([]);
  const [removedTypeIds, setRemovedTypeIds] = useState<string[]>([]);
  const [newTypeName, setNewTypeName] = useState('');
  const [newTypeKind, setNewTypeKind] = useState<'Structural' | 'Stateful'>('Structural');
  const [newTypeColor, setNewTypeColor] = useState('#1976d2');
  const [newTypeDefaultWorkflowId, setNewTypeDefaultWorkflowId] = useState<string>('');

  // Edit Roles dialog state
  const [editRolesOpen, setEditRolesOpen] = useState(false);
  const [addedRoles, setAddedRoles] = useState<{ roleId: string; name: string; states: string[]; isNew?: boolean }[]>([]);
  const [removedRoleIds, setRemovedRoleIds] = useState<string[]>([]);
  const [newRoleName, setNewRoleName] = useState('');
  const [newRoleStates, setNewRoleStates] = useState<string[]>([]);

  // Edit Workflows dialog state
  const [editWorkflowsOpen, setEditWorkflowsOpen] = useState(false);
  const [addedWorkflows, setAddedWorkflows] = useState<WorkflowWithStates[]>([]);
  const [removedWorkflowIds, setRemovedWorkflowIds] = useState<string[]>([]);
  const [newWorkflowName, setNewWorkflowName] = useState('');

  // Edit Users dialog state
  const [editUsersOpen, setEditUsersOpen] = useState(false);

  // State for creating new states within a workflow
  const [newStateName, setNewStateName] = useState('');
  const [newStateIsFinalSuccess, setNewStateIsFinalSuccess] = useState(false);
  const [newStateIsFinalFailure, setNewStateIsFinalFailure] = useState(false);
  const [editingWorkflowId, setEditingWorkflowId] = useState<string | null>(null);

  // Fetch node details - defined early so it can be used in useEffect hooks
  const nodeQuery = useQuery({
    queryKey: ['node', id],
    queryFn: () => nodesApi.getById(id!),
    enabled: !!id,
  });

  // Assignees state
  const [assigneeMapping, setAssigneeMapping] = useState<Record<string, string | null>>({});
  const [availableUsers, setAvailableUsers] = useState<{ id: string; name: string }[]>([]);
  const [isLoadingUsers, setIsLoadingUsers] = useState(false);

  // Fetch users for assignee selection
  useEffect(() => {
    if (nodeQuery.data?.workflow && nodeQuery.data?.type?.kind === 'Stateful') {
      setIsLoadingUsers(true);
      usersApi.list()
        .then(users => setAvailableUsers(users))
        .catch(() => setAvailableUsers([]))
        .finally(() => setIsLoadingUsers(false));
    } else {
      setAvailableUsers([]);
    }
  }, [nodeQuery.data?.workflow, nodeQuery.data?.type?.kind]);

  // Fetch assignees when workflow is selected
  useEffect(() => {
    if (nodeQuery.data?.workflow && id) {
      nodesApi.getAssignees(id)
        .then(data => setAssigneeMapping(data.data || {}))
        .catch(() => setAssigneeMapping({}));
    } else {
      setAssigneeMapping({});
    }
  }, [nodeQuery.data?.workflow, id]);

  // Set assignees mutation
  const setAssigneesMutation = useMutation({
    mutationFn: async (assignees: Record<string, string | null>) => {
      // Filter out null values and convert to the format backend expects
      const filteredAssignees: Record<string, string> = {};
      Object.entries(assignees).forEach(([roleId, userId]) => {
        if (userId) {
          filteredAssignees[roleId] = userId;
        }
      });
      await nodesApi.setAssignees(id!, filteredAssignees);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
    },
  });

  const handleAssigneeChange = (roleId: string, userId: string | null) => {
    const newMapping = { ...assigneeMapping, [roleId]: userId };
    setAssigneeMapping(newMapping);
    setAssigneesMutation.mutate(newMapping);
  };

  // Reset state when node changes
  useEffect(() => {
    setNewComment('');
    setDescription('');
    setManifest('');
    setSummary('');
    setSaveSuccess(false);
    setNewChildType('');
    setNewChildCaption('');
    setCreateChildError(null);
    setMenuAnchor(null);
    setEditTypesOpen(false);
    setEditRolesOpen(false);
    setEditWorkflowsOpen(false);
  }, [id]);

  // Initialize edit types dialog when opening
  useEffect(() => {
    if (editTypesOpen && nodeQuery.data?.changedTypes) {
      setAddedTypes(nodeQuery.data.changedTypes.added.map(t => ({
        typeId: t.typeId,
        name: t.name || '',
        kind: (t.kind as 'Structural' | 'Stateful') || 'Structural',
        color: t.color || '#1976d2',
      })));
      setRemovedTypeIds(nodeQuery.data.changedTypes.removed.map(t => t.typeId));
    } else if (editTypesOpen) {
      setAddedTypes([]);
      setRemovedTypeIds([]);
    }
    setNewTypeName('');
    setNewTypeKind('Structural');
    setNewTypeColor('#1976d2');
  }, [editTypesOpen, nodeQuery.data?.changedTypes]);

  // Initialize edit roles dialog when opening
  useEffect(() => {
    if (editRolesOpen && nodeQuery.data?.changedRoles) {
      setAddedRoles(nodeQuery.data.changedRoles.added.map(r => ({
        roleId: r.roleId,
        name: r.name || '',
        states: r.states?.map(s => s.id) || [],
      })));
      setRemovedRoleIds(nodeQuery.data.changedRoles.removed.map(r => r.roleId));
    } else if (editRolesOpen) {
      setAddedRoles([]);
      setRemovedRoleIds([]);
    }
    setNewRoleName('');
    setNewRoleStates([]);
  }, [editRolesOpen, nodeQuery.data?.changedRoles]);

  // Update local state when node data loads
  useEffect(() => {
    if (nodeQuery.data) {
      setDescription(nodeQuery.data.description || '');
      setManifest(nodeQuery.data.manifest || '');
      setSummary(nodeQuery.data.summary || '');
      // Initialize workflow and state selection for stateful nodes
      if (nodeQuery.data.type?.kind === 'Stateful') {
        setSelectedWorkflowId(nodeQuery.data.workflow?.id || '');
        setSelectedStateId(nodeQuery.data.state?.id || '');
      }
    }
  }, [nodeQuery.data]);

  // Set workflow mutation
  const setWorkflowMutation = useMutation({
    mutationFn: async (workflowId: string) => {
      await nodesApi.setWorkflow(id!, { workflowId });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
    },
  });

  const handleWorkflowChange = (workflowId: string) => {
    setSelectedWorkflowId(workflowId);
    setSelectedStateId('');
    if (workflowId) {
      setWorkflowMutation.mutate(workflowId);
    }
  };

  // Set state mutation
  const setStateMutation = useMutation({
    mutationFn: async (targetStateId: string) => {
      await nodesApi.setState(id!, { targetStateId });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
    },
  });

  const handleStateChange = (targetStateId: string) => {
    setSelectedStateId(targetStateId);
    if (targetStateId) {
      setStateMutation.mutate(targetStateId);
    }
  };

  // Fetch parent node
  const parentQuery = useQuery({
    queryKey: ['node', nodeQuery.data?.parentId],
    queryFn: () => nodeQuery.data?.parentId ? nodesApi.getById(nodeQuery.data.parentId) : undefined,
    enabled: !!nodeQuery.data?.parentId,
  });

  // Fetch children
  const childrenQuery = useQuery({
    queryKey: ['node', id, 'children'],
    queryFn: () => nodesApi.getChildren(id!, 0, 100).then(r => r.items),
    enabled: !!id,
  });

  // Fetch comments
  const commentsQuery = useQuery({
    queryKey: ['comments', id],
    queryFn: () => commentsApi.list(id!, 0, 50).then(r => r.items),
    enabled: !!id,
  });

  // Update node mutation
  const updateNodeMutation = useMutation({
    mutationFn: async (data: UpdateNodeDto) => {
      setIsSaving(true);
      await nodesApi.update(id!, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
      setIsSaving(false);
      setSaveSuccess(true);
      setTimeout(() => setSaveSuccess(false), 2000);
    },
    onError: () => {
      setIsSaving(false);
    },
  });

  const handleSave = () => {
    const updates: UpdateNodeDto = {};
    if (description !== nodeQuery.data?.description) {
      updates.description = description;
    }
    if (manifest !== nodeQuery.data?.manifest) {
      updates.manifest = manifest;
    }
    if (summary !== nodeQuery.data?.summary) {
      updates.summary = summary;
    }
    if (Object.keys(updates).length > 0) {
      updateNodeMutation.mutate(updates);
    }
  };

  // Add comment mutation
  const addCommentMutation = useMutation({
    mutationFn: (text: string) => commentsApi.add(id!, { text }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['comments', id] });
      setNewComment('');
    },
  });

  const handleAddComment = () => {
    if (newComment.trim()) {
      addCommentMutation.mutate(newComment.trim());
    }
  };

  const handleCopyLink = () => {
    navigator.clipboard.writeText(window.location.href);
  };

  const handleChildClick = (childId: string) => {
    navigate(`/nodes/${childId}`);
  };

  const handleParentClick = () => {
    if (nodeQuery.data?.parentId) {
      navigate(`/nodes/${nodeQuery.data.parentId}`);
    }
  };

  // Create child node mutation
  const createChildMutation = useMutation({
    mutationFn: async (data: CreateNodeDto) => {
      return nodesApi.create(data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id, 'children'] });
      setNewChildType('');
      setNewChildCaption('');
      setCreateChildError(null);
    },
    onError: (error: any) => {
      const errorMessage = error?.response?.data?.error || error.message || 'Failed to create node';
      setCreateChildError(errorMessage);
    },
  });

  const handleCreateChild = () => {
    if (newChildType && newChildCaption.trim()) {
      createChildMutation.mutate({
        ParentId: id!,
        TypeId: newChildType,
        Caption: newChildCaption.trim(),
      });
    }
  };

  const canCreateChild = newChildType && newChildCaption.trim();

  // Set types mutation
  const setTypesMutation = useMutation({
    mutationFn: async (data: { added: { typeId: string | null; name?: string; kind?: string; color?: string }[]; removed: { typeId: string }[] }) => {
      await nodesApi.setTypes(id!, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
      setEditTypesOpen(false);
    },
  });

  const handleSaveTypes = () => {
    setTypesMutation.mutate({
      added: addedTypes.map(t => ({
        typeId: t.isNew ? null : t.typeId,
        name: t.name,
        kind: t.kind,
        color: t.color,
      })),
      removed: removedTypeIds.map(typeId => ({ typeId })),
    });
  };

  const handleAddNewType = () => {
    if (newTypeName.trim()) {
      const newType = {
        typeId: `new-${Date.now()}`,
        name: newTypeName.trim(),
        kind: newTypeKind,
        color: newTypeColor,
        defaultWorkflowId: newTypeDefaultWorkflowId || undefined,
        isNew: true,
      };
      setAddedTypes([...addedTypes, newType]);
      setNewTypeName('');
      setNewTypeKind('Structural');
      setNewTypeColor('#1976d2');
      setNewTypeDefaultWorkflowId('');
    }
  };

  const handleRemoveAddedType = (typeId: string) => {
    setAddedTypes(addedTypes.filter(t => t.typeId !== typeId));
  };

  const handleToggleRemovedType = (typeId: string) => {
    if (removedTypeIds.includes(typeId)) {
      setRemovedTypeIds(removedTypeIds.filter(id => id !== typeId));
    } else {
      setRemovedTypeIds([...removedTypeIds, typeId]);
    }
  };

  // Set roles mutation
  const setRolesMutation = useMutation({
    mutationFn: async (data: { added: { roleId: string | null; name?: string; states?: string[] }[]; removed: { roleId: string }[] }) => {
      await nodesApi.setRoles(id!, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
      setEditRolesOpen(false);
    },
  });

  const handleSaveRoles = () => {
    setRolesMutation.mutate({
      added: addedRoles.map(r => ({
        roleId: r.isNew ? null : r.roleId,
        name: r.name,
        states: r.states,
      })),
      removed: removedRoleIds.map(roleId => ({ roleId })),
    });
  };

  const handleAddNewRole = () => {
    if (newRoleName.trim()) {
      const newRole = {
        roleId: `new-${Date.now()}`,
        name: newRoleName.trim(),
        states: newRoleStates,
        isNew: true,
      };
      setAddedRoles([...addedRoles, newRole]);
      setNewRoleName('');
      setNewRoleStates([]);
    }
  };

  const handleRemoveAddedRole = (roleId: string) => {
    setAddedRoles(addedRoles.filter(r => r.roleId !== roleId));
  };

  const handleToggleRemovedRole = (roleId: string) => {
    if (removedRoleIds.includes(roleId)) {
      setRemovedRoleIds(removedRoleIds.filter(id => id !== roleId));
    } else {
      setRemovedRoleIds([...removedRoleIds, roleId]);
    }
  };

  // Helper function to get state names from state IDs
  const getStateNames = (stateIds: string[]): string => {
    const names = nodeQuery.data?.inheritedWorkflows
      ?.flatMap(w => w.states || [])
      .filter(s => stateIds.includes(s.id))
      .map(s => s.name) || [];
    return names.join(', ');
  };

  // Helper function to get workflow name from ID
  const getWorkflowName = (workflowId?: string): string => {
    if (!workflowId) return '';
    const workflow = nodeQuery.data?.inheritedWorkflows?.find(w => w.id === workflowId);
    return workflow?.name || '';
  };

  // Initialize edit workflows dialog when opening
  useEffect(() => {
    if (editWorkflowsOpen && nodeQuery.data?.changedWorkflows) {
      // Initialize from changedWorkflows - load added workflows with their states
      const loadedWorkflows: WorkflowWithStates[] = (nodeQuery.data.changedWorkflows.added || []).map(w => ({
        workflowId: w.workflowId,
        name: w.name || '',
        // All states from backend (have real IDs) - marked as not new
        states: (w.states || []).map(s => ({
          stateId: s.stateId,
          name: s.name,
          isFinalSuccess: s.isFinalSuccess || false,
          isFinalFailure: s.isFinalFailure || false,
          isNew: false,
        })),
        defaultStateId: w.defaultStateId,
        defaultForTypeIds: w.defaultForTypeIds || [],
      }));
      setAddedWorkflows(loadedWorkflows);
      setRemovedWorkflowIds(nodeQuery.data.changedWorkflows.removed.map(w => w.workflowId));
    } else if (editWorkflowsOpen) {
      setAddedWorkflows([]);
      setRemovedWorkflowIds([]);
    }
    setNewWorkflowName('');
    // Reset new state form
    setNewStateName('');
    setNewStateIsFinalSuccess(false);
    setNewStateIsFinalFailure(false);
    setEditingWorkflowId(null);
  }, [editWorkflowsOpen, nodeQuery.data?.changedWorkflows]);

  // Set workflows mutation
  const setWorkflowsMutation = useMutation({
    mutationFn: async (data: { added: { workflowId: string | null; name?: string; states?: { stateId?: string | null; name: string; isFinalSuccess?: boolean; isFinalFailure?: boolean }[]; defaultStateId?: string; defaultForTypeIds?: string[] }[]; removed: { workflowId: string }[] }) => {
      await nodesApi.setWorkflows(id!, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['node', id] });
      setEditWorkflowsOpen(false);
    },
  });

  const handleSaveWorkflows = () => {
    setWorkflowsMutation.mutate({
      added: addedWorkflows.map(w => ({
        // Use null for new workflows, real ID for existing
        workflowId: w.isNew ? null : w.workflowId,
        name: w.name,
        // Convert states - include stateId for existing, null for new
        states: w.states.map(s => ({
          stateId: s.isNew ? null : s.stateId,
          name: s.name,
          isFinalSuccess: s.isFinalSuccess,
          isFinalFailure: s.isFinalFailure,
        })),
        defaultStateId: w.defaultStateId,
        defaultForTypeIds: w.defaultForTypeIds,
      })),
      removed: removedWorkflowIds.map(workflowId => ({ workflowId })),
    });
  };

  const handleAddNewWorkflow = () => {
    if (newWorkflowName.trim()) {
      const newWorkflow: WorkflowWithStates = {
        workflowId: `new-${Date.now()}`,
        name: newWorkflowName.trim(),
        states: [],
        defaultStateId: undefined,
        defaultForTypeIds: [],
        isNew: true,
      };
      setAddedWorkflows([...addedWorkflows, newWorkflow]);
      setNewWorkflowName('');
    }
  };

  const handleRemoveAddedWorkflow = (workflowId: string) => {
    setAddedWorkflows(addedWorkflows.filter(w => w.workflowId !== workflowId));
  };

  const handleToggleRemovedWorkflow = (workflowId: string) => {
    if (removedWorkflowIds.includes(workflowId)) {
      setRemovedWorkflowIds(removedWorkflowIds.filter(id => id !== workflowId));
    } else {
      setRemovedWorkflowIds([...removedWorkflowIds, workflowId]);
    }
  };

  // Handle creating a new state within a workflow
  const handleAddNewStateToWorkflow = (workflowId: string | null | undefined) => {
    if (!workflowId || !newStateName.trim()) return;
    const newState: WorkflowState = {
      stateId: `new-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      name: newStateName.trim(),
      isFinalSuccess: newStateIsFinalSuccess,
      isFinalFailure: newStateIsFinalFailure,
      isNew: true,
    };
    setAddedWorkflows(addedWorkflows.map(w => {
      if (w.workflowId === workflowId) {
        return {
          ...w,
          states: [...w.states, newState],
        };
      }
      return w;
    }));
    // Reset the form
    setNewStateName('');
    setNewStateIsFinalSuccess(false);
    setNewStateIsFinalFailure(false);
    setEditingWorkflowId(null);
  };

  // Handle removing a state from a workflow (removes from the collection)
  const handleRemoveStateFromWorkflow = (workflowId: string | null | undefined, stateId: string | null | undefined) => {
    if (!workflowId || !stateId) return;
    setAddedWorkflows(addedWorkflows.map(w => {
      if (w.workflowId === workflowId) {
        return {
          ...w,
          states: w.states.filter(s => s.stateId !== stateId),
        };
      }
      return w;
    }));
  };

  const handleSetDefaultState = (workflowId: string, stateId: string) => {
    setAddedWorkflows(addedWorkflows.map(w => {
      if (w.workflowId === workflowId) {
        return { ...w, defaultStateId: stateId };
      }
      return w;
    }));
  };

  const handleToggleDefaultForType = (workflowId: string, typeId: string) => {
    setAddedWorkflows(addedWorkflows.map(w => {
      if (w.workflowId === workflowId) {
        const isSelected = w.defaultForTypeIds.includes(typeId);
        return {
          ...w,
          defaultForTypeIds: isSelected
            ? w.defaultForTypeIds.filter(id => id !== typeId)
            : [...w.defaultForTypeIds, typeId],
        };
      }
      return w;
    }));
  };

  if (nodeQuery.isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', mt: 4 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (nodeQuery.isError) {
    return (
      <Alert severity="error">
        Failed to load node. The node may not exist.
      </Alert>
    );
  }

  const node = nodeQuery.data as Node;

  return (
    <Box>
      {/* Parent link */}
      {node.parentId && (
        <Button
          startIcon={<ArrowBackIcon />}
          onClick={handleParentClick}
          sx={{ mb: 2 }}
        >
          {parentQuery.data?.caption || `Node #${parentQuery.data?.publicId}` || 'Parent'}
        </Button>
      )}

      {/* Header: Type, State, Caption, Menu */}
      <Box sx={{ display: 'flex', alignItems: 'center', mb: 2, gap: 1 }}>
        {node.type && (
          <Chip
            label={node.type.name.toUpperCase()}
            sx={{ 
              bgcolor: node.type.color, 
              color: 'white',
              fontWeight: 'bold'
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
            fontSize: '0.9rem',
            fontWeight: 'bold',
            color: 'text.primary',
          }}
        >
          #{node.publicId}
        </Box>
        {/* State selector - for stateful nodes with workflow and states */}
        {node.type?.kind === 'Stateful' && node.workflow && node.inheritedWorkflows ? (
          (() => {
            // Find the current workflow to get available states
            const currentWorkflow = node.inheritedWorkflows?.find(w => w.id === node.workflow?.id);
            const availableStates = currentWorkflow?.states || [];
            return availableStates.length > 0 ? (
              <FormControl size="small" sx={{ minWidth: 120 }}>
                <InputLabel>State</InputLabel>
                <Select
                  value={selectedStateId || node.state?.id || ''}
                  label="State"
                  onChange={(e) => handleStateChange(e.target.value)}
                  disabled={setStateMutation.isPending}
                  renderValue={(value) => {
                    const state = availableStates.find(s => s.id === value);
                    const nodeState = node.state;
                    return (
                      <Chip
                        label={state?.name || nodeState?.name || 'Select'}
                        variant="outlined"
                        size="small"
                        color={nodeState?.isFinalSuccess ? 'success' : nodeState?.isFinalFailure ? 'error' : 'default'}
                      />
                    );
                  }}
                >
                  {availableStates.map((state) => (
                    <MenuItem key={state.id} value={state.id}>
                      {state.name}
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            ) : (
              node.state && (
                <Chip
                  label={node.state.name}
                  variant="outlined"
                  color={node.state.isFinalSuccess ? 'success' : node.state.isFinalFailure ? 'error' : 'default'}
                />
              )
            );
          })()
        ) : node.state ? (
          <Chip
            label={node.state.name}
            variant="outlined"
            color={node.state.isFinalSuccess ? 'success' : node.state.isFinalFailure ? 'error' : 'default'}
          />
        ) : null}
        {setStateMutation.isPending && <CircularProgress size={20} />}
        <Typography variant="h5" sx={{ flexGrow: 1 }}>
          {node.caption || `Node #${node.publicId}`}
        </Typography>
        <IconButton onClick={handleCopyLink} title="Copy link">
          <ContentCopyIcon />
        </IconButton>
        <IconButton onClick={(e) => setMenuAnchor(e.currentTarget)} title="More options">
          <MoreVertIcon />
        </IconButton>
        <Menu
          anchorEl={menuAnchor}
          open={menuOpen}
          onClose={() => setMenuAnchor(null)}
        >
          <MenuItem onClick={() => { setMenuAnchor(null); setEditTypesOpen(true); }}>
            Edit types
          </MenuItem>
          <MenuItem onClick={() => { setMenuAnchor(null); setEditWorkflowsOpen(true); }}>
            Edit workflows
          </MenuItem>
          <MenuItem onClick={() => { setMenuAnchor(null); setEditRolesOpen(true); }}>
            Edit roles
          </MenuItem>
          {isAdmin && (
            <MenuItem onClick={() => { setMenuAnchor(null); setEditUsersOpen(true); }}>
              Edit users
            </MenuItem>
          )}
        </Menu>
      </Box>

      <Grid container spacing={2}>
        {/* Left column: Node content */}
        <Grid item xs={12} md={8}>
          {/* Children */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Children
              </Typography>
              {childrenQuery.isLoading ? (
                <CircularProgress size={20} />
              ) : childrenQuery.data?.length === 0 ? (
                <Typography color="text.secondary">No children</Typography>
              ) : (
                <List dense>
                  {childrenQuery.data?.map((child: NodeListItem) => (
                    <ListItemButton
                      key={child.id}
                      onClick={() => handleChildClick(child.id)}
                    >
                      {child.type && (
                        <Chip
                          label={child.type.name.toUpperCase()}
                          size="small"
                          sx={{ 
                            mr: 1,
                            bgcolor: child.type.color, 
                            color: 'white',
                            fontWeight: 'bold',
                            fontSize: '0.7rem'
                          }}
                        />
                      )}
                      <Box
                        component="span"
                        sx={{
                          mr: 1,
                          px: 0.5,
                          py: 0.25,
                          bgcolor: 'grey.200',
                          borderRadius: 1,
                          fontSize: '0.75rem',
                          fontWeight: 'bold',
                          color: 'text.primary'
                        }}
                      >
                        #{child.publicId}
                      </Box>
                      <ListItemText
                        primary={child.caption || `Node #${child.publicId}`}
                        secondary={child.state?.name}
                      />
                    </ListItemButton>
                  ))}
                </List>
              )}
              
              {/* Create new child row */}
              <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, mt: 2, pt: 2, borderTop: 1, borderColor: 'divider' }}>
                <FormControl size="small" sx={{ minWidth: 120 }}>
                  <InputLabel>Type</InputLabel>
                  <Select
                    value={newChildType}
                    label="Type"
                    onChange={(e) => setNewChildType(e.target.value)}
                  >
                    {node.inheritedTypes?.map((type: InheritedType) => (
                      <MenuItem key={type.id} value={type.id}>
                        {type.name}
                      </MenuItem>
                    ))}
                  </Select>
                </FormControl>
                <TextField
                  size="small"
                  placeholder="Caption"
                  value={newChildCaption}
                  onChange={(e) => setNewChildCaption(e.target.value)}
                  sx={{ flexGrow: 1 }}
                />
                <Button
                  variant="contained"
                  size="small"
                  startIcon={<AddIcon />}
                  onClick={handleCreateChild}
                  disabled={!canCreateChild || createChildMutation.isPending}
                >
                  Create
                </Button>
              </Box>
              {createChildError && (
                <Alert severity="error" sx={{ mt: 1 }}>
                  {createChildError}
                </Alert>
              )}
            </CardContent>
          </Card>

          {/* Description */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <MarkdownField
                label="Description"
                value={description}
                onChange={setDescription}
                placeholder="No description"
                actions={
                  <Button
                    variant="contained"
                    size="small"
                    startIcon={<SaveIcon />}
                    onClick={handleSave}
                    disabled={isSaving || (description === node.description && manifest === node.manifest && summary === node.summary)}
                  >
                    {isSaving ? 'Saving...' : saveSuccess ? 'Saved!' : 'Save'}
                  </Button>
                }
              />
            </CardContent>
          </Card>

          {/* Manifest */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <MarkdownField
                label="Manifest"
                value={manifest}
                onChange={setManifest}
                placeholder="No manifest"
              />
            </CardContent>
          </Card>

          {/* Summary */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <MarkdownField
                label="Summary"
                value={summary}
                onChange={setSummary}
                placeholder="No summary"
              />
            </CardContent>
          </Card>

          {/* Assignees - with Workflow dropdown */}
          <Card sx={{ mb: 2 }}>
            <CardContent>
              <Typography variant="h6" gutterBottom>
                Assignees
              </Typography>
              
              {/* Workflow selector - only for stateful nodes with inherited workflows */}
              {node.type?.kind === 'Stateful' && node.inheritedWorkflows && node.inheritedWorkflows.length > 0 && (
                <Box sx={{ mb: 2 }}>
                  <FormControl size="small" sx={{ minWidth: 200 }}>
                    <InputLabel>Workflow</InputLabel>
                    <Select
                      value={selectedWorkflowId}
                      label="Workflow"
                      onChange={(e) => handleWorkflowChange(e.target.value)}
                      disabled={setWorkflowMutation.isPending}
                    >
                      <MenuItem value="">
                        <em>None</em>
                      </MenuItem>
                      {node.inheritedWorkflows.map((workflow: InheritedWorkflow) => (
                        <MenuItem key={workflow.id} value={workflow.id}>
                          {workflow.name}
                        </MenuItem>
                      ))}
                    </Select>
                  </FormControl>
                  {setWorkflowMutation.isPending && (
                    <CircularProgress size={20} sx={{ ml: 2, verticalAlign: 'middle' }} />
                  )}
                </Box>
              )}

              {/* Assignees table - shown when workflow is selected */}
              {node.workflow ? (
                isLoadingUsers ? (
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                    <CircularProgress size={20} />
                    <Typography color="text.secondary">Loading users...</Typography>
                  </Box>
                ) : (
                  <TableContainer>
                    <Table size="small">
                      <TableHead>
                        <TableRow>
                          <TableCell>Role</TableCell>
                          <TableCell>User</TableCell>
                        </TableRow>
                      </TableHead>
                      <TableBody>
                        {node.inheritedRoles && node.inheritedRoles.length > 0 ? (
                          node.inheritedRoles.map((role: InheritedRole) => (
                            <TableRow key={role.id}>
                              <TableCell>
                                {role.name}
                              </TableCell>
                              <TableCell>
                                <FormControl size="small" sx={{ minWidth: 150 }}>
                                  <Select
                                    value={assigneeMapping[role.id] || ''}
                                    onChange={(e) => handleAssigneeChange(role.id, e.target.value || null)}
                                    displayEmpty
                                    disabled={setAssigneesMutation.isPending}
                                  >
                                    <MenuItem value="">
                                      <em>Unassigned</em>
                                    </MenuItem>
                                    {availableUsers.map((user) => (
                                      <MenuItem key={user.id} value={user.id}>
                                        {user.name}
                                      </MenuItem>
                                    ))}
                                  </Select>
                                </FormControl>
                                {setAssigneesMutation.isPending && (
                                  <CircularProgress size={16} sx={{ ml: 1 }} />
                                )}
                              </TableCell>
                            </TableRow>
                          ))
                        ) : (
                          <TableRow>
                            <TableCell colSpan={2}>
                              <Typography color="text.secondary">
                                No roles defined. Add roles to this node to assign users.
                              </Typography>
                            </TableCell>
                          </TableRow>
                        )}
                      </TableBody>
                    </Table>
                  </TableContainer>
                )
              ) : node.type?.kind === 'Structural' ? (
                <Typography color="text.secondary">
                  Structural node: cannot set assignees
                </Typography>
              ) : (
                <Typography color="text.secondary">
                  Select a workflow to manage assignees
                </Typography>
              )}
            </CardContent>
          </Card>
        </Grid>

        {/* Right column: Comments */}
        <Grid item xs={12} md={4}>
          <Paper variant="outlined" sx={{ p: 2 }}>
            <Typography variant="h6" gutterBottom>
              Comments
            </Typography>
            
            {/* Add comment */}
            <Box sx={{ mb: 2 }}>
              <TextField
                fullWidth
                multiline
                rows={2}
                placeholder="Add a comment..."
                value={newComment}
                onChange={(e) => setNewComment(e.target.value)}
                size="small"
              />
              <Button
                variant="contained"
                size="small"
                sx={{ mt: 1 }}
                onClick={handleAddComment}
                disabled={!newComment.trim() || addCommentMutation.isPending}
              >
                Add Comment
              </Button>
            </Box>

            <Divider sx={{ my: 1 }} />

            {/* Comments list */}
            {commentsQuery.isLoading ? (
              <CircularProgress size={20} />
            ) : (
              <List dense>
                {commentsQuery.data?.map((comment: Comment) => (
                  <ListItem key={comment.id} sx={{ px: 0 }}>
                    <ListItemText
                      primary={comment.text}
                      secondary={`${comment.user?.name || 'Unknown'} - ${new Date(comment.createdAt).toLocaleString()}`}
                    />
                  </ListItem>
                ))}
                {commentsQuery.data?.length === 0 && (
                  <Typography color="text.secondary">No comments yet</Typography>
                )}
              </List>
            )}
          </Paper>
        </Grid>
      </Grid>

      {/* Edit Types Dialog */}
      <Dialog open={editTypesOpen} onClose={() => setEditTypesOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Types</DialogTitle>
        <DialogContent>
          {/* Added Types Section */}
          <Typography variant="subtitle1" sx={{ mt: 2, fontWeight: 'bold' }}>
            Added
          </Typography>
          {addedTypes.length === 0 ? (
            <Typography color="text.secondary" sx={{ mb: 2 }}>No new types added</Typography>
          ) : (
            <List dense>
              {addedTypes.map((type) => (
                <ListItem key={type.typeId} secondaryAction={
                  <IconButton edge="end" onClick={() => handleRemoveAddedType(type.typeId)}>
                    <DeleteIcon />
                  </IconButton>
                }>
                  <Chip
                    label={type.name.toUpperCase()}
                    size="small"
                    sx={{ mr: 1, bgcolor: type.color, color: 'white', fontWeight: 'bold' }}
                  />
                  <ListItemText primary={type.name} secondary={type.defaultWorkflowId ? `${type.kind} • ${getWorkflowName(type.defaultWorkflowId)}` : type.kind} />
                </ListItem>
              ))}
            </List>
          )}

          {/* Add New Type */}
          <Box sx={{ display: 'flex', gap: 1, mt: 1, mb: 2, flexWrap: 'wrap' }}>
            <TextField
              size="small"
              placeholder="New type name"
              value={newTypeName}
              onChange={(e) => setNewTypeName(e.target.value)}
              sx={{ flexGrow: 1, minWidth: 120 }}
            />
            <FormControl size="small" sx={{ minWidth: 100 }}>
              <InputLabel>Kind</InputLabel>
              <Select
                value={newTypeKind}
                label="Kind"
                onChange={(e) => {
                  setNewTypeKind(e.target.value as 'Structural' | 'Stateful');
                  if (e.target.value === 'Structural') {
                    setNewTypeDefaultWorkflowId('');
                  }
                }}
              >
                <MenuItem value="Structural">Structural</MenuItem>
                <MenuItem value="Stateful">Stateful</MenuItem>
              </Select>
            </FormControl>
            <TextField
              size="small"
              type="color"
              value={newTypeColor}
              onChange={(e) => setNewTypeColor(e.target.value)}
              sx={{ width: 50 }}
            />
            <FormControl size="small" sx={{ minWidth: 150 }} disabled={newTypeKind === 'Structural'}>
              <InputLabel>Default Workflow</InputLabel>
              <Select
                value={newTypeDefaultWorkflowId}
                label="Default Workflow"
                onChange={(e) => setNewTypeDefaultWorkflowId(e.target.value)}
              >
                <MenuItem value=""><em>None</em></MenuItem>
                {node.inheritedWorkflows?.map((workflow: InheritedWorkflow) => (
                  <MenuItem key={workflow.id} value={workflow.id}>
                    {workflow.name}
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            <Button variant="contained" size="small" onClick={handleAddNewType} disabled={!newTypeName.trim()}>
              Add
            </Button>
          </Box>

          <Divider sx={{ my: 2 }} />

          {/* Disabled (Removed) Types Section */}
          <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
            Disabled
          </Typography>
          {node.inheritedTypes && node.inheritedTypes.length > 0 ? (
            <List dense>
              {node.inheritedTypes.map((type: InheritedType) => (
                <ListItem key={type.id}>
                  <ListItemIcon>
                    <Checkbox
                      checked={removedTypeIds.includes(type.id)}
                      onChange={() => handleToggleRemovedType(type.id)}
                    />
                  </ListItemIcon>
                  <Chip
                    label={type.name.toUpperCase()}
                    size="small"
                    sx={{ mr: 1, bgcolor: type.color, color: 'white', fontWeight: 'bold' }}
                  />
                  <ListItemText primary={type.name} secondary={type.defaultWorkflowId ? getWorkflowName(type.defaultWorkflowId) : undefined} />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="text.secondary">No inherited types available</Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditTypesOpen(false)}>Cancel</Button>
          <Button 
            onClick={handleSaveTypes} 
            variant="contained"
            disabled={setTypesMutation.isPending}
          >
            {setTypesMutation.isPending ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Roles Dialog */}
      <Dialog open={editRolesOpen} onClose={() => setEditRolesOpen(false)} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Roles</DialogTitle>
        <DialogContent>
          {/* Added Roles Section */}
          <Typography variant="subtitle1" sx={{ mt: 2, fontWeight: 'bold' }}>
            Added
          </Typography>
          {addedRoles.length === 0 ? (
            <Typography color="text.secondary" sx={{ mb: 2 }}>No new roles added</Typography>
          ) : (
            <List dense>
              {addedRoles.map((role) => (
                <ListItem key={role.roleId} secondaryAction={
                  <IconButton edge="end" onClick={() => handleRemoveAddedRole(role.roleId)}>
                    <DeleteIcon />
                  </IconButton>
                }>
                  <ListItemText 
                    primary={role.name} 
                    secondary={role.states.length > 0 ? `States: ${getStateNames(role.states)}` : 'No states'} 
                  />
                </ListItem>
              ))}
            </List>
          )}

          {/* Add New Role */}
          <Box sx={{ display: 'flex', gap: 1, mt: 1, mb: 2, flexWrap: 'wrap' }}>
            <TextField
              size="small"
              placeholder="New role name"
              value={newRoleName}
              onChange={(e) => setNewRoleName(e.target.value)}
              sx={{ flexGrow: 1, minWidth: 120 }}
            />
            <FormControl size="small" sx={{ minWidth: 150 }}>
              <InputLabel>States</InputLabel>
              <Select
                multiple
                value={newRoleStates}
                label="States"
                onChange={(e) => setNewRoleStates(e.target.value as string[])}
                renderValue={(selected) => {
                  // Find state names for selected IDs
                  const selectedNames = node.inheritedWorkflows
                    ?.flatMap(w => w.states || [])
                    .filter(s => selected.includes(s.id))
                    .map(s => s.name) || [];
                  return selectedNames.join(', ');
                }}
              >
                {node.inheritedWorkflows?.flatMap((workflow: InheritedWorkflow) => 
                  (workflow.states || []).map((state: InheritedState) => (
                    <MenuItem key={state.id} value={state.id}>
                      <Checkbox checked={newRoleStates.includes(state.id)} />
                      <ListItemText primary={`${state.name} (${workflow.name})`} />
                    </MenuItem>
                  ))
                )}
              </Select>
            </FormControl>
            <Button variant="contained" size="small" onClick={handleAddNewRole} disabled={!newRoleName.trim()}>
              Add
            </Button>
          </Box>

          <Divider sx={{ my: 2 }} />

          {/* Disabled (Removed) Roles Section */}
          <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
            Disabled
          </Typography>
          {node.inheritedRoles && node.inheritedRoles.length > 0 ? (
            <List dense>
              {node.inheritedRoles.map((role: InheritedRole) => (
                <ListItem key={role.id}>
                  <ListItemIcon>
                    <Checkbox
                      checked={removedRoleIds.includes(role.id)}
                      onChange={() => handleToggleRemovedRole(role.id)}
                    />
                  </ListItemIcon>
                  <ListItemText 
                    primary={role.name} 
                    secondary={role.states && role.states.length > 0 ? `States: ${role.states.map(s => s.name).join(', ')}` : 'No states'} 
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="text.secondary">No inherited roles available</Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditRolesOpen(false)}>Cancel</Button>
          <Button 
            onClick={handleSaveRoles} 
            variant="contained"
            disabled={setRolesMutation.isPending}
          >
            {setRolesMutation.isPending ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Workflows Dialog */}
      <Dialog open={editWorkflowsOpen} onClose={() => setEditWorkflowsOpen(false)} maxWidth="md" fullWidth>
        <DialogTitle>Edit Workflows</DialogTitle>
        <DialogContent>
          {/* Added Workflows Section */}
          <Typography variant="subtitle1" sx={{ mt: 2, fontWeight: 'bold' }}>
            Added
          </Typography>
          {addedWorkflows.length === 0 ? (
            <Typography color="text.secondary" sx={{ mb: 2 }}>No new workflows added</Typography>
          ) : (
            <Box sx={{ mb: 2 }}>
              {addedWorkflows.map((workflow) => (
                <Card key={workflow.workflowId} variant="outlined" sx={{ mb: 2, p: 2 }}>
                  <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', mb: 1 }}>
                    <Typography variant="subtitle2" fontWeight="bold">{workflow.name}</Typography>
                    <IconButton edge="end" onClick={() => workflow.workflowId && handleRemoveAddedWorkflow(workflow.workflowId)} size="small">
                      <DeleteIcon />
                    </IconButton>
                  </Box>
                  
                  {/* States for this workflow */}
                  <Typography variant="caption" fontWeight="bold">States:</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1, ml: 1 }}>
                    {workflow.states.map((state) => (
                      <Chip 
                        key={state.stateId} 
                        label={state.name} 
                        size="small"
                        color={state.isNew ? 'primary' : 'default'}
                        icon={
                          state.isFinalSuccess ? <CheckBoxIcon color="success" /> : 
                          state.isFinalFailure ? <CheckBoxIcon color="error" /> : undefined
                        }
                        onDelete={() => {
                          const wfId = String(workflow.workflowId);
                          const stId = String(state.stateId);
                          if (wfId && stId) {
                            handleRemoveStateFromWorkflow(wfId, stId);
                          }
                        }}
                        deleteIcon={<DeleteIcon />}
                      />
                    ))}
                    {workflow.states.length === 0 && (
                      <Typography variant="caption" color="text.secondary">No states added</Typography>
                    )}
                  </Box>

                  {/* Add new state form - inline within workflow */}
                  {editingWorkflowId === workflow.workflowId ? (
                    <Box sx={{ display: 'flex', gap: 1, mb: 1, alignItems: 'center', flexWrap: 'wrap', ml: 1, p: 1, bgcolor: 'grey.50', borderRadius: 1 }}>
                      <TextField
                        size="small"
                        placeholder="State name"
                        value={newStateName}
                        onChange={(e) => setNewStateName(e.target.value)}
                        sx={{ minWidth: 120 }}
                      />
                      <FormControl size="small" sx={{ minWidth: 120 }}>
                        <InputLabel>Final Status</InputLabel>
                        <Select
                          value={newStateIsFinalSuccess ? 'success' : newStateIsFinalFailure ? 'failure' : 'none'}
                          label="Final Status"
                          onChange={(e) => {
                            if (e.target.value === 'success') {
                              setNewStateIsFinalSuccess(true);
                              setNewStateIsFinalFailure(false);
                            } else if (e.target.value === 'failure') {
                              setNewStateIsFinalSuccess(false);
                              setNewStateIsFinalFailure(true);
                            } else {
                              setNewStateIsFinalSuccess(false);
                              setNewStateIsFinalFailure(false);
                            }
                          }}
                        >
                          <MenuItem value="none">None</MenuItem>
                          <MenuItem value="success">Success</MenuItem>
                          <MenuItem value="failure">Failure</MenuItem>
                        </Select>
                      </FormControl>
                      <Button 
                        variant="contained" 
                        size="small" 
                        onClick={() => handleAddNewStateToWorkflow(workflow.workflowId)}
                        disabled={!newStateName.trim()}
                      >
                        Add
                      </Button>
                      <Button 
                        size="small" 
                        onClick={() => {
                          setEditingWorkflowId(null);
                          setNewStateName('');
                          setNewStateIsFinalSuccess(false);
                          setNewStateIsFinalFailure(false);
                        }}
                      >
                        Cancel
                      </Button>
                    </Box>
                  ) : (
                    <Button 
                      size="small" 
                      variant="outlined"
                      startIcon={<AddIcon />}
                        onClick={() => workflow.workflowId && setEditingWorkflowId(workflow.workflowId)}
                      sx={{ ml: 1, mb: 1 }}
                    >
                      Add State
                    </Button>
                  )}

                  {/* Default State - selection */}
                  <Typography variant="caption" fontWeight="bold" sx={{ display: 'block', mt: 1 }}>Default State:</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, mb: 1, ml: 1 }}>
                    {workflow.states.length > 0 ? workflow.states.map((state) => (
                      <Chip 
                        key={state.stateId} 
                        label={state.name} 
                        size="small"
                        variant={workflow.defaultStateId === state.stateId ? 'filled' : 'outlined'}
                        color={workflow.defaultStateId === state.stateId ? 'primary' : 'default'}
                        onClick={() => state.stateId && workflow.workflowId && handleSetDefaultState(workflow.workflowId, state.stateId)}
                        sx={{ cursor: 'pointer' }}
                      />
                    )) : (
                      <Typography variant="caption" color="text.secondary">Add states to set default</Typography>
                    )}
                  </Box>

                  {/* Default For Types - Multiple selection */}
                  <Typography variant="caption" fontWeight="bold" sx={{ display: 'block' }}>Default For Types:</Typography>
                  <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, ml: 1 }}>
                    {node.inheritedTypes?.map((type: InheritedType) => (
                      <Chip 
                        key={type.id} 
                        label={type.name} 
                        size="small"
                        variant={workflow.defaultForTypeIds.includes(type.id) ? 'filled' : 'outlined'}
                        onClick={() => workflow.workflowId && handleToggleDefaultForType(workflow.workflowId, type.id)}
                        sx={{ cursor: 'pointer' }}
                      />
                    ))}
                  </Box>
                </Card>
              ))}
            </Box>
          )}

          {/* Add New Workflow - simplified, no states initially */}
          <Box sx={{ display: 'flex', gap: 1, mb: 2 }}>
            <TextField
              size="small"
              placeholder="New workflow name"
              value={newWorkflowName}
              onChange={(e) => setNewWorkflowName(e.target.value)}
              sx={{ flexGrow: 1, minWidth: 150 }}
            />
            <Button variant="contained" size="small" onClick={handleAddNewWorkflow} disabled={!newWorkflowName.trim()}>
              Add
            </Button>
          </Box>

          <Divider sx={{ my: 2 }} />

          {/* Disabled (Removed) Workflows Section */}
          <Typography variant="subtitle1" sx={{ fontWeight: 'bold' }}>
            Disabled
          </Typography>
          {node.inheritedWorkflows && node.inheritedWorkflows.length > 0 ? (
            <List dense>
              {node.inheritedWorkflows.map((workflow: InheritedWorkflow) => (
                <ListItem key={workflow.id}>
                  <ListItemIcon>
                    <Checkbox
                      checked={removedWorkflowIds.includes(workflow.id)}
                      onChange={() => handleToggleRemovedWorkflow(workflow.id)}
                    />
                  </ListItemIcon>
                  <ListItemText 
                    primary={workflow.name} 
                    secondary={workflow.states?.map(s => s.name).join(', ') || 'No states'} 
                  />
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="text.secondary">No inherited workflows available</Typography>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setEditWorkflowsOpen(false)}>Cancel</Button>
          <Button 
            onClick={handleSaveWorkflows} 
            variant="contained"
            disabled={setWorkflowsMutation.isPending}
          >
            {setWorkflowsMutation.isPending ? 'Saving...' : 'Save'}
          </Button>
        </DialogActions>
      </Dialog>

      {/* Edit Users Dialog */}
      <EditUsersDialog
        open={editUsersOpen}
        onClose={() => {
          setEditUsersOpen(false);
        }}
      />
    </Box>
  );
}

// Edit Users Dialog Component
interface EditUsersDialogProps {
  open: boolean;
  onClose: () => void;
}

function EditUsersDialog({ open, onClose }: EditUsersDialogProps) {
  const queryClient = useQueryClient();
  const [newUserName, setNewUserName] = useState('');
  const [newUserSubject, setNewUserSubject] = useState('');
  const [deleteConfirmOpen, setDeleteConfirmOpen] = useState(false);
  const [userToDelete, setUserToDelete] = useState<string | null>(null);

  // Fetch users
  const usersQuery = useQuery({
    queryKey: ['users'],
    queryFn: () => usersApi.list(),
    enabled: open,
  });

  // Create user mutation
  const createUserMutation = useMutation({
    mutationFn: (data: { name: string; subject: string }) => usersApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setNewUserName('');
      setNewUserSubject('');
    },
  });

  // Delete user mutation
  const deleteUserMutation = useMutation({
    mutationFn: (id: string) => usersApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      setDeleteConfirmOpen(false);
      setUserToDelete(null);
    },
  });

  const handleAddUser = () => {
    if (newUserName.trim() && newUserSubject.trim()) {
      createUserMutation.mutate({
        name: newUserName.trim(),
        subject: newUserSubject.trim(),
      });
    }
  };

  const handleDeleteUser = (userId: string) => {
    setUserToDelete(userId);
    setDeleteConfirmOpen(true);
  };

  const confirmDeleteUser = () => {
    if (userToDelete) {
      deleteUserMutation.mutate(userToDelete);
    }
  };

  const canAddUser = newUserName.trim() && newUserSubject.trim();

  return (
    <>
      <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth>
        <DialogTitle>Edit Users</DialogTitle>
        <DialogContent>
          {/* Users List */}
          <Typography variant="subtitle1" sx={{ mt: 2, fontWeight: 'bold' }}>
            Users
          </Typography>
          {usersQuery.isLoading ? (
            <Box sx={{ display: 'flex', justifyContent: 'center', py: 2 }}>
              <CircularProgress size={24} />
            </Box>
          ) : usersQuery.data && usersQuery.data.length > 0 ? (
            <List dense>
              {usersQuery.data.map((user) => (
                <ListItem
                  key={user.id}
                  secondaryAction={
                    <IconButton
                      edge="end"
                      onClick={() => handleDeleteUser(user.id)}
                      color="error"
                    >
                      <DeleteIcon />
                    </IconButton>
                  }
                >
                  <ListItemText
                    primary={user.name}
                    secondary={`Subject: ${user.subject || 'N/A'}`}
                  />
                  {user.roles && user.roles.length > 0 && (
                    <Box sx={{ ml: 1 }}>
                      {user.roles.map((role) => (
                        <Chip
                          key={role.id}
                          label={role.name}
                          size="small"
                          sx={{ ml: 0.5 }}
                          color={role.name.toLowerCase().includes('admin') ? 'error' : 'default'}
                        />
                      ))}
                    </Box>
                  )}
                </ListItem>
              ))}
            </List>
          ) : (
            <Typography color="text.secondary" sx={{ py: 2 }}>
              No users found
            </Typography>
          )}

          {/* Add New User */}
          <Typography variant="subtitle1" sx={{ mt: 3, fontWeight: 'bold' }}>
            Add New User
          </Typography>
          <Box sx={{ display: 'flex', gap: 1, mt: 1 }}>
            <TextField
              size="small"
              placeholder="Name"
              value={newUserName}
              onChange={(e) => setNewUserName(e.target.value)}
              sx={{ flexGrow: 1 }}
            />
            <TextField
              size="small"
              placeholder="Subject (OAuth)"
              value={newUserSubject}
              onChange={(e) => setNewUserSubject(e.target.value)}
              sx={{ flexGrow: 1 }}
            />
            <Button
              variant="contained"
              size="small"
              onClick={handleAddUser}
              disabled={!canAddUser || createUserMutation.isPending}
            >
              Add
            </Button>
          </Box>
          {createUserMutation.isError && (
            <Alert severity="error" sx={{ mt: 1 }}>
              Failed to create user
            </Alert>
          )}
        </DialogContent>
        <DialogActions>
          <Button onClick={onClose}>Close</Button>
        </DialogActions>
      </Dialog>

      {/* Delete Confirmation Dialog */}
      <Dialog open={deleteConfirmOpen} onClose={() => setDeleteConfirmOpen(false)}>
        <DialogTitle>Delete User</DialogTitle>
        <DialogContent>
          <Typography>
            Are you sure you want to delete this user? This action cannot be undone.
          </Typography>
        </DialogContent>
        <DialogActions>
          <Button onClick={() => setDeleteConfirmOpen(false)}>Cancel</Button>
          <Button
            onClick={confirmDeleteUser}
            color="error"
            variant="contained"
            disabled={deleteUserMutation.isPending}
          >
            {deleteUserMutation.isPending ? 'Deleting...' : 'Delete'}
          </Button>
        </DialogActions>
      </Dialog>
    </>
  );
}

export default NodeDetailPage;

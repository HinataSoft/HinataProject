// Node Types
export interface NodeType {
  id: string;
  name: string;
  kind: 'Structural' | 'Stateful';
  color: string;
  defaultWorkflowId?: string;
}

export interface NodeWorkflow {
  id: string;
  name: string;
}

export interface NodeState {
  id: string;
  name: string;
  isFinalSuccess: boolean;
  isFinalFailure: boolean;
}

export interface InheritedType {
  id: string;
  name: string;
  kind: 'Structural' | 'Stateful';
  color: string;
  defaultWorkflowId?: string;
}

export interface InheritedState {
  id: string;
  name: string;
}

export interface InheritedWorkflow {
  id: string;
  name: string;
  states?: InheritedState[];
}

export interface InheritedRole {
  id: string;
  name: string;
  rights?: Rights;
  states?: InheritedState[];
}

export interface ChangedTypes {
  added: { typeId: string; name?: string; kind?: string; color?: string; defaultWorkflowId?: string }[];
  removed: { typeId: string }[];
}

export interface ChangedWorkflows {
  added: { workflowId: string | null; name?: string; states?: { stateId: string | null; name: string; isFinalSuccess?: boolean; isFinalFailure?: boolean }[]; defaultStateId?: string; defaultForTypeIds?: string[] }[];
  removed: { workflowId: string }[];
}

// For Edit Workflows dialog - state that can be either existing (from backend) or new
export interface WorkflowState {
  stateId: string | null;  // Real ID from backend, null for new states
  name: string;
  isFinalSuccess: boolean;
  isFinalFailure: boolean;
  isNew: boolean;   // true if this is a new state (tempId), false if from backend
}

// For Edit Workflows dialog - manages workflow with its states
export interface WorkflowWithStates {
  workflowId: string | null;  // Real ID from backend, null for new workflows
  name: string;
  states: WorkflowState[];  // All states - both existing and new
  defaultStateId?: string;  // Can be either stateId (for existing states) or tempId (for new states)
  defaultForTypeIds: string[];
  isNew?: boolean;  // true if this is a new workflow (tempId), false if from backend
}

export interface ChangedRoles {
  added: { roleId: string; name?: string; rights?: string; states?: { id: string; name: string }[] }[];
  removed: { roleId: string }[];
}

export interface Node {
  id: string;
  publicId: number;
  parentId: string | null;
  manifest: string;
  caption: string;
  description: string;
  summary: string;
  type?: NodeType | null;
  workflow?: NodeWorkflow | null;
  state?: NodeState | null;
  inheritedTypes?: InheritedType[];
  inheritedWorkflows?: InheritedWorkflow[];
  inheritedRoles?: InheritedRole[];
  changedTypes?: ChangedTypes | null;
  changedWorkflows?: ChangedWorkflows | null;
  changedRoles?: ChangedRoles | null;
  assignee?: { id: string; name: string } | null;
}

// Support both PascalCase (from children endpoint) and camelCase (from root endpoint)
export interface NodeListItem {
  id: string;
  publicId: number;
  caption: string;
  type?: { id: string; name: string; color?: string } | null;
  state?: { id: string; name: string } | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
}

// Comment Types
export interface CommentUser {
  id: string;
  name: string;
}

export interface Comment {
  id: string;
  text: string;
  user?: CommentUser | null;
  createdAt: string;
}

// User Types
export type Rights = 'Admin' | 'Active' | 'Passive';

export interface User {
  id: string;
  name: string;
  subject?: string;
  rights: Rights;
  roles?: { id: string; name: string }[];
}

// Assignee Types - mapping from Role to User
export interface AssigneeMapping {
  [roleId: string]: string | null;  // roleId -> userId (or null if not assigned)
}

// For displaying assignees with role and user details
export interface AssigneeDisplay {
  roleId: string;
  roleName: string;
  userId: string | null;
  userName: string | null;
}

// DTO Types
export interface CreateNodeDto {
  ParentId: string;
  TypeId: string;
  Manifest?: string;
  Caption?: string;
  Description?: string;
  Summary?: string;
  WorkflowId?: string;
}

export interface UpdateNodeDto {
  manifest?: string;
  caption?: string;
  description?: string;
  summary?: string;
}

export interface AddCommentDto {
  text: string;
}

// For setting assignees - array of [roleId, userId] pairs
export interface SetAssigneesDto {
  assignees: [string, string][];  // Array of [roleId, userId] tuples
}

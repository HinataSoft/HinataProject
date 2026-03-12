import client from './client';
import { Node, NodeListItem, CreateNodeDto, UpdateNodeDto } from '../types';

// Helper to normalize PascalCase response to camelCase
const normalizeNodeListItem = (item: any): NodeListItem => ({
  id: item.Id || item.id,
  publicId: item.PublicId || item.publicId,
  caption: item.Caption || item.caption,
  type: item.Type || item.type,
  state: item.State || item.state,
});

const normalizeNode = (node: any): Node => ({
  id: node.Id || node.id,
  publicId: node.PublicId || node.publicId,
  parentId: node.ParentId || node.parentId,
  manifest: node.Manifest || node.manifest,
  caption: node.Caption || node.caption,
  description: node.Description || node.description,
  summary: node.Summary || node.summary,
  type: node.Type || node.type,
  workflow: node.Workflow || node.workflow,
  state: node.State || node.state,
  inheritedTypes: node.InheritedTypes || node.inheritedTypes,
  inheritedWorkflows: node.InheritedWorkflows || node.inheritedWorkflows,
  inheritedRoles: node.InheritedRoles || node.inheritedRoles,
  changedTypes: node.ChangedTypes || node.changedTypes,
  changedWorkflows: node.ChangedWorkflows || node.changedWorkflows,
  changedRoles: node.ChangedRoles || node.changedRoles,
  assignee: node.Assignee || node.assignee,
});

export const nodesApi = {
  getRoot: () => client.get<any>('/nodes/root').then(r => normalizeNode(r.data)),

  getById: (id: string) => {
    // Handle "root" keyword specially
    if (id === 'root') {
      return client.get<any>('/nodes/root').then(r => normalizeNode(r.data));
    }
    return client.get<any>(`/nodes/${id}`).then(r => normalizeNode(r.data));
  },

  getChildren: async (id: string, skip = 0, take = 20) => {
    let nodeId = id;
    
    // If id is "root", first get the root node to find its actual ID
    if (id === 'root') {
      const rootNode = await client.get<any>('/nodes/root').then(r => r.data);
      nodeId = rootNode.Id || rootNode.id;
    }
    
    return client.get<any>(`/nodes/${nodeId}/children`, {
      params: { skip, take },
    }).then(r => ({
      items: (r.data.items || r.data.Items || []).map(normalizeNodeListItem),
      totalCount: r.data.totalCount || r.data.TotalCount || 0,
    }));
  },

  getAssignedToMe: (skip = 0, take = 20) =>
    client.get<any>('/nodes/assigned-to-me', {
      params: { skip, take },
    }).then(r => ({
      items: (r.data.items || r.data.Items || []).map(normalizeNodeListItem),
      totalCount: r.data.totalCount || r.data.TotalCount || 0,
    })),

  create: (data: CreateNodeDto) => client.post<{ id: string }>('/nodes', data),

  update: (id: string, data: UpdateNodeDto) => {
    // Send as camelCase - backend is configured to handle it
    return client.put(`/nodes/${id}`, data);
  },

  delete: (id: string) => client.delete(`/nodes/${id}`),

  setTypes: (id: string, data: object) => client.put(`/nodes/${id}/types`, data),

  setWorkflows: (id: string, data: object) => client.put(`/nodes/${id}/workflows`, data),

  setRoles: (id: string, data: object) => client.put(`/nodes/${id}/roles`, data),

  setState: (id: string, data: { targetStateId: string }) =>
    client.put(`/nodes/${id}/state`, data),

  setWorkflow: (id: string, data: { workflowId: string }) =>
    client.put(`/nodes/${id}/workflow`, data),

  getAssignees: (id: string) => client.get<Record<string, string>>(`/nodes/${id}/assignees`),

  setAssignees: (id: string, data: Record<string, string>) =>
    client.put(`/nodes/${id}/assignees`, { assignees: data }),
};

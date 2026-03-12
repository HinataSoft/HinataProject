import client from './client';
import { Comment, AddCommentDto } from '../types';

// Helper to normalize PascalCase response to camelCase
const normalizeComment = (comment: any): Comment => ({
  id: comment.Id || comment.id,
  text: comment.Text || comment.text,
  user: comment.User || comment.user,
  createdAt: comment.CreatedAt || comment.createdAt,
});

export const commentsApi = {
  list: (nodeId: string, skip = 0, take = 20, ordering: 'asc' | 'desc' = 'asc') =>
    client.get<any>(`/nodes/${nodeId}/comments`, {
      params: { skip, take, ordering },
    }).then(r => ({
      items: (r.data.items || r.data.Items || []).map(normalizeComment),
      totalCount: r.data.totalCount || r.data.TotalCount || 0,
    })),

  add: (nodeId: string, data: AddCommentDto) =>
    client.post<{ id: string }>(`/nodes/${nodeId}/comments`, data),
};

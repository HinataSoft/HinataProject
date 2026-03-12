import client from './client';

export type Rights = 'Admin' | 'Active' | 'Passive';

export interface User {
  id: string;
  name: string;
  subject?: string;
  rights: Rights;
  roles?: { id: string; name: string }[];
}

export interface CreateUserDto {
  name: string;
  subject: string;
  info?: string;
  rights?: Rights;
}

export interface UpdateUserDto {
  name?: string;
  subject?: string;
  info?: string;
  rights?: Rights;
}

export const usersApi = {
  list: () => client.get<User[]>('/users').then(r => r.data),
  getById: (id: string) => client.get<User>(`/users/${id}`).then(r => r.data),
  getCurrentUser: () => client.get<User>('/users/me').then(r => r.data),
  create: (data: CreateUserDto) => client.post<User>('/users', data).then(r => r.data),
  update: (id: string, data: UpdateUserDto) => client.put<User>(`/users/${id}`, data).then(r => r.data),
  delete: (id: string) => client.delete(`/users/${id}`),
};

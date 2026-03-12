import client from './client';

export interface OAuthSettings {
  authorizationEndpoint: string;
  tokenEndpoint: string;
  clientId: string;
  redirectUri: string;
  scope: string;
}

export const oauthApi = {
  getSettings: () => client.get<OAuthSettings>('/oauth/settings').then(r => r.data),
};

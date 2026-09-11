import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AiDraftResponse {
  title: string;
  description: string;
  checklist: string[];
  labels: string[];
  priority: string;
  issueType: string;
  storyPoints?: number;
  provider: string;
  model: string;
  rawJson: string;
}

export interface AiEnhanceResponse {
  title: string;
  description: string;
  provider: string;
  model: string;
  rawJson: string;
}

export interface AiCriteriaResponse {
  criteria: string[];
  provider: string;
  model: string;
  rawJson: string;
}

export interface AiBreakdownResponse {
  subtasks: string[];
  provider: string;
  model: string;
  rawJson: string;
}

@Injectable({ providedIn: 'root' })
export class AiService {
  private http = inject(HttpClient);

  draft(prompt: string, model: string, projectId?: string) {
    return this.http.post<AiDraftResponse>(`${environment.apiUrl}/api/ai/draft`, { prompt, model, projectId }, { withCredentials: true });
  }
  enhance(taskId: string | undefined, title: string, description: string, model: string, projectId?: string) {
    return this.http.post<AiEnhanceResponse>(`${environment.apiUrl}/api/ai/enhance`, { taskId, title, description, model, projectId }, { withCredentials: true });
  }
  criteria(taskId: string | undefined, title: string, description: string, model: string, projectId?: string) {
    return this.http.post<AiCriteriaResponse>(`${environment.apiUrl}/api/ai/criteria`, { taskId, title, description, model, projectId }, { withCredentials: true });
  }
  breakdown(taskId: string | undefined, title: string, description: string, model: string, projectId?: string) {
    return this.http.post<AiBreakdownResponse>(`${environment.apiUrl}/api/ai/breakdown`, { taskId, title, description, model, projectId }, { withCredentials: true });
  }
  usage(orgId?: string, projectId?: string) {
    let params: any = {};
    if (orgId) params.orgId = orgId;
    if (projectId) params.projectId = projectId;
    return this.http.get<any[]>(`${environment.apiUrl}/api/ai/usage`, { params, withCredentials: true });
  }
  usageSummary(orgId?: string, projectId?: string) {
    let params: any = {};
    if (orgId) params.orgId = orgId;
    if (projectId) params.projectId = projectId;
    return this.http.get<any[]>(`${environment.apiUrl}/api/ai/usage/summary`, { params, withCredentials: true });
  }
}

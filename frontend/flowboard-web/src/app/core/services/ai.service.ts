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

@Injectable({ providedIn: 'root' })
export class AiService {
  private http = inject(HttpClient);

  draft(prompt: string, model: string, projectId?: string) {
    return this.http.post<AiDraftResponse>(`${environment.apiUrl}/api/ai/draft`, { prompt, model, projectId }, { withCredentials: true });
  }
  enhance(taskId: string | undefined, title: string, description: string, model: string, projectId?: string) {
    return this.http.post<AiEnhanceResponse>(`${environment.apiUrl}/api/ai/enhance`, { taskId, title, description, model, projectId }, { withCredentials: true });
  }
}

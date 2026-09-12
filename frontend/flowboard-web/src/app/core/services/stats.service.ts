import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface OrgStats { totalWorkspaces: number; totalProjects: number; totalMembers: number; totalIssues: number; activeSprints: number; completedIssues: number; }
export interface ChartBucket { label: string; value: number; }
export interface DailyCount { date: string; count: number; }
export interface AiDaily { date: string; tokens: number; cost: number; requests: number; }
export interface OrgChartData {
  issuesByStatus: ChartBucket[];
  issuesByPriority: ChartBucket[];
  issuesByWorkspace: ChartBucket[];
  activityTrend: DailyCount[];
  aiUsage: AiDaily[];
}

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  getOrgStats(orgId: string) {
    return this.http.get<OrgStats>(`${environment.apiUrl}/api/organizations/${orgId}/stats`, { withCredentials: true });
  }
  getOrgChartData(orgId: string) {
    return this.http.get<OrgChartData>(`${environment.apiUrl}/api/organizations/${orgId}/chart-data`, { withCredentials: true });
  }
}

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
export interface ProjectStats { totalIssues: number; completedIssues: number; inProgressIssues: number; totalStoryPoints: number; activeSprints: number; activeSprintName: string | null; }
export interface ProjectChartData {
  issuesByStatus: ChartBucket[];
  issuesByType: ChartBucket[];
  issuesByPriority: ChartBucket[];
  assigneeWorkload: { assigneeName: string; issueCount: number }[];
  sprintVelocity: { sprintName: string; storyPoints: number; completedPoints: number }[];
}
export interface BurndownPoint { date: string; total: number; remaining: number; ideal: number; }
export interface BurndownData { sprintId: string; sprintName: string; startDate: string; endDate: string; points: BurndownPoint[]; }

@Injectable({ providedIn: 'root' })
export class StatsService {
  private http = inject(HttpClient);
  getOrgStats(orgId: string) {
    return this.http.get<OrgStats>(`${environment.apiUrl}/api/organizations/${orgId}/stats`, { withCredentials: true });
  }
  getOrgChartData(orgId: string) {
    return this.http.get<OrgChartData>(`${environment.apiUrl}/api/organizations/${orgId}/chart-data`, { withCredentials: true });
  }
  getProjectStats(projectId: string) {
    return this.http.get<ProjectStats>(`${environment.apiUrl}/api/projects/${projectId}/stats`, { withCredentials: true });
  }
  getProjectChartData(projectId: string) {
    return this.http.get<ProjectChartData>(`${environment.apiUrl}/api/projects/${projectId}/chart-data`, { withCredentials: true });
  }
  getBurndown(projectId: string, sprintId?: string) {
    let params: any = {};
    if (sprintId) params.sprintId = sprintId;
    return this.http.get<BurndownData>(`${environment.apiUrl}/api/projects/${projectId}/burndown`, { params, withCredentials: true });
  }
}

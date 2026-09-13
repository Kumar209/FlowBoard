import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface SuperAdminKpis {
  totalOrganizations: number;
  totalUsers: number;
  activeUsers: number;
  totalWorkspaces: number;
  totalProjects: number;
  storageUsedGb: number;
}

export interface ServiceHealth {
  name: string;
  status: string;
  latencyMs: number;
  uptime: string;
}

export interface GrowthChart {
  labels: string[];
  organizations: number[];
  users: number[];
  workspaces: number[];
  projects: number[];
  storage: number[];
  revenue: number[];
}

export interface SuperAdminDashboard {
  kpis: SuperAdminKpis;
  health: ServiceHealth[];
  growth: GrowthChart;
}

export interface SuperAdminOrgRow {
  id: string;
  name: string;
  slug: string;
  ownerId: string;
  ownerName: string;
  ownerEmail: string;
  planId: string;
  planName: string;
  isActive: boolean;
  users: number;
  workspaces: number;
  projects: number;
  createdAt: string;
}
export interface SuperAdminOrgsResponse {
  items: SuperAdminOrgRow[];
  total: number;
}
export interface SuperAdminUserRow {
  id: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isPendingSuspension: boolean;
  pendingDeadline?: string | null;
  pendingReason?: string | null;
  orgCount: number;
  organizationId: string;
  organizationName: string;
  orgRole: string;
  createdAt: string;
  lastLoginAt?: string | null;
}
export interface SuperAdminUsersResponse {
  items: SuperAdminUserRow[];
  total: number;
}
export interface OrgMemberRow {
  userId: string;
  fullName: string;
  email: string;
  orgRole: string;
  isActive: boolean;
  joinedAt: string;
}
export interface OrgMembersResponse {
  items: OrgMemberRow[];
  total: number;
}
export interface SubscriptionOverview {
  mrr: number;
  arr: number;
  activeSubscriptions: number;
  churnRate: number;
  avgRevenuePerOrg: number;
}
export interface SubscriptionRow {
  organizationId: string;
  organizationName: string;
  planName: string;
  billingCycle: string;
  status: string;
  amount: number;
  renewalAt: string;
}
export interface PlanConfig {
  id: string;
  name: string;
  price: number;
  maxUsers: number;
  maxWorkspaces: number;
  maxProjects: number;
  storageGB: number;
  aiRequests: number;
  apiLimit: number;
  featuresJson: string;
}
export interface SuperAdminSubscriptionsResponse {
  overview: SubscriptionOverview;
  subscriptions: SubscriptionRow[];
  plans: PlanConfig[];
  revenueHistory: number[];
}
export interface AiUsageOverview {
  totalRequests: number;
  successRequests: number;
  failedRequests: number;
  totalTokens: number;
  estimatedCost: number;
  avgLatencyMs: number;
  fallbackCount: number;
}
export interface AiProviderUsage {
  provider: string;
  requests: number;
  totalTokens: number;
  successCount: number;
  failedCount: number;
  avgLatencyMs: number;
  totalCost: number;
}
export interface AiModelUsage {
  model: string;
  provider: string;
  requests: number;
  inputTokens: number;
  outputTokens: number;
  totalTokens: number;
  totalCost: number;
}
export interface AiOrgUsage {
  organizationId: string;
  organizationName: string;
  requests: number;
  totalTokens: number;
  totalCost: number;
}
export interface AiOperationUsage {
  operation: string;
  requests: number;
  totalTokens: number;
  totalCost: number;
}
export interface AiFailure {
  reason: string;
  count: number;
}
export interface AiPlatformUsage {
  overview: AiUsageOverview;
  providers: AiProviderUsage[];
  models: AiModelUsage[];
  organizations: AiOrgUsage[];
  operations: AiOperationUsage[];
  failures: AiFailure[];
}

@Injectable({ providedIn: 'root' })
export class SuperAdminService {
  private http = inject(HttpClient);
  getDashboard() {
    return this.http.get<SuperAdminDashboard>(`${environment.apiUrl}/api/superadmin/dashboard`, { withCredentials: true });
  }
  getOrganizations(search?: string, page = 1, pageSize = 10) {
    let params: any = { page, pageSize };
    if (search) params.search = search;
    return this.http.get<SuperAdminOrgsResponse>(`${environment.apiUrl}/api/superadmin/organizations`, { params, withCredentials: true });
  }
  getUsers(search?: string, page = 1, pageSize = 10) {
    let params: any = { page, pageSize };
    if (search) params.search = search;
    return this.http.get<SuperAdminUsersResponse>(`${environment.apiUrl}/api/superadmin/users`, { params, withCredentials: true });
  }
  getSubscriptions() {
    return this.http.get<SuperAdminSubscriptionsResponse>(`${environment.apiUrl}/api/superadmin/subscriptions`, { withCredentials: true });
  }
  getOrgMembers(orgId: string, search?: string, page = 1, pageSize = 10) {
    let params: any = { page, pageSize };
    if (search) params.search = search;
    return this.http.get<OrgMembersResponse>(`${environment.apiUrl}/api/superadmin/organizations/${orgId}/members`, { params, withCredentials: true });
  }
  suspendOrganization(orgId: string, reason: string, message?: string) {
    return this.http.put(`${environment.apiUrl}/api/superadmin/organizations/${orgId}/suspend`, { reason, message }, { withCredentials: true });
  }
  activateOrganization(orgId: string) {
    return this.http.put(`${environment.apiUrl}/api/superadmin/organizations/${orgId}/activate`, {}, { withCredentials: true });
  }
  deleteOrganization(orgId: string) {
    return this.http.delete(`${environment.apiUrl}/api/superadmin/organizations/${orgId}`, { withCredentials: true });
  }
  suspendUser(userId: string, organizationId: string, reason: string, message: string, deadlineDays = 7) {
    return this.http.post(`${environment.apiUrl}/api/superadmin/users/${userId}/suspend`, { organizationId, reason, message, deadlineDays }, { withCredentials: true });
  }
  activateUser(userId: string) {
    return this.http.put(`${environment.apiUrl}/api/superadmin/users/${userId}/activate`, {}, { withCredentials: true });
  }
  deleteUser(userId: string) {
    return this.http.delete(`${environment.apiUrl}/api/superadmin/users/${userId}`, { withCredentials: true });
  }
  getNotices(orgId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/organizations/${orgId}/notices`, { withCredentials: true });
  }
  getComplaints(orgId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/organizations/${orgId}/complaints`, { withCredentials: true });
  }
  createComplaint(orgId: string, subject: string, message: string) {
    return this.http.post(`${environment.apiUrl}/api/organizations/${orgId}/complaints`, { subject, message }, { withCredentials: true });
  }
  replyComplaint(orgId: string, complaintId: string, message: string) {
    return this.http.post(`${environment.apiUrl}/api/organizations/${orgId}/complaints/${complaintId}/reply`, { message }, { withCredentials: true });
  }
  getSuperAdminComplaints(orgId?: string) {
    let params: any = {};
    if (orgId) params.organizationId = orgId;
    return this.http.get<any[]>(`${environment.apiUrl}/api/superadmin/complaints`, { params, withCredentials: true });
  }
  replyAsSuperAdmin(complaintId: string, message: string) {
    return this.http.post(`${environment.apiUrl}/api/superadmin/complaints/${complaintId}/reply`, { message }, { withCredentials: true });
  }
  getComplaintDetail(orgId: string, complaintId: string) {
    return this.http.get<any>(`${environment.apiUrl}/api/organizations/${orgId}/complaints/${complaintId}`, { withCredentials: true });
  }
  getSuperAdminComplaintDetail(complaintId: string) {
    return this.http.get<any>(`${environment.apiUrl}/api/superadmin/complaints/${complaintId}`, { withCredentials: true });
  }
  deleteComplaint(orgId: string, complaintId: string) {
    return this.http.delete(`${environment.apiUrl}/api/organizations/${orgId}/complaints/${complaintId}`, { withCredentials: true });
  }
  deleteSuperAdminComplaint(complaintId: string) {
    return this.http.delete(`${environment.apiUrl}/api/superadmin/complaints/${complaintId}`, { withCredentials: true });
  }
  getPlatformActivities(search?: string, action?: string, page = 1, pageSize = 10) {
    let params: any = { page, pageSize };
    if (search) params.search = search;
    if (action && action !== 'All') params.action = action;
    return this.http.get<{ items: any[]; total: number; page: number; pageSize: number }>(`${environment.apiUrl}/api/superadmin/activities`, { params, withCredentials: true });
  }
  getSystemStatus() {
    return this.http.get<any>(`${environment.apiUrl}/api/superadmin/system`, { withCredentials: true });
  }
  getFeatureFlags() {
    return this.http.get<any[]>(`${environment.apiUrl}/api/superadmin/flags`, { withCredentials: true });
  }
  toggleFlag(key: string) {
    return this.http.put<any>(`${environment.apiUrl}/api/superadmin/flags/${key}/toggle`, {}, { withCredentials: true });
  }
  getOrgFeatureFlags(orgId: string) {
    return this.http.get<any[]>(`${environment.apiUrl}/api/superadmin/flags/organizations/${orgId}`, { withCredentials: true });
  }
  toggleOrgFlag(orgId: string, key: string) {
    return this.http.put<any>(`${environment.apiUrl}/api/superadmin/flags/${key}/organizations/${orgId}/toggle`, {}, { withCredentials: true });
  }
  getAiUsage() {
    return this.http.get<AiPlatformUsage>(`${environment.apiUrl}/api/superadmin/ai-usage`, { withCredentials: true });
  }
}

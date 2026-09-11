import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

export interface AttachmentDto {
  id: string;
  projectId: string;
  taskId: string;
  uploaderId: string;
  fileName: string;
  url: string;
  publicId: string;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AttachmentService {
  private http = inject(HttpClient);

  getAttachments(taskId: string) {
    return this.http.get<AttachmentDto[]>(`${environment.apiUrl}/api/tasks/${taskId}/attachments`, { withCredentials: true });
  }

  upload(taskId: string, file: File) {
    const fd = new FormData();
    fd.append('TaskId', taskId);
    fd.append('File', file, file.name);
    return this.http.post<AttachmentDto>(`${environment.apiUrl}/api/files/upload`, fd, { withCredentials: true });
  }

  delete(attachmentId: string) {
    return this.http.delete(`${environment.apiUrl}/api/files/${attachmentId}`, { withCredentials: true });
  }

  // Cloudinary thumb: w_300 for grid, w_600 for preview, f_auto,q_auto
  getThumbUrl(url: string, width = 300): string {
    if (!url || !url.includes('cloudinary')) return url;
    // insert transformation after /upload/
    return url.replace('/upload/', `/upload/f_auto,q_auto,w_${width}/`);
  }

  getDownloadUrl(url: string, fileName: string): string {
    // Use direct secure_url — Cloudinary fl_attachment via /upload/fl_attachment was 400 for old image/pdf (public_id too long) and for raw vs image type mismatch.
    // Direct URL with download attribute works for same-origin; for cross-origin we fetch as blob in component.
    return url || '';
  }

  async download(url: string, fileName: string): Promise<void> {
    try {
      const res = await fetch(url, { mode: 'cors' });
      if (!res.ok) throw new Error('Fetch failed ' + res.status);
      const blob = await res.blob();
      const blobUrl = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = blobUrl;
      a.download = fileName || 'download';
      document.body.appendChild(a);
      a.click();
      a.remove();
      setTimeout(() => URL.revokeObjectURL(blobUrl), 2000);
    } catch {
      // Fallback: open in new tab (let browser handle)
      window.open(url, '_blank');
    }
  }

  getPreviewType(mime: string, fileName: string): 'image' | 'video' | 'pdf' | 'office' | 'archive' | 'text' | 'other' {
    const mt = (mime || '').toLowerCase();
    const ext = (fileName.split('.').pop() || '').toLowerCase();
    if (mt.startsWith('image/') || ['jpg','jpeg','png','gif','webp','svg'].includes(ext)) return 'image';
    if (mt.startsWith('video/') || ['mp4','mov','webm'].includes(ext)) return 'video';
    if (mt === 'application/pdf' || ext === 'pdf') return 'pdf';
    if (['doc','docx','xls','xlsx','ppt','pptx'].includes(ext) || mt.includes('msword') || mt.includes('officedocument') || mt.includes('presentation')) return 'office';
    if (['zip'].includes(ext) || mt.includes('zip')) return 'archive';
    if (mt.startsWith('text/') || ['txt','csv'].includes(ext)) return 'text';
    return 'other';
  }

  formatSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024*1024) return `${(bytes/1024).toFixed(1)} KB`;
    return `${(bytes/1024/1024).toFixed(1)} MB`;
  }
}

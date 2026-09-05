import { Injectable, signal } from '@angular/core';

export interface Toast { id: number; message: string; type: 'success' | 'error' | 'info'; }

/**
 * ToastService - MNC-grade: signals + computed auto-dismiss 3s. Used by all mutations (create/update/delete 403->error).
 */
@Injectable({ providedIn: 'root' })
export class ToastService {
  toasts = signal<Toast[]>([]);
  private seq = 0;

  show(message: string, type: Toast['type'] = 'success') {
    const id = ++this.seq;
    this.toasts.update(t => [...t, { id, message, type }]);
    setTimeout(() => this.dismiss(id), 3000);
  }
  success(m: string) { this.show(m, 'success'); }
  error(m: string) { this.show(m, 'error'); }
  info(m: string) { this.show(m, 'info'); }
  dismiss(id: number) { this.toasts.update(t => t.filter(x => x.id !== id)); }
}

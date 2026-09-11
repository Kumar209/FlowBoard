import { Component, ChangeDetectionStrategy, input, output, signal, computed, effect, HostListener, ElementRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * ColumnModal - Create/Edit Board Column with name + position.
 * Validates position uniqueness against existingColumns.
 */
@Component({
  selector: 'app-column-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './column-modal.component.html',
  styleUrls: ['./column-modal.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ColumnModalComponent {
  open = input<boolean>(false);
  mode = input<'create'|'update'>('create');
  initialName = input<string>('');
  initialPosition = input<number>(0);
  existingColumns = input<{id:string; name:string; position:number}[]>([]);
  currentColumnId = input<string | null>(null);
  loading = input<boolean>(false);
  error = input<string|null>(null);
  availableStatuses = input<{id:string; name:string}[]>([]);
  initialStatusIds = input<string[]>([]);
  closed = output<void>();
  submitted = output<{name:string; position:number; statusIds:string[]}>();

  name = signal('');
  position = signal<number>(0);
  selectedStatusIds = signal<string[]>([]);
  dropdownOpen = signal(false);
  isUpdate = computed(() => this.mode() === 'update');
  private el = inject(ElementRef);
  pendingMoveStatus = signal<{id:string; name:string; fromColumn:string} | null>(null);

  // Map statusId -> column name for already used statuses (excluding current column)
  usedStatusMap = computed(() => {
    const map = new Map<string,string>();
    const cols: any[] = (this.existingColumns() as any) || [];
    const curId = this.currentColumnId();
    for (const c of cols) {
      if (c.id === curId) continue;
      const sids: string[] = (c as any).statusIds || [];
      for (const sid of sids) {
        const st = this.availableStatuses().find(s=>s.id===sid);
        map.set(sid, c.name || st?.name || sid.slice(0,4));
      }
    }
    return map;
  });

  @HostListener('document:click', ['$event'])
  onDocClick(event: MouseEvent) {
    if (!this.dropdownOpen()) return;
    const target = event.target as HTMLElement;
    const host = this.el.nativeElement as HTMLElement;
    const dropdown = host.querySelector('.status-dropdown');
    const button = host.querySelector('.status-dropdown-button');
    if (dropdown && !dropdown.contains(target) && button && !button.contains(target)) {
      this.dropdownOpen.set(false);
    }
  }
  @HostListener('document:keydown.escape')
  onEsc() { this.dropdownOpen.set(false); }

  positionError = computed(() => {
    const p = this.position();
    if (p < 0) return 'Position must be >= 0';
    const cols = this.existingColumns();
    const curId = this.currentColumnId();
    const conflict = cols.find(c => c.position === p && c.id !== curId);
    if (conflict) return `Position ${p} already used by "${conflict.name}" — choose another`;
    return null;
  });

  isValid = computed(() => this.name().trim().length > 0 && this.name().trim().length <= 100 && !this.positionError() && this.selectedStatusIds().length > 0);

  constructor() {
    effect(() => {
      if (this.open()) {
        this.name.set(this.initialName() || '');
        this.position.set(this.initialPosition());
        // Only set from initialStatusIds on open, don't auto-select by name to avoid freeze and duplicate
        const init = this.initialStatusIds() || [];
        this.selectedStatusIds.set([...init]);
        this.dropdownOpen.set(false);
      }
    }, { allowSignalWrites: true });
  }

  toggleStatus(id:string, checked:boolean){
    const usedMap = this.usedStatusMap();
    const alreadyIn = usedMap.get(id);
    if (checked && alreadyIn) {
      const st = this.availableStatuses().find(s=>s.id===id);
      this.pendingMoveStatus.set({ id, name: st?.name || id.slice(0,4), fromColumn: alreadyIn });
      return;
    }
    if(checked) this.selectedStatusIds.set([...this.selectedStatusIds(), id]);
    else this.selectedStatusIds.set(this.selectedStatusIds().filter(x=>x!==id));
  }
  confirmMoveStatus(){
    const pending = this.pendingMoveStatus();
    if (!pending) return;
    this.selectedStatusIds.set([...this.selectedStatusIds(), pending.id]);
    this.pendingMoveStatus.set(null);
  }
  cancelMoveStatus(){ this.pendingMoveStatus.set(null); }

  submit() {
    if (!this.isValid()) return;
    this.submitted.emit({ name: this.name().trim(), position: this.position(), statusIds: this.selectedStatusIds() });
  }
}

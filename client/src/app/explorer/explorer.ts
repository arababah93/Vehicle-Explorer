import { Component, computed, ElementRef, HostListener, inject, OnInit, signal, viewChild } from '@angular/core';
import { forkJoin } from 'rxjs';
import { Make, Model, VehicleType } from '../models';
import { VehicleApi } from '../vehicle-api';

@Component({
  selector: 'app-explorer',
  templateUrl: './explorer.html',
})
export class Explorer implements OnInit {
  private readonly api = inject(VehicleApi);
  private readonly makeSearch = viewChild<ElementRef<HTMLInputElement>>('makeSearch');
  private readonly pageSize = 40;
  private requestId = 0;
  private searchTimer: ReturnType<typeof setTimeout> | undefined;

  protected readonly makesOpen = signal(false);
  protected readonly prefix = signal('');
  protected readonly makes = signal<Make[]>([]);
  protected readonly totalMakes = signal(0);
  protected readonly makesStatus = signal<'loading' | 'ready' | 'error'>('loading');
  protected readonly loadingMore = signal(false);
  protected readonly selectedMake = signal<Make | null>(null);
  protected readonly selectedYear = signal<number | null>(null);
  protected readonly types = signal<VehicleType[]>([]);
  protected readonly selectedType = signal<string | null>(null);
  protected readonly models = signal<Model[]>([]);
  protected readonly resultsStatus = signal<'idle' | 'loading' | 'ready' | 'error'>('idle');
  protected readonly modelsBusy = signal(false);

  protected readonly years = computed(() => {
    const current = new Date().getFullYear();
    const years: number[] = [];
    for (let year = current; year >= 1996; year--) {
      years.push(year);
    }
    return years;
  });

  protected readonly modelGroups = computed(() => {
    const groups: { letter: string; models: Model[] }[] = [];
    for (const model of this.models()) {
      const letter = (model.name[0] ?? '#').toLocaleUpperCase();
      const current = groups.at(-1);
      if (current?.letter === letter) {
        current.models.push(model);
      } else {
        groups.push({ letter, models: [model] });
      }
    }
    return groups;
  });

  protected readonly canLoadMore = computed(
    () => this.makesStatus() === 'ready' && this.makes().length < this.totalMakes(),
  );

  ngOnInit(): void {
    this.reloadMakes();
  }

  protected toggleMakes(): void {
    const open = !this.makesOpen();
    this.makesOpen.set(open);
    if (open) {
      setTimeout(() => this.makeSearch()?.nativeElement.focus());
    }
  }

  protected formatCount(value: number): string {
    return value.toLocaleString('en-US');
  }

  protected closeMakes(): void {
    this.makesOpen.set(false);
  }

  @HostListener('document:keydown.escape')
  protected closeMakesOnEscape(): void {
    this.closeMakes();
  }

  protected onPrefixInput(value: string): void {
    this.prefix.set(value);
    clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => this.reloadMakes(), 300);
  }

  protected onMakesScroll(event: Event): void {
    const list = event.target as HTMLElement;
    const nearBottom = list.scrollTop + list.clientHeight >= list.scrollHeight - 32;
    if (nearBottom) {
      this.loadMore();
    }
  }

  protected loadMore(): void {
    if (!this.canLoadMore() || this.loadingMore()) {
      return;
    }

    this.fetchMakes(this.requestId, this.makes().length, false);
  }

  protected retryMakes(): void {
    if (this.makes().length === 0) {
      this.reloadMakes();
      return;
    }

    this.loadMore();
  }

  protected selectMake(make: Make): void {
    this.selectedMake.set(make);
    this.selectedType.set(null);
    this.types.set([]);
    this.models.set([]);
    this.closeMakes();
    this.loadResults();
  }

  protected selectYear(value: string): void {
    const year = Number(value);
    this.selectedYear.set(Number.isInteger(year) && year >= 1996 ? year : null);
    this.loadResults();
  }

  protected selectType(name: string): void {
    this.selectedType.set(name ? name : null);
    this.loadModels();
  }

  protected retryResults(): void {
    this.loadResults();
  }

  private reloadMakes(): void {
    const id = ++this.requestId;
    this.makes.set([]);
    this.totalMakes.set(0);
    this.makesStatus.set('loading');
    this.fetchMakes(id, 0, true);
  }

  private fetchMakes(id: number, skip: number, replace: boolean): void {
    this.loadingMore.set(!replace);
    this.api.getMakes(this.prefix(), skip, this.pageSize).subscribe({
      next: (page) => {
        if (id !== this.requestId) {
          return;
        }

        this.makes.update((current) => (replace ? page.items : [...current, ...page.items]));
        this.totalMakes.set(page.total);
        this.makesStatus.set('ready');
        this.loadingMore.set(false);
      },
      error: () => {
        if (id !== this.requestId) {
          return;
        }

        this.makesStatus.set(replace ? 'error' : 'ready');
        this.loadingMore.set(false);
      },
    });
  }

  private loadResults(): void {
    const make = this.selectedMake();
    const year = this.selectedYear();
    if (!make || !year) {
      this.resultsStatus.set('idle');
      this.types.set([]);
      this.models.set([]);
      return;
    }

    this.resultsStatus.set('loading');
    this.modelsBusy.set(false);
    this.selectedType.set(null);
    forkJoin({
      types: this.api.getVehicleTypes(make.id),
      models: this.api.getModels(make.id, year),
    }).subscribe({
      next: ({ types, models }) => {
        this.types.set(types);
        this.models.set(models);
        this.resultsStatus.set('ready');
      },
      error: () => this.resultsStatus.set('error'),
    });
  }

  private loadModels(): void {
    const make = this.selectedMake();
    const year = this.selectedYear();
    if (!make || !year) {
      return;
    }

    this.modelsBusy.set(true);
    this.api.getModels(make.id, year, this.selectedType()).subscribe({
      next: (models) => {
        this.models.set(models);
        this.modelsBusy.set(false);
        this.resultsStatus.set('ready');
      },
      error: () => {
        this.modelsBusy.set(false);
        this.resultsStatus.set('error');
      },
    });
  }
}

import { Component, computed, input, output } from '@angular/core';

/**
 * "Showing 21–40 of 192" with previous/next buttons, under every paged table or grid.
 * It does not load data itself: it tells the page which page number to load.
 */
@Component({
  selector: 'app-paginator',
  template: `
    <span class="paginator__range">
      @if (totalCount() > 0) {
        מציג {{ from() }}–{{ to() }} מתוך {{ totalCount() }}
      }
    </span>
    <div class="paginator__buttons">
      <!-- In RTL "previous" points right, so the arrows are chosen to match the reading direction. -->
      <button
        type="button"
        class="icon-button"
        [disabled]="page() <= 1"
        (click)="pageChange.emit(page() - 1)"
        aria-label="העמוד הקודם"
      >
        <span class="icon" aria-hidden="true">chevron_right</span>
      </button>
      <span class="paginator__page">עמוד {{ page() }} מתוך {{ pageCount() }}</span>
      <button
        type="button"
        class="icon-button"
        [disabled]="page() >= pageCount()"
        (click)="pageChange.emit(page() + 1)"
        aria-label="העמוד הבא"
      >
        <span class="icon" aria-hidden="true">chevron_left</span>
      </button>
    </div>
  `,
  styles: `
    :host {
      display: flex;
      align-items: center;
      justify-content: space-between;
      flex-wrap: wrap;
      gap: var(--space-3);
      padding-block-start: var(--space-4);
      font-size: var(--font-size-sm);
      color: var(--color-text-secondary);
    }
    .paginator__buttons {
      display: flex;
      align-items: center;
      gap: var(--space-2);
    }
    .paginator__page {
      color: var(--color-text);
      font-weight: 500;
    }
  `,
})
export class Paginator {
  readonly page = input.required<number>();
  readonly pageSize = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageChange = output<number>();

  protected readonly pageCount = computed(() =>
    Math.max(1, Math.ceil(this.totalCount() / this.pageSize())),
  );
  protected readonly from = computed(() => (this.page() - 1) * this.pageSize() + 1);
  protected readonly to = computed(() =>
    Math.min(this.page() * this.pageSize(), this.totalCount()),
  );
}

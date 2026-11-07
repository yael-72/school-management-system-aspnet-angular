import { Component, ElementRef, effect, input, output, viewChild } from '@angular/core';

/**
 * A modal window built on the native <dialog> element, which gives us focus trapping,
 * the Escape key and the dark backdrop for free (no extra library needed).
 *
 * Usage: <app-dialog [open]="isOpen()" title="..." (closed)="isOpen.set(false)"> ...form... </app-dialog>
 */
@Component({
  selector: 'app-dialog',
  template: `
    <dialog #dialog class="dialog" [attr.aria-labelledby]="titleId" (close)="closed.emit()">
      <header class="dialog__header">
        <h2 class="dialog__title" [id]="titleId">{{ title() }}</h2>
        <button type="button" class="icon-button" (click)="dialog.close()" aria-label="סגירה">
          <span class="icon" aria-hidden="true">close</span>
        </button>
      </header>
      <ng-content />
    </dialog>
  `,
  styles: `
    .dialog {
      inline-size: min(520px, calc(100vw - 32px));
      max-block-size: calc(100vh - 32px);
      padding: var(--space-6);
      color: var(--color-text);
      background: var(--color-surface);
      border: 0;
      border-radius: var(--radius-card);
      box-shadow: var(--shadow-soft);

      &::backdrop {
        background: rgba(11, 20, 55, 0.4);
      }
    }
    .dialog__header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      margin-block-end: var(--space-5);
    }
    .dialog__title {
      font-size: var(--font-size-lg);
      font-weight: 700;
    }
  `,
})
export class Dialog {
  private static nextId = 0;

  readonly open = input(false);
  readonly title = input.required<string>();
  /** Emitted when the dialog closes for any reason (close button, Escape key or the parent). */
  readonly closed = output<void>();

  protected readonly titleId = `dialog-title-${Dialog.nextId++}`;
  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    // Keep the native dialog in sync with the [open] input.
    effect(() => {
      const dialog = this.dialog().nativeElement;
      if (this.open() && !dialog.open) {
        dialog.showModal();
      } else if (!this.open() && dialog.open) {
        dialog.close();
      }
    });
  }
}

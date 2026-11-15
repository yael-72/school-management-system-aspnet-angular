import { Component, input, output } from '@angular/core';

/**
 * An on/off switch (Horizon "Switch"). It is a real checkbox with role="switch", so keyboard and
 * screen readers work as expected.
 * Usage: <app-toggle label="תזכורת לפני מבחן" [checked]="on()" (changed)="save($event)" />
 *
 * It is "controlled": a click only asks the parent for a change (the `changed` output), and the
 * switch shows whatever `checked` says. So if the parent rejects or undoes the change, the switch
 * cannot get out of sync with the data.
 */
@Component({
  selector: 'app-toggle',
  template: `
    <label class="toggle" [class.is-disabled]="disabled()">
      <input
        type="checkbox"
        role="switch"
        class="toggle__input"
        [checked]="checked()"
        [disabled]="disabled()"
        (change)="onChange($event)"
      />
      <span class="toggle__track" aria-hidden="true"></span>
      <span class="toggle__label">{{ label() }}</span>
    </label>
  `,
  styles: `
    .toggle {
      display: inline-flex;
      align-items: center;
      gap: var(--space-3);
      font-size: var(--font-size-sm);
      font-weight: 500;
      color: var(--color-text);
      cursor: pointer;
    }
    .toggle.is-disabled {
      cursor: not-allowed;
      opacity: 0.6;
    }
    .toggle__input {
      position: absolute;
      opacity: 0;
      pointer-events: none;
    }
    .toggle__track {
      position: relative;
      flex: none;
      inline-size: 36px;
      block-size: 20px;
      background: var(--color-border);
      border-radius: var(--radius-pill);
      transition: background-color var(--transition-fast);
    }
    // The knob. In RTL "start" is the right side, so "off" sits on the right and "on" moves left.
    .toggle__track::after {
      content: '';
      position: absolute;
      inset-block-start: 3px;
      inset-inline-start: 3px;
      inline-size: 14px;
      block-size: 14px;
      background: #fff;
      border-radius: 50%;
      transition: inset-inline-start var(--transition-fast);
    }
    .toggle__input:checked + .toggle__track {
      background: var(--color-primary);
    }
    .toggle__input:checked + .toggle__track::after {
      inset-inline-start: 19px;
    }
    .toggle__input:focus-visible + .toggle__track {
      outline: 2px solid var(--color-primary);
      outline-offset: 2px;
    }
  `,
})
export class Toggle {
  readonly label = input.required<string>();
  readonly checked = input(false);
  readonly disabled = input(false);
  readonly changed = output<boolean>();

  protected onChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const requested = input.checked;
    input.checked = this.checked(); // the [checked] input stays the only source of truth
    this.changed.emit(requested);
  }
}

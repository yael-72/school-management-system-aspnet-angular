import { Component, computed, input } from '@angular/core';

/** Background colors for avatars; picked by the name so the same person always gets the same color. */
const AVATAR_COLORS = ['#4318ff', '#05cd99', '#ffb547', '#ee5d50', '#3965ff', '#7551ff', '#01b574'];

/**
 * A colored circle with the person's initials (the design uses initials instead of photos).
 */
@Component({
  selector: 'app-avatar',
  template: `<span class="avatar" [style.background-color]="color()" [style.--avatar-size.px]="size()"
    role="img" [attr.aria-label]="name()">{{ initials() }}</span>`,
  styles: `
    .avatar {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      inline-size: var(--avatar-size);
      block-size: var(--avatar-size);
      border-radius: 50%;
      color: #fff;
      font-weight: 700;
      font-size: calc(var(--avatar-size) * 0.38);
      flex-shrink: 0;
    }
  `,
})
export class Avatar {
  readonly name = input.required<string>();
  readonly size = input(40);

  protected readonly initials = computed(() =>
    this.name()
      .split(' ')
      .filter((part) => part.length > 0)
      .slice(0, 2)
      .map((part) => part[0])
      .join(''),
  );

  protected readonly color = computed(() => {
    const hash = [...this.name()].reduce((sum, char) => sum + char.charCodeAt(0), 0);
    return AVATAR_COLORS[hash % AVATAR_COLORS.length];
  });
}

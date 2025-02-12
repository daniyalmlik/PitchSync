import { Component } from '@angular/core';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading',
  standalone: true,
  imports: [MatProgressSpinnerModule],
  template: `
    <div style="display:flex;justify-content:center;padding:32px">
      <mat-spinner diameter="40" />
    </div>
  `
})
export class LoadingComponent {}

import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

@Component({
  selector: 'app-invite-code-dialog',
  standalone: true,
  imports: [CommonModule, FormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>Private Room</h2>
    <mat-dialog-content>
      <p>This room requires an invite code to join.</p>
      <mat-form-field appearance="outline" class="full-width">
        <mat-label>Invite Code</mat-label>
        <input matInput [(ngModel)]="code" (keyup.enter)="confirm()" placeholder="Enter code..." />
      </mat-form-field>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-stroked-button mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="!code.trim()" (click)="confirm()">Join</button>
    </mat-dialog-actions>
  `,
  styles: [`.full-width { width: 100%; }`],
})
export class InviteCodeDialogComponent {
  private readonly dialogRef = inject(MatDialogRef<InviteCodeDialogComponent>);
  code = '';

  confirm(): void {
    if (this.code.trim()) this.dialogRef.close(this.code.trim());
  }
}

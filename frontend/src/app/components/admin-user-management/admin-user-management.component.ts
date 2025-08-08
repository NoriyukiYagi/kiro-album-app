import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-admin-user-management',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <h2>ユーザー管理</h2>
      <p>管理者用ユーザー管理機能（実装予定）</p>
    </div>
  `,
  styles: []
})
export class AdminUserManagementComponent {
}
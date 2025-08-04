import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-album-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="album-container">
      <h1>アルバム</h1>
      
      <div class="upload-section">
        <button mat-raised-button color="primary">
          <mat-icon>cloud_upload</mat-icon>
          写真・動画をアップロード
        </button>
      </div>
      
      <div class="media-grid">
        <mat-card class="media-item" *ngFor="let item of mockItems">
          <img [src]="item.thumbnail" [alt]="item.name">
          <mat-card-content>
            <p>{{item.name}}</p>
            <small>{{item.date}}</small>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .album-container {
      max-width: 1200px;
      margin: 0 auto;
    }
    
    .upload-section {
      margin: 20px 0;
      text-align: center;
    }
    
    .media-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(200px, 1fr));
      gap: 16px;
    }
    
    .media-item {
      cursor: pointer;
      transition: transform 0.2s ease;
    }
    
    .media-item:hover {
      transform: scale(1.02);
    }
    
    .media-item img {
      width: 100%;
      height: 150px;
      object-fit: cover;
    }
  `]
})
export class AlbumListComponent {
  mockItems = [
    { name: 'Sample Image 1', date: '2024-01-01', thumbnail: 'https://via.placeholder.com/200x150' },
    { name: 'Sample Image 2', date: '2024-01-02', thumbnail: 'https://via.placeholder.com/200x150' },
    { name: 'Sample Image 3', date: '2024-01-03', thumbnail: 'https://via.placeholder.com/200x150' }
  ];
}
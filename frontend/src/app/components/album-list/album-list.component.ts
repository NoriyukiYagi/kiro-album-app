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
  templateUrl: './album-list.component.html',
  styleUrls: ['./album-list.component.scss']
})
export class AlbumListComponent {
  mockItems = [
    { name: 'Sample Image 1', date: '2024-01-01', thumbnail: 'https://via.placeholder.com/200x150' },
    { name: 'Sample Image 2', date: '2024-01-02', thumbnail: 'https://via.placeholder.com/200x150' },
    { name: 'Sample Image 3', date: '2024-01-03', thumbnail: 'https://via.placeholder.com/200x150' }
  ];
}
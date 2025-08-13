import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { Subject, takeUntil, finalize } from 'rxjs';

import { MediaService } from '../../services/media.service';
import { MediaFile, MediaListResponse } from '../../models/media.model';

@Component({
  selector: 'app-album-list',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,

  ],
  templateUrl: './album-list.component.html',
  styleUrls: ['./album-list.component.scss']
})
export class AlbumListComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  // Data properties
  mediaFiles: MediaFile[] = [];
  totalCount = 0;
  pageIndex = 0;
  pageSize = 20;
  pageSizeOptions = [12, 20, 40, 60];
  
  // UI state properties
  isLoading = false;
  hasError = false;
  errorMessage = '';

  // Expose Math for template
  Math = Math;

  constructor(
    private mediaService: MediaService,
    private snackBar: MatSnackBar,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadMediaFiles();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Load media files from the server
   */
  loadMediaFiles(): void {
    this.isLoading = true;
    this.hasError = false;

    this.mediaService.getMediaList(this.pageIndex, this.pageSize)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => this.isLoading = false)
      )
      .subscribe({
        next: (response: MediaListResponse) => {
          this.mediaFiles = response.items;
          this.totalCount = response.totalCount;
          // Don't override pageIndex as it's already set by onPageChange or initial load
          this.hasError = false;
        },
        error: (error) => {
          console.error('Failed to load media files:', error);
          this.hasError = true;
          this.errorMessage = this.getErrorMessage(error);
          this.showErrorSnackBar(this.errorMessage);
        }
      });
  }

  /**
   * Handle page change events from paginator
   */
  onPageChange(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadMediaFiles();
  }

  /**
   * Get thumbnail URL for a media file
   */
  getThumbnailUrl(mediaFile: MediaFile): string {
    return this.mediaService.getThumbnailUrl(mediaFile.id);
  }

  /**
   * Handle thumbnail click to view media
   */
  onThumbnailClick(mediaFile: MediaFile): void {
    // Navigate to media viewer (will be implemented in task 14)
    // For now, we'll just log the action
    console.log('Viewing media:', mediaFile);
    // TODO: Implement navigation to media viewer
    // this.router.navigate(['/media', mediaFile.id]);
  }

  /**
   * Navigate to upload page
   */
  onUploadClick(): void {
    this.router.navigate(['/upload']);
  }

  /**
   * Refresh the media list
   */
  onRefresh(): void {
    this.pageIndex = 0; // Reset to first page
    this.loadMediaFiles();
  }

  /**
   * Format date for display
   */
  formatDate(date: Date): string {
    if (!date) return '';
    
    const dateObj = new Date(date);
    return dateObj.toLocaleDateString('ja-JP', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit'
    });
  }

  /**
   * Format file size for display
   */
  formatFileSize(bytes: number): string {
    return this.mediaService.formatFileSize(bytes);
  }

  /**
   * Check if media file is a video
   */
  isVideo(mediaFile: MediaFile): boolean {
    return mediaFile.contentType.startsWith('video/');
  }

  /**
   * Check if media file is an image
   */
  isImage(mediaFile: MediaFile): boolean {
    return mediaFile.contentType.startsWith('image/');
  }

  /**
   * Show error message in snack bar
   */
  private showErrorSnackBar(message: string): void {
    this.snackBar.open(message, '閉じる', {
      duration: 5000,
      horizontalPosition: 'center',
      verticalPosition: 'bottom'
    });
  }

  /**
   * Handle image loading errors
   */
  onImageError(event: any, mediaFile: MediaFile): void {
    // Mark the media file as having an image error
    (mediaFile as any).hasImageError = true;
    
    // Hide the broken image
    event.target.style.display = 'none';
  }

  /**
   * Check if media file has image error
   */
  hasImageError(mediaFile: MediaFile): boolean {
    return (mediaFile as any).hasImageError === true;
  }

  /**
   * Track by function for ngFor performance optimization
   */
  trackByMediaId(index: number, mediaFile: MediaFile): number {
    return mediaFile.id;
  }

  /**
   * Extract user-friendly error message
   */
  private getErrorMessage(error: any): string {
    if (error?.error?.message) {
      return error.error.message;
    }
    if (error?.message) {
      return error.message;
    }
    return 'メディアファイルの読み込みに失敗しました。再度お試しください。';
  }
}
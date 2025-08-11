import { Component, OnInit, OnDestroy, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subject, takeUntil } from 'rxjs';

import { MediaService } from '../../services/media.service';
import { UploadProgress, ALLOWED_EXTENSIONS, MAX_FILE_SIZE } from '../../models/media.model';

@Component({
  selector: 'app-upload',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatListModule,
    MatDividerModule,
    MatChipsModule,
    MatTooltipModule
  ],
  templateUrl: './upload.component.html',
  styleUrls: ['./upload.component.scss']
})
export class UploadComponent implements OnInit, OnDestroy {
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;

  uploadProgresses: UploadProgress[] = [];
  isDragOver = false;
  isUploading = false;
  
  readonly allowedExtensions = ALLOWED_EXTENSIONS;
  readonly maxFileSize = MAX_FILE_SIZE;
  
  private destroy$ = new Subject<void>();

  constructor(
    private mediaService: MediaService,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    // Subscribe to upload progress
    this.mediaService.uploadProgress$
      .pipe(takeUntil(this.destroy$))
      .subscribe(progresses => {
        this.uploadProgresses = progresses;
        this.isUploading = progresses.some(p => p.status === 'uploading' || p.status === 'pending');
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  /**
   * Handle file input change
   */
  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleFiles(Array.from(input.files));
    }
  }

  /**
   * Handle drag over event
   */
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  /**
   * Handle drag leave event
   */
  onDragLeave(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  /**
   * Handle drop event
   */
  onDrop(event: DragEvent): void {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;

    if (event.dataTransfer?.files) {
      this.handleFiles(Array.from(event.dataTransfer.files));
    }
  }

  /**
   * Open file dialog
   */
  openFileDialog(): void {
    this.fileInput.nativeElement.click();
  }

  /**
   * Handle selected files
   */
  private handleFiles(files: File[]): void {
    if (files.length === 0) {
      return;
    }

    // Validate files
    const validationErrors = this.mediaService.validateFiles(files);
    if (validationErrors.length > 0) {
      this.snackBar.open(validationErrors.join('\n'), '閉じる', {
        duration: 5000,
        panelClass: ['error-snackbar']
      });
      return;
    }

    // Clear previous upload progress
    this.mediaService.clearUploadProgress();

    // Start upload
    try {
      this.mediaService.uploadFiles(files).subscribe({
        next: (progresses) => {
          // Progress updates are handled by the subscription in ngOnInit
          const completedUploads = progresses.filter(p => p.status === 'completed');
          const errorUploads = progresses.filter(p => p.status === 'error');
          
          if (completedUploads.length > 0 && errorUploads.length === 0) {
            // All uploads completed successfully
            if (progresses.every(p => p.status === 'completed')) {
              this.snackBar.open(
                `${completedUploads.length}個のファイルがアップロードされました`, 
                '閉じる', 
                { duration: 3000, panelClass: ['success-snackbar'] }
              );
            }
          }
        },
        error: (error) => {
          console.error('Upload error:', error);
          this.snackBar.open(
            error.message || 'アップロードに失敗しました', 
            '閉じる', 
            { duration: 5000, panelClass: ['error-snackbar'] }
          );
        }
      });
    } catch (error: any) {
      this.snackBar.open(
        error.message || 'アップロードに失敗しました', 
        '閉じる', 
        { duration: 5000, panelClass: ['error-snackbar'] }
      );
    }

    // Clear file input
    this.fileInput.nativeElement.value = '';
  }

  /**
   * Clear upload progress
   */
  clearProgress(): void {
    this.mediaService.clearUploadProgress();
  }

  /**
   * Get file size formatted string
   */
  getFileSize(bytes: number): string {
    return this.mediaService.formatFileSize(bytes);
  }

  /**
   * Get max file size formatted string
   */
  getMaxFileSize(): string {
    return this.mediaService.formatFileSize(this.maxFileSize);
  }

  /**
   * Get status icon for upload progress
   */
  getStatusIcon(status: string): string {
    switch (status) {
      case 'pending':
        return 'schedule';
      case 'uploading':
        return 'cloud_upload';
      case 'completed':
        return 'check_circle';
      case 'error':
        return 'error';
      default:
        return 'help';
    }
  }

  /**
   * Get status color for upload progress
   */
  getStatusColor(status: string): string {
    switch (status) {
      case 'pending':
        return 'accent';
      case 'uploading':
        return 'primary';
      case 'completed':
        return 'primary';
      case 'error':
        return 'warn';
      default:
        return '';
    }
  }

  /**
   * Check if there are any uploads in progress
   */
  hasUploadsInProgress(): boolean {
    return this.uploadProgresses.some(p => p.status === 'uploading' || p.status === 'pending');
  }

  /**
   * Check if there are any completed uploads
   */
  hasCompletedUploads(): boolean {
    return this.uploadProgresses.some(p => p.status === 'completed');
  }

  /**
   * Check if there are any failed uploads
   */
  hasFailedUploads(): boolean {
    return this.uploadProgresses.some(p => p.status === 'error');
  }

  /**
   * Get count of completed uploads
   */
  getCompletedCount(): number {
    return this.uploadProgresses.filter(p => p.status === 'completed').length;
  }

  /**
   * Get count of failed uploads
   */
  getFailedCount(): number {
    return this.uploadProgresses.filter(p => p.status === 'error').length;
  }
}
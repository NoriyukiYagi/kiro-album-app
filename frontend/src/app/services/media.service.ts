import { Injectable } from '@angular/core';
import { HttpClient, HttpEventType, HttpRequest } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { 
  MediaUploadResponse, 
  MediaListResponse, 
  UploadProgress,
  ALLOWED_FILE_TYPES,
  ALLOWED_EXTENSIONS,
  MAX_FILE_SIZE
} from '../models/media.model';
import { ApiResponse } from '../models/user.model';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MediaService {
  private readonly API_URL = environment.apiUrl;
  
  // Upload progress tracking
  private uploadProgressSubject = new BehaviorSubject<UploadProgress[]>([]);
  public uploadProgress$ = this.uploadProgressSubject.asObservable();

  constructor(private http: HttpClient) {}

  /**
   * Get media files list with pagination
   */
  getMediaList(pageIndex: number = 0, pageSize: number = 20): Observable<MediaListResponse> {
    const params = {
      page: (pageIndex + 1).toString(), // Convert 0-based to 1-based page numbering
      pageSize: pageSize.toString()
    };

    return this.http.get<ApiResponse<MediaListResponse>>(`${this.API_URL}/media`, { params })
      .pipe(
        map(apiResponse => {
          if (!apiResponse.success || !apiResponse.data) {
            throw new Error(apiResponse.message || 'Failed to get media list');
          }
          return apiResponse.data;
        }),
        catchError(error => {
          console.error('Failed to get media list:', error);
          throw error;
        })
      );
  }

  /**
   * Upload multiple files with progress tracking
   */
  uploadFiles(files: File[]): Observable<UploadProgress[]> {
    // Validate files before upload
    const validationErrors = this.validateFiles(files);
    if (validationErrors.length > 0) {
      throw new Error(validationErrors.join('\n'));
    }

    // Initialize progress tracking
    const uploadProgresses: UploadProgress[] = files.map(file => ({
      file,
      progress: 0,
      status: 'pending'
    }));

    this.uploadProgressSubject.next(uploadProgresses);

    // Upload files one by one
    files.forEach((file, index) => {
      this.uploadSingleFile(file, index);
    });

    return this.uploadProgress$;
  }

  /**
   * Upload a single file with progress tracking
   */
  private uploadSingleFile(file: File, index: number): void {
    const formData = new FormData();
    formData.append('file', file);

    const uploadRequest = new HttpRequest('POST', `${this.API_URL}/media/upload`, formData, {
      reportProgress: true
    });

    // Update status to uploading
    this.updateUploadProgress(index, { status: 'uploading', progress: 0 });

    this.http.request<ApiResponse<MediaUploadResponse>>(uploadRequest).subscribe({
      next: (event) => {
        if (event.type === HttpEventType.UploadProgress && event.total) {
          const progress = Math.round(100 * event.loaded / event.total);
          this.updateUploadProgress(index, { progress });
        } else if (event.type === HttpEventType.Response) {
          if (event.body?.success && event.body.data) {
            this.updateUploadProgress(index, {
              status: 'completed',
              progress: 100,
              response: event.body.data
            });
          } else {
            this.updateUploadProgress(index, {
              status: 'error',
              error: event.body?.message || 'Upload failed'
            });
          }
        }
      },
      error: (error) => {
        console.error('Upload failed:', error);
        this.updateUploadProgress(index, {
          status: 'error',
          error: this.getErrorMessage(error)
        });
      }
    });
  }

  /**
   * Update upload progress for a specific file
   */
  private updateUploadProgress(index: number, updates: Partial<UploadProgress>): void {
    const currentProgresses = this.uploadProgressSubject.value;
    if (currentProgresses[index]) {
      currentProgresses[index] = { ...currentProgresses[index], ...updates };
      this.uploadProgressSubject.next([...currentProgresses]);
    }
  }

  /**
   * Validate files before upload
   */
  validateFiles(files: File[]): string[] {
    const errors: string[] = [];

    files.forEach((file, index) => {
      // Check file size
      if (file.size > MAX_FILE_SIZE) {
        errors.push(`ファイル "${file.name}" のサイズが上限（100MB）を超えています。`);
      }

      // Check file type
      const fileExtension = file.name.split('.').pop()?.toLowerCase();
      const isValidType = ALLOWED_FILE_TYPES.includes(file.type) || 
                         (fileExtension && ALLOWED_EXTENSIONS.includes(fileExtension));

      if (!isValidType) {
        errors.push(`ファイル "${file.name}" の形式がサポートされていません。対応形式: JPG, PNG, HEIC, MP4, MOV`);
      }
    });

    return errors;
  }

  /**
   * Check if a file type is supported
   */
  isFileTypeSupported(file: File): boolean {
    const fileExtension = file.name.split('.').pop()?.toLowerCase();
    return ALLOWED_FILE_TYPES.includes(file.type) || 
           (fileExtension ? ALLOWED_EXTENSIONS.includes(fileExtension) : false);
  }

  /**
   * Format file size for display
   */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * Clear upload progress
   */
  clearUploadProgress(): void {
    this.uploadProgressSubject.next([]);
  }

  /**
   * Get thumbnail URL for a media file
   */
  getThumbnailUrl(mediaId: number): string {
    return `${this.API_URL}/media/thumbnail/${mediaId}`;
  }

  /**
   * Get media file URL
   */
  getMediaUrl(mediaId: number): string {
    return `${this.API_URL}/media/${mediaId}`;
  }

  /**
   * Delete a media file
   */
  deleteMedia(mediaId: number): Observable<void> {
    return this.http.delete<ApiResponse<void>>(`${this.API_URL}/media/${mediaId}`)
      .pipe(
        map(apiResponse => {
          if (!apiResponse.success) {
            throw new Error(apiResponse.message || 'Failed to delete media');
          }
        }),
        catchError(error => {
          console.error('Failed to delete media:', error);
          throw error;
        })
      );
  }

  /**
   * Extract user-friendly error message from error object
   */
  private getErrorMessage(error: any): string {
    if (error?.error?.message) {
      return error.error.message;
    }
    if (error?.message) {
      return error.message;
    }
    if (error?.status === 413) {
      return 'ファイルサイズが大きすぎます。100MB以下のファイルをアップロードしてください。';
    }
    if (error?.status === 415) {
      return 'サポートされていないファイル形式です。JPG, PNG, HEIC, MP4, MOVファイルをアップロードしてください。';
    }
    return 'アップロードに失敗しました。再度お試しください。';
  }
}
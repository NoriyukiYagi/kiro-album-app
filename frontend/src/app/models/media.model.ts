export interface MediaFile {
  id: number;
  fileName: string;
  originalFileName: string;
  filePath: string;
  thumbnailPath: string;
  contentType: string;
  fileSize: number;
  takenAt: Date;
  uploadedAt: Date;
  uploadedBy: number;
}

export interface MediaUploadRequest {
  file: File;
}

export interface MediaUploadResponse {
  id: number;
  fileName: string;
  originalFileName: string;
  fileSize: number;
  contentType: string;
  uploadedAt: Date;
}

export interface MediaListResponse {
  items: MediaFile[];
  totalCount: number;
  pageIndex: number;
  pageSize: number;
  totalPages: number;
}

export interface UploadProgress {
  file: File;
  progress: number;
  status: 'pending' | 'uploading' | 'completed' | 'error';
  error?: string;
  response?: MediaUploadResponse;
}

export const ALLOWED_FILE_TYPES = ['image/jpeg', 'image/jpg', 'image/png', 'image/heic', 'video/mp4', 'video/quicktime'];
export const ALLOWED_EXTENSIONS = ['jpg', 'jpeg', 'png', 'heic', 'mp4', 'mov'];
export const MAX_FILE_SIZE = 100 * 1024 * 1024; // 100MB in bytes
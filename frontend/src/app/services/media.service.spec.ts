import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpEventType } from '@angular/common/http';

import { MediaService } from './media.service';
import { 
  MediaListResponse, 
  MediaUploadResponse, 
  UploadProgress,
  ALLOWED_FILE_TYPES,
  ALLOWED_EXTENSIONS,
  MAX_FILE_SIZE
} from '../models/media.model';
import { ApiResponse } from '../models/user.model';
import { environment } from '../../environments/environment';

describe('MediaService', () => {
  let service: MediaService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [MediaService]
    });
    service = TestBed.inject(MediaService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('getMediaList', () => {
    it('should get media list with default pagination', () => {
      const mockResponse: ApiResponse<MediaListResponse> = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          pageIndex: 0,
          pageSize: 20,
          totalPages: 0
        }
      };

      service.getMediaList().subscribe(response => {
        expect(response).toEqual(mockResponse.data!);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/media?pageIndex=0&pageSize=20`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should get media list with custom pagination', () => {
      const mockResponse: ApiResponse<MediaListResponse> = {
        success: true,
        data: {
          items: [],
          totalCount: 0,
          pageIndex: 1,
          pageSize: 10,
          totalPages: 0
        }
      };

      service.getMediaList(1, 10).subscribe(response => {
        expect(response).toEqual(mockResponse.data!);
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/media?pageIndex=1&pageSize=10`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });

    it('should handle API error', () => {
      const mockResponse: ApiResponse<MediaListResponse> = {
        success: false,
        message: 'Failed to get media list'
      };

      service.getMediaList().subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          expect(error.message).toBe('Failed to get media list');
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/media?pageIndex=0&pageSize=20`);
      req.flush(mockResponse);
    });
  });

  describe('validateFiles', () => {
    it('should return no errors for valid files', () => {
      const validFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      const errors = service.validateFiles([validFile]);
      expect(errors).toEqual([]);
    });

    it('should return error for oversized file', () => {
      const oversizedFile = new File(['x'.repeat(MAX_FILE_SIZE + 1)], 'large.jpg', { type: 'image/jpeg' });
      const errors = service.validateFiles([oversizedFile]);
      expect(errors.length).toBe(1);
      expect(errors[0]).toContain('サイズが上限（100MB）を超えています');
    });

    it('should return error for unsupported file type', () => {
      const unsupportedFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      const errors = service.validateFiles([unsupportedFile]);
      expect(errors.length).toBe(1);
      expect(errors[0]).toContain('形式がサポートされていません');
    });

    it('should return multiple errors for multiple invalid files', () => {
      const oversizedFile = new File(['x'.repeat(MAX_FILE_SIZE + 1)], 'large.jpg', { type: 'image/jpeg' });
      const unsupportedFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      const errors = service.validateFiles([oversizedFile, unsupportedFile]);
      expect(errors.length).toBe(2);
    });
  });

  describe('isFileTypeSupported', () => {
    it('should return true for supported MIME types', () => {
      const jpegFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
      expect(service.isFileTypeSupported(jpegFile)).toBeTrue();
    });

    it('should return true for supported extensions', () => {
      const heicFile = new File(['test'], 'test.heic', { type: '' });
      expect(service.isFileTypeSupported(heicFile)).toBeTrue();
    });

    it('should return false for unsupported files', () => {
      const txtFile = new File(['test'], 'test.txt', { type: 'text/plain' });
      expect(service.isFileTypeSupported(txtFile)).toBeFalse();
    });
  });

  describe('formatFileSize', () => {
    it('should format bytes correctly', () => {
      expect(service.formatFileSize(0)).toBe('0 Bytes');
      expect(service.formatFileSize(1024)).toBe('1 KB');
      expect(service.formatFileSize(1024 * 1024)).toBe('1 MB');
      expect(service.formatFileSize(1024 * 1024 * 1024)).toBe('1 GB');
    });

    it('should format decimal values correctly', () => {
      expect(service.formatFileSize(1536)).toBe('1.5 KB');
      expect(service.formatFileSize(1024 * 1024 * 1.5)).toBe('1.5 MB');
    });
  });

  describe('getThumbnailUrl', () => {
    it('should return correct thumbnail URL', () => {
      const url = service.getThumbnailUrl(123);
      expect(url).toBe(`${environment.apiUrl}/media/thumbnail/123`);
    });
  });

  describe('getMediaUrl', () => {
    it('should return correct media URL', () => {
      const url = service.getMediaUrl(123);
      expect(url).toBe(`${environment.apiUrl}/media/123`);
    });
  });

  describe('deleteMedia', () => {
    it('should delete media successfully', () => {
      const mockResponse: ApiResponse<void> = {
        success: true
      };

      service.deleteMedia(123).subscribe(() => {
        // Success case
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/media/123`);
      expect(req.request.method).toBe('DELETE');
      req.flush(mockResponse);
    });

    it('should handle delete error', () => {
      const mockResponse: ApiResponse<void> = {
        success: false,
        message: 'Failed to delete media'
      };

      service.deleteMedia(123).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          expect(error.message).toBe('Failed to delete media');
        }
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/media/123`);
      req.flush(mockResponse);
    });
  });

  describe('clearUploadProgress', () => {
    it('should clear upload progress', () => {
      service.clearUploadProgress();
      service.uploadProgress$.subscribe(progresses => {
        expect(progresses).toEqual([]);
      });
    });
  });
});
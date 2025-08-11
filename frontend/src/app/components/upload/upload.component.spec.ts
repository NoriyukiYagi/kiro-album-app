import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { UploadComponent } from './upload.component';
import { MediaService } from '../../services/media.service';
import { UploadProgress } from '../../models/media.model';

describe('UploadComponent', () => {
  let component: UploadComponent;
  let fixture: ComponentFixture<UploadComponent>;
  let mockMediaService: jasmine.SpyObj<MediaService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let uploadProgressSubject: BehaviorSubject<UploadProgress[]>;

  beforeEach(async () => {
    uploadProgressSubject = new BehaviorSubject<UploadProgress[]>([]);
    
    mockMediaService = jasmine.createSpyObj('MediaService', [
      'uploadFiles',
      'validateFiles',
      'clearUploadProgress',
      'formatFileSize',
      'isFileTypeSupported'
    ], {
      uploadProgress$: uploadProgressSubject.asObservable()
    });

    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [UploadComponent, NoopAnimationsModule],
      providers: [
        { provide: MediaService, useValue: mockMediaService },
        { provide: MatSnackBar, useValue: mockSnackBar }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(UploadComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with empty upload progresses', () => {
    expect(component.uploadProgresses).toEqual([]);
    expect(component.isUploading).toBeFalse();
    expect(component.isDragOver).toBeFalse();
  });

  it('should update upload progresses when service emits', () => {
    const mockProgresses: UploadProgress[] = [
      {
        file: new File(['test'], 'test.jpg', { type: 'image/jpeg' }),
        progress: 50,
        status: 'uploading'
      }
    ];

    uploadProgressSubject.next(mockProgresses);

    expect(component.uploadProgresses).toEqual(mockProgresses);
    expect(component.isUploading).toBeTrue();
  });

  it('should handle drag over event', () => {
    const event = new DragEvent('dragover');
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');

    component.onDragOver(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(component.isDragOver).toBeTrue();
  });

  it('should handle drag leave event', () => {
    component.isDragOver = true;
    const event = new DragEvent('dragleave');
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');

    component.onDragLeave(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(component.isDragOver).toBeFalse();
  });

  it('should handle drop event with files', () => {
    const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const event = new DragEvent('drop');
    Object.defineProperty(event, 'dataTransfer', {
      value: { files: [mockFile] }
    });
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');
    mockMediaService.validateFiles.and.returnValue([]);
    mockMediaService.uploadFiles.and.returnValue(of([]));

    component.onDrop(event);

    expect(event.preventDefault).toHaveBeenCalled();
    expect(event.stopPropagation).toHaveBeenCalled();
    expect(component.isDragOver).toBeFalse();
    expect(mockMediaService.validateFiles).toHaveBeenCalledWith([mockFile]);
  });

  it('should handle file selection', () => {
    const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const mockFileList = {
      0: mockFile,
      length: 1,
      item: (index: number) => index === 0 ? mockFile : null
    } as FileList;
    const event = { target: { files: mockFileList } } as any;
    mockMediaService.validateFiles.and.returnValue([]);
    mockMediaService.uploadFiles.and.returnValue(of([]));

    component.onFileSelected(event);

    expect(mockMediaService.validateFiles).toHaveBeenCalledWith([mockFile]);
    expect(mockMediaService.clearUploadProgress).toHaveBeenCalled();
  });

  it('should show validation errors', () => {
    const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const validationErrors = ['File too large'];
    mockMediaService.validateFiles.and.returnValue(validationErrors);

    // Test the drop event instead, which directly calls handleFiles
    const event = new DragEvent('drop');
    Object.defineProperty(event, 'dataTransfer', {
      value: { files: [mockFile] }
    });
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');

    component.onDrop(event);

    expect(mockSnackBar.open).toHaveBeenCalledWith(
      validationErrors.join('\n'),
      '閉じる',
      { duration: 5000, panelClass: ['error-snackbar'] }
    );
    expect(mockMediaService.uploadFiles).not.toHaveBeenCalled();
  });

  it('should handle upload errors', () => {
    const mockFile = new File(['test'], 'test.jpg', { type: 'image/jpeg' });
    const validationErrors: string[] = [];
    mockMediaService.validateFiles.and.returnValue(validationErrors);
    
    // Mock uploadFiles to throw an error synchronously
    const error = new Error('Upload failed');
    mockMediaService.uploadFiles.and.callFake(() => {
      throw error;
    });

    // Test the drop event instead, which directly calls handleFiles
    const event = new DragEvent('drop');
    Object.defineProperty(event, 'dataTransfer', {
      value: { files: [mockFile] }
    });
    spyOn(event, 'preventDefault');
    spyOn(event, 'stopPropagation');

    component.onDrop(event);

    expect(mockSnackBar.open).toHaveBeenCalledWith(
      'Upload failed',
      '閉じる',
      { duration: 5000, panelClass: ['error-snackbar'] }
    );
  });

  it('should clear progress', () => {
    component.clearProgress();
    expect(mockMediaService.clearUploadProgress).toHaveBeenCalled();
  });

  it('should get correct status icon', () => {
    expect(component.getStatusIcon('pending')).toBe('schedule');
    expect(component.getStatusIcon('uploading')).toBe('cloud_upload');
    expect(component.getStatusIcon('completed')).toBe('check_circle');
    expect(component.getStatusIcon('error')).toBe('error');
    expect(component.getStatusIcon('unknown')).toBe('help');
  });

  it('should get correct status color', () => {
    expect(component.getStatusColor('pending')).toBe('accent');
    expect(component.getStatusColor('uploading')).toBe('primary');
    expect(component.getStatusColor('completed')).toBe('primary');
    expect(component.getStatusColor('error')).toBe('warn');
    expect(component.getStatusColor('unknown')).toBe('');
  });

  it('should detect uploads in progress', () => {
    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 50, status: 'uploading' }
    ];
    expect(component.hasUploadsInProgress()).toBeTrue();

    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 100, status: 'completed' }
    ];
    expect(component.hasUploadsInProgress()).toBeFalse();
  });

  it('should detect completed uploads', () => {
    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 100, status: 'completed' }
    ];
    expect(component.hasCompletedUploads()).toBeTrue();

    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 50, status: 'uploading' }
    ];
    expect(component.hasCompletedUploads()).toBeFalse();
  });

  it('should detect failed uploads', () => {
    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 0, status: 'error', error: 'Failed' }
    ];
    expect(component.hasFailedUploads()).toBeTrue();

    component.uploadProgresses = [
      { file: new File([''], 'test.jpg'), progress: 100, status: 'completed' }
    ];
    expect(component.hasFailedUploads()).toBeFalse();
  });

  it('should get completed count', () => {
    component.uploadProgresses = [
      { file: new File([''], 'test1.jpg'), progress: 100, status: 'completed' },
      { file: new File([''], 'test2.jpg'), progress: 100, status: 'completed' },
      { file: new File([''], 'test3.jpg'), progress: 0, status: 'error', error: 'Failed' }
    ];
    expect(component.getCompletedCount()).toBe(2);
  });

  it('should get failed count', () => {
    component.uploadProgresses = [
      { file: new File([''], 'test1.jpg'), progress: 100, status: 'completed' },
      { file: new File([''], 'test2.jpg'), progress: 0, status: 'error', error: 'Failed' },
      { file: new File([''], 'test3.jpg'), progress: 0, status: 'error', error: 'Failed' }
    ];
    expect(component.getFailedCount()).toBe(2);
  });
});
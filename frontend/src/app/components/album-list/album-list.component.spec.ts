import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterTestingModule } from '@angular/router/testing';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';

import { AlbumListComponent } from './album-list.component';
import { MediaService } from '../../services/media.service';
import { MediaListResponse } from '../../models/media.model';

describe('AlbumListComponent', () => {
  let component: AlbumListComponent;
  let fixture: ComponentFixture<AlbumListComponent>;
  let mediaService: jasmine.SpyObj<MediaService>;

  const mockMediaListResponse: MediaListResponse = {
    items: [
      {
        id: 1,
        fileName: 'test1.jpg',
        originalFileName: 'test1.jpg',
        filePath: '/data/pict/20240101/test1.jpg',
        thumbnailPath: '/data/thumb/20240101/test1.jpg',
        contentType: 'image/jpeg',
        fileSize: 1024000,
        takenAt: new Date('2024-01-01'),
        uploadedAt: new Date('2024-01-01'),
        uploadedBy: 1
      },
      {
        id: 2,
        fileName: 'test2.mp4',
        originalFileName: 'test2.mp4',
        filePath: '/data/pict/20240102/test2.mp4',
        thumbnailPath: '/data/thumb/20240102/test2.jpg',
        contentType: 'video/mp4',
        fileSize: 5120000,
        takenAt: new Date('2024-01-02'),
        uploadedAt: new Date('2024-01-02'),
        uploadedBy: 1
      }
    ],
    totalCount: 2,
    pageIndex: 0,
    pageSize: 20,
    totalPages: 1
  };

  beforeEach(async () => {
    const mediaServiceSpy = jasmine.createSpyObj('MediaService', [
      'getMediaList',
      'getThumbnailUrl',
      'formatFileSize'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        AlbumListComponent,
        HttpClientTestingModule,
        MatSnackBarModule,
        RouterTestingModule,
        BrowserAnimationsModule
      ],
      providers: [
        { provide: MediaService, useValue: mediaServiceSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AlbumListComponent);
    component = fixture.componentInstance;
    mediaService = TestBed.inject(MediaService) as jasmine.SpyObj<MediaService>;

    // Setup default spy returns
    mediaService.getMediaList.and.returnValue(of(mockMediaListResponse));
    mediaService.getThumbnailUrl.and.returnValue('http://localhost:5000/api/media/thumbnail/1');
    mediaService.formatFileSize.and.returnValue('1.0 MB');
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load media files on init', () => {
    component.ngOnInit();
    
    expect(mediaService.getMediaList).toHaveBeenCalledWith(0, 20);
    expect(component.mediaFiles).toEqual(mockMediaListResponse.items);
    expect(component.totalCount).toBe(2);
  });

  it('should handle page change', () => {
    // Set initial state
    component.pageIndex = 0;
    component.pageSize = 20;
    
    const pageEvent = { pageIndex: 1, pageSize: 10, length: 100 };
    
    component.onPageChange(pageEvent);
    
    expect(component.pageIndex).toBe(1);
    expect(component.pageSize).toBe(10);
    expect(mediaService.getMediaList).toHaveBeenCalledWith(1, 10);
  });

  it('should format date correctly', () => {
    const testDate = new Date('2024-01-15');
    const formattedDate = component.formatDate(testDate);
    
    expect(formattedDate).toBe('2024/01/15');
  });

  it('should identify video files correctly', () => {
    const videoFile = mockMediaListResponse.items[1];
    const imageFile = mockMediaListResponse.items[0];
    
    expect(component.isVideo(videoFile)).toBe(true);
    expect(component.isVideo(imageFile)).toBe(false);
  });

  it('should identify image files correctly', () => {
    const videoFile = mockMediaListResponse.items[1];
    const imageFile = mockMediaListResponse.items[0];
    
    expect(component.isImage(imageFile)).toBe(true);
    expect(component.isImage(videoFile)).toBe(false);
  });

  it('should handle image error', () => {
    const mockEvent = { target: { style: { display: '' } } };
    const mediaFile = mockMediaListResponse.items[0];
    
    component.onImageError(mockEvent, mediaFile);
    
    expect((mediaFile as any).hasImageError).toBe(true);
    expect(mockEvent.target.style.display).toBe('none');
  });

  it('should track media files by id', () => {
    const mediaFile = mockMediaListResponse.items[0];
    const trackId = component.trackByMediaId(0, mediaFile);
    
    expect(trackId).toBe(1);
  });
});
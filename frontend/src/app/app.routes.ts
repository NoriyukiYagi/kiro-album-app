import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/album',
    pathMatch: 'full'
  },
  {
    path: 'album',
    loadComponent: () => import('./components/album-list/album-list.component').then(m => m.AlbumListComponent)
  },
  {
    path: 'login',
    loadComponent: () => import('./components/login/login.component').then(m => m.LoginComponent)
  }
];
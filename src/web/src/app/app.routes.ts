import { Routes } from '@angular/router';
import { authGuard } from './core/auth/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    title: 'Sign in — SmartFactory AI',
    loadComponent: () => import('./features/login/login').then((m) => m.Login),
  },
  {
    path: '',
    pathMatch: 'full',
    title: 'SmartFactory AI',
    canActivate: [authGuard],
    loadComponent: () => import('./features/home/home').then((m) => m.Home),
  },
  {
    path: 'assets',
    title: 'Assets — SmartFactory AI',
    canActivate: [authGuard],
    loadComponent: () => import('./features/assets/asset-list').then((m) => m.AssetList),
  },
  {
    path: 'assets/:id',
    title: 'Asset — SmartFactory AI',
    canActivate: [authGuard],
    loadComponent: () => import('./features/assets/asset-detail').then((m) => m.AssetDetail),
  },
  {
    path: 'my-work',
    title: 'My work — SmartFactory AI',
    canActivate: [authGuard],
    loadComponent: () => import('./features/work/my-work').then((m) => m.MyWork),
  },
  {
    path: 'my-work/:id',
    title: 'Work order — SmartFactory AI',
    canActivate: [authGuard],
    loadComponent: () => import('./features/work/work-order').then((m) => m.WorkOrder),
  },
  { path: '**', redirectTo: '' },
];

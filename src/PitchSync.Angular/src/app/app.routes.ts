import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { LoginComponent } from './components/auth/login/login.component';
import { RegisterComponent } from './components/auth/register/register.component';
import { MatchBrowserComponent } from './components/match-browser/match-browser.component';
import { CreateMatchComponent } from './components/create-match/create-match.component';
import { MatchRoomComponent } from './components/match-room/match-room.component';
import { matchesResolver } from './resolvers/matches.resolver';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'matches', component: MatchBrowserComponent, canActivate: [authGuard], resolve: { rooms: matchesResolver } },
  { path: 'matches/new', component: CreateMatchComponent, canActivate: [authGuard] },
  { path: 'matches/:id', component: MatchRoomComponent, canActivate: [authGuard] },
  { path: '', redirectTo: 'matches', pathMatch: 'full' },
  { path: '**', redirectTo: 'matches' },
];

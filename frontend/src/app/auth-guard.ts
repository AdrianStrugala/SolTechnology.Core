import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot } from '@angular/router';
import { Observable } from 'rxjs';
import { LoginComponent } from './main-page/login/login.component';
import { UserService } from './user.service';
import { MatDialog } from '@angular/material/dialog';

@Injectable({
    providedIn: 'root'
})
export class AuthGuard implements CanActivate {

    constructor(private userService: UserService, public dialog: MatDialog) { }

    canActivate(
        next: ActivatedRouteSnapshot,
        state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean {

        //If user is not logged in - redirect back and show login modal
        if (!this.userService.isLoggedIn()) {
            if (this.dialog.openDialogs.length == 0) {
                this.dialog.open(LoginComponent, {
                    height: '35rem',
                    width: '30rem'
                });
            }
            else {
                this.dialog.closeAll();
            }
            return false;
        }

        return true;
    }
}
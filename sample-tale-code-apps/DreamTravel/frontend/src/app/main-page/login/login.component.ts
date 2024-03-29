import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { UserService } from '../../user.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { IUser } from '../../user.service'
import { Router } from '@angular/router';
import { handleError } from '../../shared/error';


@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent{

  loginForm = new FormGroup({
    email: new FormControl('', {
      validators:[Validators.required, Validators.email],
      updateOn: "blur"}),
    password: new FormControl(),
});

  error : string;

  constructor(
    public dialogRef: MatDialogRef<LoginComponent>, private userService: UserService, private router: Router) {}

    login(){
      this.userService.user.email = this.loginForm.value.email;
      this.userService.user.password = this.loginForm.value.password;

      this.userService.login()
      .subscribe(
        (data : IUser) => {
        this.userService.user = data;

      this.dialogRef.close();
      },
      (error) => {
        this.error = handleError(error);
      });
    }

  register(): void {

    this.router.navigate(["register"]);
    this.dialogRef.close();
  }
}

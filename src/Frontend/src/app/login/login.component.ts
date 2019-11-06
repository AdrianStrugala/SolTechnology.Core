import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { UserService } from '../user.service';
import { FormControl, FormGroup, Validators, FormBuilder } from '@angular/forms';
import { IUser } from '../user.service'


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
    public dialogRef: MatDialogRef<LoginComponent>, private userService: UserService) {}

    login(email: string, password: string){
      this.userService.user.Email = email;
      this.userService.user.Password = password;
      console.log(this.userService.user.Email)

      this.userService.login()
      .subscribe(
        (data : IUser) => {        
        this.userService.user = data;
      
      localStorage.setItem("user", JSON.stringify(this.userService.user))

      this.dialogRef.close();
      },
      (error) => {
        this.error = error.error
      });
    }

  register(): void {
    this.dialogRef.close();
  }
}

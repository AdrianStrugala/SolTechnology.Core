import { Component, Inject } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { UserService } from '../user.service';
import { FormControl, FormGroup, Validators, FormBuilder } from '@angular/forms';


@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss']
})
export class LoginComponent{

  loginForm = new FormGroup({
    email: new FormControl(),
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
        (data : number) => {        
        this.userService.user.Id = data;
      
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

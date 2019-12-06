import { Component } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';
import { UserService, IUser } from '../user.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent{

  registerForm = new FormGroup({
    email: new FormControl('', {
      validators: [Validators.required, Validators.email],
      updateOn: "blur"
    }),
    name: new FormControl(
      null,
      { validators: [Validators.required] }
    ),
    password: new FormControl(
      null,
      { validators: [Validators.required] }
    ),
    confirmPassword: new FormControl(
      null,
      { validators: [Validators.required] }
    ),
  });

  error : string;

  constructor(private userService: UserService) { }

  register(){
    let user: IUser;
    user.email = this.registerForm.value.email;
    user.password = this.registerForm.value.password;
    user.name = this.registerForm.value.name;

    this.userService.register(user)
    .subscribe(
      (data : boolean) => {        
    
    },
    (error) => {
      this.error = error.error
    });
  }
}

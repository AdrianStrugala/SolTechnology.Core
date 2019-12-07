import { Component } from "@angular/core";
import { FormGroup, FormControl, Validators } from "@angular/forms";
import { UserService, IUser } from "../user.service";
import { Router } from "@angular/router";

@Component({
  selector: "app-register",
  templateUrl: "./register.component.html",
  styleUrls: ["./register.component.scss"]
})
export class RegisterComponent {
  registerForm = new FormGroup({
    email: new FormControl("", {
      validators: [Validators.required, Validators.email],
      updateOn: "blur"
    }),
    name: new FormControl(null, { validators: [Validators.required] }),
    password: new FormControl(null, { validators: [Validators.required] }),
    confirmPassword: new FormControl(null, {
      validators: [Validators.required]
    })
  });

  error: string;

  constructor(private userService: UserService, private router: Router) {}

  register() {
    this.error = null;

    var user = {} as IUser;
    console.log(this.registerForm.value);
    user.email = this.registerForm.value.email;
    user.password = this.registerForm.value.password;
    user.name = this.registerForm.value.name;

    this.userService.register(user).subscribe(
      () => {
        this.router.navigate([""], { queryParams: { successMessage: "You are successfully registered!" } })
      },
      error => {
        this.error = error.error;
      }
    );
  }
}

import { Component } from "@angular/core";
import { FormGroup, FormControl, Validators } from "@angular/forms";
import { UserService, IUser } from "../user.service";
import { Router } from "@angular/router";
import { SuccessMessageService } from "../main-page/success-message/success-message.service";
import { confirmPasswordValidator } from "../shared/validators";
import { handleError } from "../shared/error";

@Component({
  selector: "app-register",
  templateUrl: "./register.component.html",
  styleUrls: ["./register.component.scss"]
})
export class RegisterComponent {

  registerForm = new FormGroup(
    {
      email: new FormControl("", {
        validators: [Validators.required, Validators.email],
        updateOn: "blur"
      }),
      name: new FormControl(null, { validators: [Validators.required] }),
      password: new FormControl(null, {
        validators: [
          Validators.required,
          Validators.minLength(8),
          Validators.maxLength(100)
        ]
      }),
      confirmPassword: new FormControl(null, {
        validators: [Validators.required]
      })
    },
    [confirmPasswordValidator]
  );

  error: string;
  registrationInProgress: boolean;

  constructor(
    private userService: UserService,
    private router: Router,
    private successMessageService: SuccessMessageService
  ) {
    this.registrationInProgress = false;
  }

  register() {
    this.registrationInProgress = true;
    this.error = null;

    var user = {} as IUser;
    user.email = this.registerForm.value.email;
    user.password = this.registerForm.value.password;
    user.name = this.registerForm.value.name;

    this.userService.register(user).subscribe(
      () => {
        this.successMessageService.set("You are successfully registered!");
        this.router.navigate([""]);
      },
      error => {
        this.error = handleError(error);
      }
    );

    this.registrationInProgress = false;
  }
}

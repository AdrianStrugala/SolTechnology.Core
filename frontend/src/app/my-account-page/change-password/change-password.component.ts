import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { UserService } from '../../user.service';
import { FormControl, FormGroup, Validators } from '@angular/forms';
import { handleError } from '../../shared/error';
import { confirmPasswordValidator } from '../../shared/validators';


@Component({
  selector: 'app-change-password',
  templateUrl: './change-password.component.html',
  styleUrls: ['./change-password.component.scss']
})
export class ChangePasswordComponent {

  changePasswordForm = new FormGroup({
    currentPassword: new FormControl('', {
      validators: [Validators.required],
      updateOn: "blur"
    }),
    password: new FormControl('', { validators: [Validators.required] }),
    confirmPassword: new FormControl('', { validators: [Validators.required] }),
  },
    [confirmPasswordValidator]);

  error: string;

  constructor(
    public dialogRef: MatDialogRef<ChangePasswordComponent>, private userService: UserService) { }

  changePassword() {
    this.userService.changePassword(this.changePasswordForm.value.currentPassword, this.changePasswordForm.value.password)
      .subscribe(
        () => {

          //TODO some success

          this.dialogRef.close();
        },
        (error) => {
          this.error = handleError(error);
        });
  }
}

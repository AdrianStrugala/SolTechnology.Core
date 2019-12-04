import { Component, OnInit } from '@angular/core';
import { FormGroup, FormControl, Validators } from '@angular/forms';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss']
})
export class RegisterComponent implements OnInit {

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

  constructor() { }

  ngOnInit() {
  }

}

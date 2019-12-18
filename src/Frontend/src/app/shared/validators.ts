import { FormGroup } from "@angular/forms";

export function confirmPasswordValidator(orderForm: FormGroup) {
  let password = orderForm.get("password").value;
  let confirmPassword = orderForm.get("confirmPassword").value;

  if (password != confirmPassword) {
    return { confirmPassword };
  }
  return null;
}

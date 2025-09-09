import { Component } from '@angular/core';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';

@Component({
  selector: 'app-contact-form',
  templateUrl: './contact-form.component.html',
  styleUrl: './contact-form.component.css'
})
export class ContactFormComponent {

  contactForm!: FormGroup;

  constructor(private fb: FormBuilder) {
    this.formBuilder();
  }

  private formBuilder() {
    debugger;
    this.contactForm = this.fb.group({
      name: ['',Validators.required],
      email: ['',[Validators.email,Validators.required]],
      password: ['', [
        Validators.pattern(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[!@#$%^&*])[a-zA-Z\d!@#$%^&*]{6,}$/), // Updated pattern
        Validators.minLength(6),
        Validators.required
      ]],
      message: ['',Validators.required]
    });
  }

  onSubmit() {
    debugger;
    if (this.contactForm.valid) {
      console.log(this.contactForm.value); // You can send this data to your backend or perform other actions
      this.contactForm.reset(); // Optional: Reset the form after submission
    } else {
      // Mark all form fields as touched to display validation errors
      this.markFormGroupTouched(this.contactForm);
    }
  }

  markFormGroupTouched(formGroup: FormGroup) {
    debugger;
    Object.values(formGroup.controls).forEach(control => {
      control.markAsTouched();
      if (control instanceof FormGroup) {
        this.markFormGroupTouched(control);
      }
    });
  }
}

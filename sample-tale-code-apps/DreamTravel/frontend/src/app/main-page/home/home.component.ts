import { ComponentRef, ComponentFactoryResolver, ViewContainerRef, ViewChild, Component } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { SuccessMessageService } from '../success-message/success-message.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ["./home.component.scss"]
})
export class HomeComponent {
  title = 'Dream Travel';

  @ViewChild('viewContainerRef', { read: ViewContainerRef, static: false }) VCR: ViewContainerRef;

  success: string;
  // to store references of dynamically created components
  componentsReferences = [];

  page: number;

  constructor(private CFR: ComponentFactoryResolver, public successMessageService: SuccessMessageService) {
  }

}

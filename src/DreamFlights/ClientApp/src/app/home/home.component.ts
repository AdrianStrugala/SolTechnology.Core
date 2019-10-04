import { ComponentRef, ComponentFactoryResolver, ViewContainerRef, ViewChild, Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  title = 'Dream Flights';

  @ViewChild('viewContainerRef', { read: ViewContainerRef }) VCR: ViewContainerRef;


  // to store references of dynamically created components
  componentsReferences = [];

  constructor(private CFR: ComponentFactoryResolver) {
  }
}

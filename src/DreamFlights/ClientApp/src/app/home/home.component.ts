import { ComponentRef, ComponentFactoryResolver, ViewContainerRef, ViewChild, Component } from '@angular/core';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  title = 'Dream Flights';

  @ViewChild('viewContainerRef', { read: ViewContainerRef }) VCR: ViewContainerRef;

  //manually indexing the child components for better removal
  //although there is by-default indexing but it is being avoid for now
  //so index is a unique property here to identify each component individually.
  index: number = 0;

  // to store references of dynamically created components
  componentsReferences = [];

  constructor(private CFR: ComponentFactoryResolver) {
  }
}

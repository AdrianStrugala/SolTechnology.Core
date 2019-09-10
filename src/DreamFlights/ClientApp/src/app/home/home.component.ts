import { ComponentRef, ComponentFactoryResolver, ViewContainerRef, ViewChild, Component } from '@angular/core';
import { WorkerComponent } from "../worker/worker.component";


@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent {
  title = 'Aurë Swarm';

  @ViewChild('viewContainerRef', { read: ViewContainerRef }) VCR: ViewContainerRef;

  //manually indexing the child components for better removal
  //although there is by-default indexing but it is being avoid for now
  //so index is a unique property here to identify each component individually.
  index: number = 0;

  // to store references of dynamically created components
  componentsReferences = [];

  constructor(private CFR: ComponentFactoryResolver) {
  }

  addWorker() {

    let componentFactory = this.CFR.resolveComponentFactory(WorkerComponent);
    let componentRef: ComponentRef<WorkerComponent> = this.VCR.createComponent(componentFactory);
    let currentComponent = componentRef.instance;

    currentComponent.selfRef = currentComponent;
    currentComponent.index = ++this.index;

    // prividing parent Component reference to get access to parent class methods
    currentComponent.compInteraction = this;

    // add reference for newly created component
    this.componentsReferences.push(componentRef);
  }

  deleteWorker(index: number) {

    if (this.VCR.length < 1)
      return;

    let componentRef = this.componentsReferences.filter(x => x.instance.index == index)[0];
    let component: WorkerComponent = <WorkerComponent>componentRef.instance;

    let vcrIndex: number = this.VCR.indexOf(componentRef);

    // removing component from container
    this.VCR.remove(vcrIndex);

    this.componentsReferences = this.componentsReferences.filter(x => x.instance.index !== index);
  }
}

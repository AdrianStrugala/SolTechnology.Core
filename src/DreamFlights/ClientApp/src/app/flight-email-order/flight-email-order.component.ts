import { ComponentRef, ComponentFactoryResolver, ViewContainerRef, ViewChild, Component } from '@angular/core';

@Component({
  selector: 'flight-email-order',
  templateUrl: './flight-email-order.component.html',
  styleUrls: ['./flight-email-order.component.scss'],
})
export class FlightEmailOrderComponent {

  @ViewChild('viewContainerRef', { read: ViewContainerRef }) VCR: ViewContainerRef;

}

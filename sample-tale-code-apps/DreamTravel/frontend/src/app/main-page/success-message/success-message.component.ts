import { Component, Input, OnDestroy } from '@angular/core';
import { SuccessMessageService } from './success-message.service';

@Component({
  selector: 'app-success-message',
  templateUrl: './success-message.component.html',
  styleUrls: ['./success-message.component.scss']
})
export class SuccessMessageComponent implements OnDestroy {

  
  @Input() success: string;

  constructor(private successMessageService: SuccessMessageService) { }

  ngOnDestroy(): void {
    this.successMessageService.reset();
  }
}

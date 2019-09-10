import { Component, Input } from '@angular/core';

@Component({
  selector: 'worker',
  templateUrl: './worker.component.html',
  styleUrls: ['./worker.component.css']
})
export class WorkerComponent  {
  @Input() name: string;

  public index: number;
  public selfRef: WorkerComponent;

  //interface for Parent-Child interaction
  public compInteraction: IWorker;

  constructor() {
  }

  removeMe(index) {
    this.compInteraction.deleteWorker(index);
  }
}

// Interface
export interface IWorker {
  deleteWorker(index: number);
}

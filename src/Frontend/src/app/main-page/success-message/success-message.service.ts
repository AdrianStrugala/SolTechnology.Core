import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class SuccessMessageService {

  message: string;

  constructor() {
  }

  set(message: string){
    this.message = message;
  }

  get(){
    return this.message;
  }

reset(){
  if (this.message != null){
    this.message = null;
  }
}
}

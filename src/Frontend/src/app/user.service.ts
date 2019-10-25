import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class UserService {

  public Id : number;
  public Email: string;
  public Password : string;

  constructor() { }

  login() : string {

    return 'dupa';
  } 
}

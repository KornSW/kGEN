/* Generated From 1.0.0.0
   WARNING: dont edit this code */

import { Observable, Subscription, Subject, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators';

import * as DTOs from 'my-contract-module/dtos';
import * as Models from 'my-contract-module/models';
import * as Interfaces from 'my-contract-module/interfaces';


export class FooClient {
  
  constructor(
    private rootUrlResolver: () => string,
    private apiTokenResolver: () => string,
    private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Observable<any>
  ){}
  
  private getEndpointUrl(): string {
    let rootUrl = this.rootUrlResolver();
    if(rootUrl.endsWith('/')){
      return rootUrl + 'foo/';
    }
    else{
      return rootUrl + '/foo/';
    }
  }
  
  
  /**
   * Foooo
   */
  public foooo(): Observable<{b: number, return: boolean}> {
    
    let requestWrapper : DTOs.FooooRequest = {
    };
    
    let url = this.getEndpointUrl() + 'foooo';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).pipe(map(
      (r) => {
        let responseWrapper = (r as DTOs.FooooResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return {b: responseWrapper.b, return: responseWrapper.return};
      }
    ));
  }
  
  /**
   * Kkkkkk
   */
  public kkkkkk(): Observable<Models.TestModel> {
    
    let requestWrapper : DTOs.KkkkkkRequest = {
    };
    
    let url = this.getEndpointUrl() + 'kkkkkk';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).pipe(map(
      (r) => {
        let responseWrapper = (r as DTOs.KkkkkkResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return responseWrapper.return;
      }
    ));
  }
  
  /**
   * Meth
   */
  public aVoid(): Observable<void> {
    
    let requestWrapper : DTOs.AVoidRequest = {
    };
    
    let url = this.getEndpointUrl() + 'aVoid';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).pipe(map(
      (r) => {
        let responseWrapper = (r as DTOs.AVoidResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return;
      }
    ));
  }
  
  /**
   * TestNullableDt
   */
  public testNullableDt(): Observable<boolean> {
    
    let requestWrapper : DTOs.TestNullableDtRequest = {
    };
    
    let url = this.getEndpointUrl() + 'testNullableDt';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).pipe(map(
      (r) => {
        let responseWrapper = (r as DTOs.TestNullableDtResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return responseWrapper.return;
      }
    ));
  }
  
}

export class DemoConnector {
  
  private fooClient: FooClient;
  
  constructor(
    private rootUrlResolver: () => string,
    private apiTokenResolver: () => string,
    private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Observable<any>
  ){
  
    this.fooClient = new FooClient(this.rootUrlResolver, this.apiTokenResolver, this.httpPostMethod);
    
  }
  
  private getRootUrl(): string {
    let rootUrl = this.rootUrlResolver();
    if(rootUrl.endsWith('/')){
      return rootUrl;
    }
    else{
      return rootUrl + '/';
    }
  }
  
  get foo(): FooClient { return this.fooClient }
  
}

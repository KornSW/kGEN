/* Generated From 1.0.0.0
   WARNING: dont edit this code */

import { axios, AxiosInstance } from 'axios';

import * as DTOs from 'my-contract-module/dtos';
import * as Models from 'my-contract-module/models';
import * as Interfaces from 'my-contract-module/interfaces';

export class FooClient {
  
  constructor(
    private rootUrlResolver: () => string,
    private apiTokenResolver: () => string,
    private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Promise<any>
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
  public foooo(a: string): Promise<{b: number, return: boolean}> {
    
    let requestWrapper : DTOs.FooooRequest = {
      a: a,
    };
    
    let url = this.getEndpointUrl() + 'foooo';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).then(
      (r) => {
        let responseWrapper = (r as DTOs.FooooResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return {b: responseWrapper.b, return: responseWrapper.return};
      }
    );
  }
  
  /**
   * Kkkkkk
   */
  public kkkkkk(optParamA: number = 0, optParamB: string = 'f'): Promise<Models.TestModel> {
    
    let requestWrapper : DTOs.KkkkkkRequest = {
      optParamA: optParamA,
      optParamB: optParamB
    };
    
    let url = this.getEndpointUrl() + 'kkkkkk';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).then(
      (r) => {
        let responseWrapper = (r as DTOs.KkkkkkResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return responseWrapper.return;
      }
    );
  }
  
  /**
   * Meth
   */
  public aVoid(errorCode: Models.TestModel): Promise<void> {
    
    let requestWrapper : DTOs.AVoidRequest = {
      errorCode: errorCode
    };
    
    let url = this.getEndpointUrl() + 'aVoid';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).then(
      (r) => {
        let responseWrapper = (r as DTOs.AVoidResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return;
      }
    );
  }
  
  /**
   * TestNullableDt
   */
  public testNullableDt(dt: Date): Promise<boolean> {
    
    let requestWrapper : DTOs.TestNullableDtRequest = {
      dt: dt
    };
    
    let url = this.getEndpointUrl() + 'testNullableDt';
    return this.httpPostMethod(url, requestWrapper, this.apiTokenResolver()).then(
      (r) => {
        let responseWrapper = (r as DTOs.TestNullableDtResponse);
        if(responseWrapper.fault){
          console.warn('Request to "' + url + '" faulted: ' + responseWrapper.fault);
          throw {message: responseWrapper.fault};
        }
        return responseWrapper.return;
      }
    );
  }
  
}

export class DemoConnector {
  
  private fooClient: FooClient;
  
  private axiosHttpApi: AxiosInstance;
  
  constructor(
    private rootUrlResolver: () => string,
    private apiTokenResolver: () => string,
    private httpPostMethod: (url: string, requestObject: any, apiToken: string) => Promise<any>
  ){
  
    if (this.httpPostMethod == null) {
      this.axiosHttpApi = axios.create({ baseURL: this.rootUrlResolver() });
      this.httpPostMethod = (url, requestObject, apiToken) => {
        return this.axiosHttpApi.post(url, requestObject, {
          headers: {
            Authorization: apiToken
          }
        });
      };
    }
    
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

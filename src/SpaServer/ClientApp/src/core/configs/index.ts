/* eslint-disable @typescript-eslint/no-unsafe-return */
/* eslint-disable @typescript-eslint/no-unsafe-member-access */
/* eslint-disable @typescript-eslint/no-unsafe-assignment */
/* eslint-disable @typescript-eslint/no-explicit-any */
import { I18nConfig } from './I18nConfig';
import { ApiEndpointsConfig } from './ApiEndpointsConfig';
import appsettings from '../../appsettings.json';

class Configurations {
  public title!: string;
  public initialRoutePath!: string;
  public endpoints!: ApiEndpointsConfig;
  public i18n!: I18nConfig;
  public debug!: boolean;

  constructor() {
    this.debug = process.env.NODE_ENV !== 'production';
    Object.keys(appsettings).forEach(key => {
      const property = Object.getOwnPropertyDescriptor(appsettings, key);
      this.loadConfiguration(key, property);
    });
  }

  private loadConfiguration(key: string, property: any): void {
    property.value = this.getEnvironmentVariable('', key, property);
    Object.defineProperty(this, key, property);
  }

  private getEnvironmentVariable(parentKey: string, key: string, property: any): any {
    if (this.isObject(property.value)) {
      Object.keys(property.value).forEach(innerKey => {
        const innerProp = Object.getOwnPropertyDescriptor(property.value, innerKey);
        if (innerProp) {
          innerProp.value = this.getEnvironmentVariable(parentKey ? `${parentKey}__${key}` : key, innerKey, innerProp);
          Object.defineProperty(property.value, innerKey, innerProp);
        }
      });
      return property.value;
    }

    let envKey = parentKey ? `${parentKey}__${key}` : key;

    if (!envKey.startsWith('VUE_APP')) envKey = `VUE_APP__${envKey}`;

    if (window) return (window as any)[envKey] || process.env[envKey] || property.value;

    return process.env[envKey] || property.value;
  }

  private isObject = (value: any): boolean => value === Object(value);
}

export default new Configurations();

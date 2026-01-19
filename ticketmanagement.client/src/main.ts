import { bootstrapApplication } from '@angular/platform-browser';
import { provideAnimations } from '@angular/platform-browser/animations';
// import { registerLicense } from '@syncfusion/ej2-base';

import { AppComponent } from './app/app.component';

// TODO: replace with your Syncfusion license key.
// registerLicense('YOUR SYNCFUSION LICENSE KEY');

bootstrapApplication(AppComponent).catch(err => console.error(err));
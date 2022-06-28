import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';

import { AppComponent } from './app.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';

import { MatTabsModule } from '@angular/material/tabs';
import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatCardModule } from '@angular/material/card';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { SelectChannelDialogComponent } from './select-channel-dialog/select-channel-dialog.component';
import { ChannelComponent } from './channel/channel.component';
import { CreateChannelDialogComponent } from './create-channel-dialog/create-channel-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    SelectChannelDialogComponent,
    ChannelComponent,
    CreateChannelDialogComponent,
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    MatTabsModule,
    MatDialogModule,
    MatButtonModule,
    MatListModule,
    MatCardModule,
    MatInputModule,
    MatIconModule,
    MatProgressSpinnerModule,
  ],
  providers: [],
  bootstrap: [AppComponent],
})
export class AppModule { }

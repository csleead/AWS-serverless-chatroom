import { Channel } from './../dto/channel';
import { BackendCommunicationService } from '../backend-communication.service';
import { Component } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';

@Component({
  selector: 'app-select-channel-dialog',
  templateUrl: './select-channel-dialog.component.html',
  styleUrls: ['./select-channel-dialog.component.sass'],
})
export class SelectChannelDialogComponent {
  channels$: Promise<Channel[]>;
  selectedChannel?: Channel;

  public constructor(private backendService: BackendCommunicationService, private dialogRef: MatDialogRef<SelectChannelDialogComponent>) {
    this.channels$ = backendService.listChannels().then(data => data.channels);
  }

  joinChannel(channel: Channel) {
    this.dialogRef.close(channel);
  }
}

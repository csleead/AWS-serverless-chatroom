import { Component, OnInit, ɵsetCurrentInjector } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { BackendCommunicationService } from '../backend-communication.service';
import { Channel } from '../dto/channel';

@Component({
  selector: 'app-create-channel-dialog',
  templateUrl: './create-channel-dialog.component.html',
  styleUrls: ['./create-channel-dialog.component.sass'],
})
export class CreateChannelDialogComponent {
  channelName = '';
  loading = false;

  public constructor(
    private dialogRef: MatDialogRef<CreateChannelDialogComponent>,
    private backendService: BackendCommunicationService,
  ) {
  }

  async create() {
    if(this.channelName.length === 0) {
      return;
    }

    this.loading = true;

    try {
      const response = await this.backendService.createChannel(this.channelName);
      const channel: Channel = {
        id: response.channelId,
        name: this.channelName,
      };

      this.dialogRef.close(channel);
    }
    finally {
      this.loading = false;
    }
  }

  cancel() {
    this.dialogRef.close();
  }
}

import { CreateChannelDialogComponent } from './create-channel-dialog/create-channel-dialog.component';
import { Channel } from './dto/channel';
import { BackendCommunicationService } from './backend-communication.service';
import { SelectChannelDialogComponent } from './select-channel-dialog/select-channel-dialog.component';
import { Component, OnInit } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.sass'],
})
export class AppComponent implements OnInit {
  initialized = false;
  connectionId!: string;
  joinedChannels: Channel[] = [];
  selected = 0;

  constructor(
    private backendService: BackendCommunicationService,
    private dialog: MatDialog,
  ) {
  }

  async ngOnInit(): Promise<void> {
    const connectionInfo = await this.backendService.getConnectionInfo();
    this.connectionId = connectionInfo.connectionId;
    this.initialized = true;
  }

  openSelectChannelDialog() {
    const dialogRef = this.dialog.open(SelectChannelDialogComponent, {
      width: '500px',
    });

    dialogRef.afterClosed().subscribe((channel: Channel | undefined) => {
      if(channel) {
        this.joinedChannels.push(channel);
        this.selected = this.joinedChannels.length - 1;
      }
    });
  }

  openCreateChannelDialog() {
    const dialogRef = this.dialog.open(CreateChannelDialogComponent, {
      width: '250px',
    });

    dialogRef.afterClosed().subscribe((channel: Channel | undefined) => {
      if(channel) {
        this.joinedChannels.push(channel);
        this.selected = this.joinedChannels.length - 1;
      }
    });
  }

  async leaveChannelButtonClicked(c: Channel) {
    if(confirm(`You want to leave channel ${c.name}?`)){
      await this.backendService.leaveChannel(c.id);
      const index = this.joinedChannels.indexOf(c);
      this.joinedChannels.splice(index, 1);
    }
  }
}

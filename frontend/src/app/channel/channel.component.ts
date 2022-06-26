import { BackendCommunicationService } from './../backend-communication.service';
import { ChannelMessage } from './channel-message';
import { Channel, MessageDto } from './../dto/channel';
import { Component, Input, OnInit, OnDestroy } from '@angular/core';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-channel',
  templateUrl: './channel.component.html',
  styleUrls: ['./channel.component.sass'],
})
export class ChannelComponent implements OnInit, OnDestroy {
  @Input() channel!: Channel;
  @Input() connectionId!: string;

  messages: ChannelMessage[] = [];
  initializing = true;
  newMessagesSubscription!: Subscription;
  textAreaMessage = '';
  sendingMessage = false;

  constructor(private backendService: BackendCommunicationService) {
  }

  async ngOnInit() {
    if(!this.channel || !this.connectionId) {
      throw new Error('Missing required input for ChannelComponent');
    }

    const { messages } = await this.backendService.fetchMessages(this.channel.id, 50);
    this.messages = messages.map(m => ({
      fromConnection: m.fromConnection,
      sequence: m.sequence,
      content: m.content,
      isMyMessage: m.fromConnection === this.connectionId,
      time: new Date(m.time),
    }));

    await this.backendService.joinChannel(this.channel.id);

    this.newMessagesSubscription = this.backendService.subscribeMessagesOfChannel(this.channel.id)
      .subscribe(data => {
        this.onNewMessage(data);
      });

    this.initializing = false;
  }

  ngOnDestroy(): void {
    this.newMessagesSubscription?.unsubscribe();
  }

  async sendMessage() {
    this.sendingMessage = true;
    const response = await this.backendService.sendMessage(this.channel.id, this.textAreaMessage);
    if(response.result === 'success') {
      this.onNewMessage(response.message);
    } else {
      alert('Message sending failed');
    }

    this.sendingMessage = false;
    this.textAreaMessage = '';
  }

  onNewMessage(msgDto: MessageDto) {
    const msg: ChannelMessage = {
      fromConnection: msgDto.fromConnection,
      sequence: msgDto.sequence,
      content: msgDto.content,
      isMyMessage: msgDto.fromConnection === this.connectionId,
      time: new Date(msgDto.time),
    };

    this.messages.push(msg);
  }
}

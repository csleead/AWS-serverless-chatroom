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
      await this.onNewMessage(response.message);
    } else {
      alert('Message sending failed');
    }

    this.sendingMessage = false;
    this.textAreaMessage = '';
  }

  async onNewMessage(msgDto: MessageDto) {
    const msg = mapMessageFromDto(msgDto, this.connectionId);
    if(this.messages.length === 0) {
      this.messages.push(msg);
      return;
    }

    const currentLatestSequence = this.messages[this.messages.length - 1].sequence;
    if(msgDto.sequence <= currentLatestSequence) {
      // we already have that message, simply discard it.
      return;
    }

    if(currentLatestSequence + 1 !== msg.sequence) {
      // There is a "gap" in messages, fill it
      const { messages } = await this.backendService.fetchMessages(this.channel.id, msg.sequence - currentLatestSequence - 1, msg.sequence - 1);
      this.messages = [...this.messages, ...messages.map(m => mapMessageFromDto(m, this.connectionId))];
    }

    this.messages.push(msg);
  }
}

function mapMessageFromDto(msgDto: MessageDto, myConnectionId: string): ChannelMessage {
  return {
    fromConnection: msgDto.fromConnection,
    sequence: msgDto.sequence,
    content: msgDto.content,
    isMyMessage: msgDto.fromConnection === myConnectionId,
    time: new Date(msgDto.time),
  };
}

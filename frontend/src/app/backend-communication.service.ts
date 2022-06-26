import { Injectable } from '@angular/core';
import {  CreateChannelResponseData, GetConnectionInfoResponseData, ListChannelsResponseData, FetchMessagesResponseData, JoinChannelResponseData, MessageDto, SendMessageResponseData } from './dto/channel';
import { Observable, Subject, firstValueFrom, BehaviorSubject } from 'rxjs';
import { filter, map } from 'rxjs/operators';

@Injectable({
  providedIn: 'root',
})
export class BackendCommunicationService {
  readonly socket: WebSocket;
  readonly onMessage$: Observable<Message>;
  readonly socketStatus$: BehaviorSubject<'opening' | 'opened' |'closed'>;

  constructor() {
    this.socketStatus$ = new BehaviorSubject<'opening' | 'opened' |'closed'>('opening');

    this.socket = new WebSocket('wss://edmsfe29m9.execute-api.ap-east-1.amazonaws.com/prod');

    this.socket.addEventListener('open', () => {
      this.socketStatus$.next('opened');
    });

    this.socket.addEventListener('close', () => {
      this.socketStatus$.next('closed');
    });

    const subject = new Subject<Message>();
    this.socket.addEventListener('message', e => {
      subject.next(JSON.parse(e.data));
    });
    this.onMessage$ = subject;
  }

  public async getConnectionInfo(): Promise<GetConnectionInfoResponseData> {
    return this.makeRequest<GetConnectionInfoResponseData>({
      action: 'getConnectionInfo',
    });
  }

  public async listChannels(): Promise<ListChannelsResponseData> {
    return this.makeRequest<ListChannelsResponseData>({
      action: 'listChannels',
    });
  }

  public createChannel(channelName: string): Promise<CreateChannelResponseData> {
    return this.makeRequest<CreateChannelResponseData>({
      action: 'createChannel',
      channelName,
    });
  }

  public fetchMessages(channelId: string, takeLast: number, maxSequence?: number): Promise<FetchMessagesResponseData> {
    return this.makeRequest<FetchMessagesResponseData>({
      action: 'fetchMessages',
      channelId,
      takeLast,
      maxSequence,
    });
  }

  public joinChannel(channelId: string): Promise<JoinChannelResponseData> {
    return this.makeRequest<JoinChannelResponseData>({
      action: 'joinChannel',
      channelId,
    });
  }

  public leaveChannel(channelId: string): Promise<void> {
    return this.makeRequest<void>({
      action: 'leaveChannel',
      channelId,
    });
  }

  public sendMessage(channelId: string, content: string): Promise<SendMessageResponseData> {
    return this.makeRequest<SendMessageResponseData>({
      action: 'sendMessage',
      channelId,
      content,
    });
  }

  public subscribeMessagesOfChannel(channelId: string): Observable<MessageDto> {
    return this.onMessage$.pipe(
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      filter(m => m.type === 'newMessage' && (m.data as any).channelId === channelId),
      map(m => m.data as MessageDto),
    );
  }

  private async makeRequest<TResponseData>(payloadExceptId: Record<string, unknown>): Promise<TResponseData> {
    await this.ensureWebsocketIsReady();
    const messageId = newMessageId();

    const data$ = this.onMessage$.pipe(
      filter((m) => (m.messageId === messageId)),
      map(m => m.data)
    );

    const promise = firstValueFrom(data$)
      .then(data => data as TResponseData);

    this.socket.send(JSON.stringify({
      messageId,
      ...payloadExceptId,
    }));

    return promise;
  }

  private ensureWebsocketIsReady(): Promise<unknown> {
    const status = this.socketStatus$.value;
    if(status === 'opened') {
      return Promise.resolve();
    }

    if(status === 'opening') {
      return firstValueFrom(this.socketStatus$.pipe(filter(sts => sts === 'opened')));
    }

    throw new Error('The socket is closed!');
  }
}

function newMessageId(): string {
  return Math.random().toString(16).substring(2, 14);
}

interface Message {
  messageId?: string;
  message?: string;
  data?: unknown;
  type?: string;
}
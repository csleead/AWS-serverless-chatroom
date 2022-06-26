export interface Channel {
  id: string;
  name: string;
}

export interface ListChannelsResponseData {
  channels: Channel[];
}

export interface GetConnectionInfoResponseData {
  connectionId: string;
}

export interface CreateChannelResponseData {
  channelId: string;
}

export interface FetchMessagesResponseData {
  messages: MessageDto[];
}

export interface JoinChannelResponseData {
  result: 'success' | 'channelNotFound';
}

export type SendMessageResponseData = {  result: 'success'; message: MessageDto; } | { result: 'channelNotFound' };

export interface MessageDto {
  channelId: string;
  sequence: number;
  content: string;
  fromConnection: string;
  time: string;
}


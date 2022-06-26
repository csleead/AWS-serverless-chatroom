export interface ChannelMessage {
  fromConnection: string;
  content: string;
  sequence: number;
  time: Date;
  isMyMessage: boolean;
}
import { Channel } from './../dto/channel';
import { Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'app-channel',
  template: `
    <p>
      This is channel {{ channel!.name }}
    </p>
  `,
  styles: [
  ],
})
export class ChannelComponent {
  @Input() channel?: Channel;
}

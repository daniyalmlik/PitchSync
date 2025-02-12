import { Pipe, PipeTransform } from '@angular/core';
import { MatchEventType } from '../models/event.model';

const ICON_MAP: Record<MatchEventType, string> = {
  Goal: 'sports_soccer',
  OwnGoal: 'sports_soccer',
  Assist: 'assistant',
  YellowCard: 'style',
  RedCard: 'style',
  Substitution: 'swap_horiz',
  Penalty: 'sports_soccer',
  PenaltyMiss: 'close',
  VAR: 'videocam',
  Injury: 'local_hospital',
  HalfTime: 'pause',
  FullTime: 'flag',
  KickOff: 'play_arrow',
  FreeKick: 'sports_soccer',
  Corner: 'flag',
  Save: 'pan_tool',
  Comment: 'comment',
};

@Pipe({ name: 'eventIcon', standalone: true })
export class EventIconPipe implements PipeTransform {
  transform(eventType: MatchEventType): string {
    return ICON_MAP[eventType] ?? 'circle';
  }
}

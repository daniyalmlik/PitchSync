import { Pipe, PipeTransform } from '@angular/core';
import { MatchEventType } from '../models/event.model';

const EMOJI_MAP: Record<MatchEventType, string> = {
  Goal: '⚽',
  OwnGoal: '⚽🔴',
  Assist: '👟',
  YellowCard: '🟨',
  RedCard: '🟥',
  Substitution: '🔄',
  Penalty: '⚽(P)',
  PenaltyMiss: '❌',
  VAR: '📺',
  Injury: '🏥',
  HalfTime: '⏸️',
  FullTime: '🏁',
  KickOff: '📣',
  Comment: '💬',
  FreeKick: '🦶',
  Corner: '🚩',
  Save: '🧤',
};

@Pipe({ name: 'eventIcon', standalone: true })
export class EventIconPipe implements PipeTransform {
  transform(eventType: MatchEventType): string {
    return EMOJI_MAP[eventType] ?? '•';
  }
}

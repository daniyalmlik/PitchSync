import { EventIconPipe } from './event-icon.pipe';

describe('EventIconPipe', () => {
  let pipe: EventIconPipe;

  beforeEach(() => {
    pipe = new EventIconPipe();
  });

  it('Maps_Goal_To_Soccer_Emoji', () => {
    expect(pipe.transform('Goal')).toBe('⚽');
  });

  it('Maps_YellowCard', () => {
    expect(pipe.transform('YellowCard')).toBe('🟨');
  });

  it('Maps_Unknown_ToDefault', () => {
    expect(pipe.transform('9999' as any)).toBe('•');
  });
});

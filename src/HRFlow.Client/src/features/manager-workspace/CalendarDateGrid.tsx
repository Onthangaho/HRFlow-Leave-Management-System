import { useEffect, useId, useRef, useState, type RefObject } from 'react';
import type { TeamLeave } from '../team-leave/api';
import { activeAbsent, calendarKeyDate, calendarRange, fullDateLabel, iso, monthLabel } from './calendar';

const weekdays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

/** One tab stop with explicit selection; background queries never unmount focused date controls. */
export function CalendarDateGrid({ month, range, today, selected, entries, onSelect, mobileInput }: {
  month: Date;
  range: ReturnType<typeof calendarRange>;
  today: string;
  selected: string | null;
  entries: TeamLeave[] | undefined;
  onSelect: (date: string) => void;
  mobileInput: RefObject<HTMLInputElement | null>;
}) {
  const [focused, setFocused] = useState(() => selected ?? (range.days.includes(today) ? today : iso(month)));
  const grid = useRef<HTMLDivElement>(null);
  const ownsFocus = useRef(false);
  const buttons = useRef(new Map<string, HTMLButtonElement>());
  const helpId = useId();
  useEffect(() => {
    const desktop = window.matchMedia('(min-width: 40rem)');
    const update = () => {
      // Only repair focus that would otherwise remain in a newly hidden grid; widening never steals it.
      // Chromium may blur a CSS-hidden button before delivering the media change event.
      if (!desktop.matches && (ownsFocus.current || grid.current?.contains(document.activeElement))) {
        mobileInput.current?.focus();
        ownsFocus.current = false;
      }
    };
    desktop.addEventListener('change', update);
    return () => desktop.removeEventListener('change', update);
  }, [mobileInput]);

  return <div className="hidden sm:block">
    <p id={helpId} className="mb-3 text-sm">Keyboard: arrows move one day or week; Home/End move to Monday/Sunday. Enter or Space selects a date. Tab leaves the calendar. Movement stops at the displayed range; use month controls for another month.</p>
    <div ref={grid} role="grid" aria-label={`Team dates, ${monthLabel(month)}`} aria-describedby={helpId} aria-readonly="true" className="space-y-1"
      onFocusCapture={() => { ownsFocus.current = true; }}
      onBlurCapture={event => {
        if (event.currentTarget.contains(event.relatedTarget)) return;
        if (!event.relatedTarget && !window.matchMedia('(min-width: 40rem)').matches) mobileInput.current?.focus();
        ownsFocus.current = false;
      }}>
      <div role="row" className="grid grid-cols-7 gap-1 text-center text-sm">
        {weekdays.map(day => <div role="columnheader" aria-label={day} key={day}>{day.slice(0, 3)}</div>)}
      </div>
      {Array.from({ length: range.days.length / 7 }, (_, week) => <div role="row" className="grid grid-cols-7 gap-1" key={week}>
        {range.days.slice(week * 7, week * 7 + 7).map(day => {
          const count = entries ? activeAbsent(entries, day) : undefined;
          return <div role="gridcell" aria-selected={selected === day} key={day} className="min-w-0">
            <button type="button" ref={button => { if (button) buttons.current.set(day, button); else buttons.current.delete(day); }}
              data-calendar-date={day} tabIndex={focused === day ? 0 : -1}
              aria-current={today === day ? 'date' : undefined}
              aria-label={`${fullDateLabel(day)}, ${count === undefined ? 'absence count unavailable' : `${count} active people with Approved leave`}${day.slice(0, 7) !== iso(month).slice(0, 7) ? ', adjacent month' : ''}`}
              className={`ui-secondary team-calendar-date w-full min-w-0 flex-col px-1 py-3 ${selected === day ? 'team-calendar-selected' : ''}`}
              onFocus={() => setFocused(day)} onClick={() => onSelect(day)}
              onKeyDown={event => {
                if (event.altKey || event.metaKey || event.shiftKey) return;
                if (event.ctrlKey && event.key !== 'Home' && event.key !== 'End') return;
                const target = calendarKeyDate(day, event.key, range.start, range.end, event.ctrlKey);
                if (!target) return;
                event.preventDefault();
                setFocused(target);
                buttons.current.get(target)?.focus();
              }}>
              <span>{Number(day.slice(8))}{today === day && <span className="ml-1 text-xs">Today</span>}</span>
              <span className="block text-xs">{count === undefined ? 'Unavailable' : `${count} absent`}</span>
              <span className="block min-h-4 text-xs">{selected === day ? 'Selected' : '\u00a0'}</span>
            </button>
          </div>;
        })}
      </div>)}
    </div>
  </div>;
}

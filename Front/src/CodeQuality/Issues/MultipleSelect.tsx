import React from 'react';
import { Button, Checkbox, Gapped, MenuSeparator, Tooltip } from '@skbkontur/react-ui';
import { ArrowCDownIcon16Regular } from '@skbkontur/icons/ArrowCDownIcon16Regular';

interface MultipleSelectProps {
  caption?: string;
  items: string[];
  selected: string[];
  onChange: (items: string[]) => void;
}

export function MultipleSelect({ caption, items, selected, onChange }: MultipleSelectProps) {
  const renderTooltip = (): JSX.Element => (
    <Gapped vertical gap={8} style={{ maxHeight: 550, overflow: 'auto', minWidth: 200 }}>
      <Checkbox checked={selected.length === 0} onValueChange={(_) => onChange([])}>
        All
      </Checkbox>
      <MenuSeparator />
      {items.map((item) => (
        <Checkbox
          key={item}
          checked={selected.includes(item)}
          onValueChange={(val) =>
            val ? onChange([...selected, item]) : onChange(selected.filter((x) => x !== item))
          }
        >
          {item}
        </Checkbox>
      ))}
    </Gapped>
  );

  const captionElements =
    selected.length === 0 ? 'All' : selected.length === 1 ? selected[0] : `${selected[0]}, ...`;

  return (
    <Tooltip
      render={renderTooltip}
      trigger="click"
      pos="bottom left"
      useWrapper
      closeButton={false}
    >
      <Button rightIcon={<ArrowCDownIcon16Regular />}>
        {caption}: {captionElements}
      </Button>
    </Tooltip>
  );
}

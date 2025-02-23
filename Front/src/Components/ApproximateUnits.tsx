import React, { ReactElement } from "react";

interface Unit {
    name: string;
    value: number;
}

export const ITEMS_UNITS: Unit[] = [
    { name: "M", value: 1000 * 1000 },
    { name: "k", value: 1000 },
    { name: "", value: 1 },
];
export const TIME_UNITS: Unit[] = [
    { name: "h", value: 1000 * 60 * 60 },
    { name: "m", value: 1000 * 60 },
    { name: "s", value: 1000 },
    { name: "ms", value: 1 },
];

export function ApproximateUnits({ value, units }: { value: number; units: Unit[] }): ReactElement {
    const unit = units.find(u => value >= u.value) ?? units[units.length - 1];
    return (
        <>
            {Math.round(value / unit.value)}
            {unit.name}
        </>
    );
}

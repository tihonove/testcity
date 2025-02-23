import React from "react";
import { QuestionCircleIcon16Solid } from "@skbkontur/icons/QuestionCircleIcon16Solid";
import { XCircleIcon16Solid } from "@skbkontur/icons/XCircleIcon16Solid";
import { ReactUIComponentWithRef } from "@skbkontur/icons/helpers/forwardRef";
import { BaseIconProps } from "@skbkontur/icons/internal/BaseIcon";
import { WarningCircleIcon16Solid } from "@skbkontur/icons/WarningCircleIcon16Solid";
import { ShapeTriangleUpIcon16Solid } from "@skbkontur/icons/ShapeTriangleUpIcon16Solid";
import { WarningTriangleIcon16Solid } from "@skbkontur/icons/WarningTriangleIcon16Solid";
import { InfoCircleIcon16Solid } from "@skbkontur/icons/InfoCircleIcon16Solid";
import { Severity } from "./types/Severity";

const icons: Record<Severity | "unknown", ReactUIComponentWithRef<SVGSVGElement, BaseIconProps>> = {
    blocker: XCircleIcon16Solid,
    critical: WarningCircleIcon16Solid,
    major: WarningTriangleIcon16Solid,
    minor: ShapeTriangleUpIcon16Solid,
    info: InfoCircleIcon16Solid,
    unknown: QuestionCircleIcon16Solid,
};

const colors: Record<Severity | "unknown", string> = {
    blocker: "#fe4c4c",
    critical: "#fe4c4c",
    major: "#fcb73e",
    minor: "#fcb73e",
    info: "#2291ff",
    unknown: "rgba(0, 0, 0, 0.54)",
};

interface SeverityIconProps {
    type: Severity;
}

export function SeverityIcon({ type }: SeverityIconProps) {
    const Icon = icons[type];
    return <Icon title={type} color={colors[type]} />;
}

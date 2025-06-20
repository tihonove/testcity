import * as React from "react";

interface PieChartProps {
    percentage: number;
    size?: number;
    thickness?: number; // Добавляем новый параметр для толщины кольца
}

export function SvgPieChart({ percentage, size = 20, thickness = 4 }: PieChartProps) {
    const radius = size / 2;
    const strokeRadius = radius - thickness / 2;
    const circumference = 2 * Math.PI * strokeRadius;
    const strokeDashoffset = circumference * (1 - percentage);

    return (
        <svg
            width={size}
            height={size}
            viewBox={`0 0 ${size.toString()} ${size.toString()}`}
            style={{ verticalAlign: "middle", marginRight: "8px" }}>
            {/* Background red circle (unsuccessful part) */}
            <circle
                cx={radius}
                cy={radius}
                r={strokeRadius}
                fill="none"
                stroke="#E95454" // Default failed color
                strokeWidth={thickness}
            />

            {/* Green sector (successful part) */}
            {percentage > 0 && (
                <circle
                    cx={radius}
                    cy={radius}
                    r={strokeRadius}
                    fill="none"
                    stroke="#4CAF50" // Default success color
                    strokeWidth={thickness}
                    strokeDasharray={circumference}
                    strokeDashoffset={strokeDashoffset}
                    transform={`rotate(-90, ${radius.toString()}, ${radius.toString()})`}
                    style={{ transition: "stroke-dashoffset 0.3s" }}
                />
            )}
        </svg>
    );
}

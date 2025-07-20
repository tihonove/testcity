import * as React from "react";
import styles from "./RunStatisticsChart.module.css";
import { Tooltip, Bar, BarChart, Brush, CartesianGrid, Rectangle, ResponsiveContainer, XAxis, YAxis } from "recharts";
import { formatDuration } from "./DurationUtils";

interface RunStatisticsChartProps {
    value: Array<[state: string, duration: number, startDate: string]>;
}

interface BarShapeProps {
    payload?: { "0": string };
    [key: string]: unknown;
}

interface TooltipProps {
    label?: string | number;
}

function BarShape(props: BarShapeProps) {
    if (props.payload?.["0"] === "Success") {
        return <Rectangle {...props} fill="rgb(var(--inverse-color-base) / 0.3)" />;
    } else {
        return <Rectangle {...props} fill="rgb(from var(--failed-bg-color) r g b / 50%)" />;
    }
}

function CustomTooltip(props: TooltipProps) {
    return <div className={styles.tooltip}>{props.label}</div>;
}

export function RunStatisticsChart(props: RunStatisticsChartProps): React.JSX.Element {
    const maxVisibleDuration = React.useMemo(() => {
        const maxDuration = props.value.reduce((x, y) => (x > y[1] ? x : y[1]), 0);
        if (maxDuration == 0) return 100;
        if (maxDuration < 10) return 10;
        if (maxDuration < 100) return 100;
        if (maxDuration < 1000) return Math.ceil(maxDuration / 100) * 100;
        if (maxDuration < 0.8 * 60 * 1000) return Math.ceil(maxDuration / 1000) * 1000;
        if (maxDuration <= 60 * 1000) return 60 * 1000;
        if (maxDuration < 0.8 * 60 * 60 * 1000) return Math.ceil(maxDuration / (60 * 1000)) * (60 * 1000);
        if (maxDuration <= 60 * 60 * 1000) return 60 * 60 * 1000;
        return Math.ceil(maxDuration / (60 * 1000)) * (60 * 1000);
    }, [props.value]);

    return (
        <div className={styles.chartContainer}>
            <ResponsiveContainer>
                <BarChart data={props.value} width={500} height={200}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey={2} />
                    <YAxis
                        domain={[0, maxVisibleDuration]}
                        dataKey={1}
                        tickFormatter={(x: number) => {
                            return formatDuration(maxVisibleDuration, x);
                        }}
                    />
                    <Tooltip content={CustomTooltip} />
                    <Brush
                        fill={"var(--primary-bg)"}
                        height={30}
                        startIndex={Math.max(props.value.length - 200, 0)}
                        endIndex={props.value.length - 1}>
                        <BarChart>
                            <XAxis dataKey={2} />
                            <Bar dataKey={1} fill="rgb(var(--inverse-color-base) / 0.3)" shape={<BarShape />}></Bar>
                        </BarChart>
                    </Brush>
                    <Bar dataKey={1} fill="rgb(var(--inverse-color-base) / 0.3)" shape={<BarShape />}></Bar>
                </BarChart>
            </ResponsiveContainer>
        </div>
    );
}

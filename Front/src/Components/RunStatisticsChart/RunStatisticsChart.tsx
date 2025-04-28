import * as React from "react";
import { Button, Tooltip } from "@skbkontur/react-ui";
import { formatDuration } from "./DurationUtils";
import { useLayoutEffect, useRef } from "react";
import styles from "./RunStatisticsChart.module.css";

interface RunStatisticsChartProps {
    value: Array<[state: string, duration: number, startDate: string]>;
}

export function RunStatisticsChart(props: RunStatisticsChartProps): React.JSX.Element {
    const [scale, setScale] = React.useState(1);
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

    const durationLabelStep = maxVisibleDuration / 4;
    const scrollContainer = useRef<HTMLDivElement>(null);

    useLayoutEffect(() => {
        if (scrollContainer.current != undefined) scrollContainer.current.scrollLeft = 100000;
    }, []);

    return (
        <div className={styles.chartContainer}>
            <div className={styles.scaleButtons}>
                <Button
                    onClick={() => {
                        setScale(scale / 2);
                    }}>
                    -
                </Button>
                <Button
                    onClick={() => {
                        setScale(scale * 2);
                    }}>
                    +
                </Button>
            </div>
            <div className={styles.gaugeLabels}>
                <div style={{ top: 0 }}>
                    {formatDuration(maxVisibleDuration, maxVisibleDuration - 0 * durationLabelStep)}
                </div>
                <div style={{ top: 25 }}>
                    {formatDuration(maxVisibleDuration, maxVisibleDuration - 1 * durationLabelStep)}
                </div>
                <div style={{ top: 50 }}>
                    {formatDuration(maxVisibleDuration, maxVisibleDuration - 2 * durationLabelStep)}
                </div>
                <div style={{ top: 75 }}>
                    {formatDuration(maxVisibleDuration, maxVisibleDuration - 3 * durationLabelStep)}
                </div>
                <div style={{ top: 100 }}>
                    {formatDuration(maxVisibleDuration, maxVisibleDuration - 4 * durationLabelStep)}
                </div>
            </div>
            <div className={styles.gridLine} />
            <div className={styles.gridLine} />
            <div className={styles.gridLine} />
            <div className={styles.gridLine} />
            <div className={styles.scrollContainer} ref={scrollContainer}>
                <div className={styles.container}>
                    {reverse(props.value).map((x, index) => (
                        <Tooltip trigger={"hover"} render={() => x[2]} key={index}>
                            <div
                                data-state={x[0] === "Success" ? "Success" : "Failed"}
                                key={index}
                                style={{
                                    height: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                    flexBasis: `${(16 * scale).toString()}px`,
                                }}>
                                {index % (8 / scale) === 0 && <div className={styles.dateLabel}>{x[2]}</div>}
                            </div>
                        </Tooltip>
                    ))}
                </div>
                <div className={styles.datesContainer}></div>
            </div>
        </div>
    );
}

function reverse<T>(items: T[]): T[] {
    const result = [];
    for (let i = 0; i < items.length; i++) {
        result[items.length - 1 - i] = items[i];
    }
    return result;
}

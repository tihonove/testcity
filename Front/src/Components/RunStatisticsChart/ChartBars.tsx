import * as React from "react";
import { Tooltip } from "@skbkontur/react-ui";
import styles from "./RunStatisticsChart.module.css";

interface ChartBarsProps {
    data: Array<[state: string, duration: number, startDate: string]>;
    barWidth: number;
    maxVisibleDuration: number;
    containerWidth: number;
}

export function reverse<T>(items: T[]): T[] {
    const result = [];
    for (let i = 0; i < items.length; i++) {
        result[items.length - 1 - i] = items[i];
    }
    return result;
}

export const ChartBars = React.memo<ChartBarsProps>(({ data, barWidth, maxVisibleDuration, containerWidth }) => {
    const reversedData = React.useMemo(() => reverse(data), [data]);

    return (
        <div className={styles.container} style={{ width: containerWidth }}>
            {reversedData.map((x, index) => (
                <Tooltip trigger={"hover"} render={() => x[2]} key={index}>
                    <div
                        data-state={x[0] === "Success" ? "Success" : "Failed"}
                        key={index}
                        style={{
                            height: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                            flexBasis: `${barWidth.toString()}px`,
                        }}>
                        <div
                            style={{
                                height: `${(100 - 100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                bottom: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                            }}
                            className={styles.errorLine}></div>
                        {index % Math.floor(200 / barWidth) === 0 && <div className={styles.dateLabel}>{x[2]}</div>}
                    </div>
                </Tooltip>
            ))}
        </div>
    );
});

ChartBars.displayName = "ChartBars";

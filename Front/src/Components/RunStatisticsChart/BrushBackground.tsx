import * as React from "react";
import styles from "./RunStatisticsChart.module.css";
import { reverse } from "./ChartBars";

interface BrushBackgroundProps {
    data: Array<readonly [state: string, duration: number, startDate: string]>;
    maxVisibleDuration: number;
}

export const BrushBackground = React.memo<BrushBackgroundProps>(({ data, maxVisibleDuration }) => {
    const reversedData = React.useMemo(() => reverse(data), [data]);

    return (
        <div className={styles.brushBackgroundContainer}>
            {reversedData.map((x, index) => (
                <div
                    key={index}
                    data-state={x[0] === "Success" ? "Success" : "Failed"}
                    style={{
                        height: `${(100 * (x[1] / maxVisibleDuration)).toString()}%`,
                        flexBasis: `100%`,
                    }}>
                    <div
                        style={{
                            height: `${(30 - 30 * (x[1] / maxVisibleDuration)).toString()}px`,
                            bottom: `${(30 * (x[1] / maxVisibleDuration)).toString()}px`,
                        }}
                        className={styles.errorLine}></div>
                </div>
            ))}
        </div>
    );
});

BrushBackground.displayName = "BrushBackground";

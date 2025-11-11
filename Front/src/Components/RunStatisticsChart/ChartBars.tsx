import * as React from "react";
import { Tooltip } from "@skbkontur/react-ui";
import styles from "./RunStatisticsChart.module.css";

// Debounce hook для отложенного обновления с принудительным обновлением
function useDebounceWithForceUpdate<T>(value: T, delay: number): [T, (newValue: T) => void] {
    const [debouncedValue, setDebouncedValue] = React.useState(value);

    const forceUpdate = React.useCallback((newValue: T) => {
        setDebouncedValue(newValue);
    }, []);

    React.useEffect(() => {
        const handler = setTimeout(() => {
            setDebouncedValue(value);
        }, delay);

        return () => {
            clearTimeout(handler);
        };
    }, [value, delay]);

    return [debouncedValue, forceUpdate];
}

interface ChartBarsProps {
    data: Array<readonly [state: string, duration: number, startDate: string]>;
    barWidth: number;
    maxVisibleDuration: number;
}

export function reverse<T>(items: T[]): T[] {
    const result = [];
    for (let i = 0; i < items.length; i++) {
        result[items.length - 1 - i] = items[i];
    }
    return result;
}

// Компонент для отрисовки массива столбцов - мемоизируется только при изменении debouncedBarWidth
const ChartBarsContent = React.memo<{
    reversedData: Array<readonly [state: string, duration: number, startDate: string]>;
    debouncedBarWidth: number;
    maxVisibleDuration: number;
}>(({ reversedData, debouncedBarWidth, maxVisibleDuration }) => {
    return React.useMemo(
        () => (
            <>
                {reversedData.map((x, index) => (
                    <Tooltip trigger={"hover"} render={() => x[2]} key={index}>
                        <div
                            data-state={x[0] === "Success" ? "Success" : "Failed"}
                            key={index}
                            style={{
                                height: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                flexBasis: `${debouncedBarWidth.toString()}px`,
                            }}>
                            <div
                                style={{
                                    height: `${(100 - 100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                    bottom: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                }}
                                className={styles.errorLine}></div>
                            {index % Math.floor(200 / debouncedBarWidth) === 0 && (
                                <div className={styles.dateLabel}>{x[2]}</div>
                            )}
                        </div>
                    </Tooltip>
                ))}
            </>
        ),
        [reversedData, debouncedBarWidth, maxVisibleDuration]
    );
});

ChartBarsContent.displayName = "ChartBarsContent";

export const ChartBars = React.memo<ChartBarsProps>(({ data, barWidth, maxVisibleDuration }) => {
    const reversedData = React.useMemo(() => reverse(data), [data]);

    // Используем debounce для barWidth с задержкой 300ms и возможностью принудительного обновления
    const [debouncedBarWidth, forceDebouncedBarWidthUpdate] = useDebounceWithForceUpdate(barWidth, 300);

    // Вычисляем масштаб для временного скейлинга
    const scaleX = React.useMemo(() => {
        if (debouncedBarWidth === 0) return 1;
        return barWidth / debouncedBarWidth;
    }, [barWidth, debouncedBarWidth]);

    // Принудительно обновляем debouncedBarWidth если масштаб изменился на 50% или больше
    React.useEffect(() => {
        const scaleChange = Math.abs(scaleX - 1);
        if (scaleChange >= 0.5) {
            forceDebouncedBarWidthUpdate(barWidth);
        }
    }, [scaleX, barWidth, forceDebouncedBarWidthUpdate]);

    // Используем debouncedBarWidth для расчета ширины контейнера и рендера
    const containerWidth = React.useMemo(
        () => (debouncedBarWidth ? debouncedBarWidth * data.length : undefined),
        [debouncedBarWidth, data]
    );

    // Определяем, нужно ли применять временное масштабирование
    const isScaling = scaleX !== 1;

    return (
        <div
            className={styles.container}
            style={{
                width: containerWidth,
                transform: isScaling ? `scaleX(${scaleX.toString()})` : undefined,
                transformOrigin: "left center",
            }}>
            <ChartBarsContent
                reversedData={reversedData}
                debouncedBarWidth={debouncedBarWidth}
                maxVisibleDuration={maxVisibleDuration}
            />
        </div>
    );
});

ChartBars.displayName = "ChartBars";

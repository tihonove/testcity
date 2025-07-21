import * as React from "react";
import { Button, Tooltip } from "@skbkontur/react-ui";
import { formatDuration } from "./DurationUtils";
import { useEffect, useLayoutEffect, useRef } from "react";
import styles from "./RunStatisticsChart.module.css";
import Draggable from "react-draggable";
import { useElementSize } from "../../Utils/useElementSize";

interface RunStatisticsChartProps {
    value: Array<[state: string, duration: number, startDate: string]>;
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

    const durationLabelStep = maxVisibleDuration / 4;

    const brushContainer = useRef<HTMLDivElement>(null);
    const scrollContainer = useRef<HTMLDivElement>(null);
    const container = useRef<HTMLDivElement>(null);

    const brushContainerSize = useElementSize(brushContainer);
    const scrollContainerSize = useElementSize(scrollContainer);

    const druggingRef = React.useRef<boolean>(false);
    const [left, setLeft] = React.useState(0);
    const [right, setRight] = React.useState(100);
    const barWidth = React.useMemo(() => {
        console.log(3, left, right);
        if (!scrollContainerSize || !brushContainerSize) return undefined;
        return (scrollContainerSize.width * brushContainerSize.width) / ((right - left) * props.value.length);
    }, [left, right]);
    const containerWidth = React.useMemo(
        () => (barWidth ? barWidth * props.value.length : undefined),
        [barWidth, props.value]
    );

    useLayoutEffect(() => {
        if (barWidth == undefined && brushContainerSize && scrollContainerSize) {
            setLeft(
                Math.max(
                    brushContainerSize.width -
                        (scrollContainerSize.width * brushContainerSize.width) / (20 * props.value.length),
                    0
                )
            );
            setRight(brushContainerSize.width);
        }
    }, [brushContainerSize, scrollContainerSize]);

    const syncFromScrollPositionToBrush = React.useCallback(() => {
        if (!brushContainerSize || !containerWidth) return;
        const scrollContainerEl = scrollContainer.current;
        if (scrollContainerEl && scrollContainerSize) {
            if (scrollContainerEl.scrollLeft > 0) {
                setLeft(Math.floor(scrollContainerEl.scrollLeft * (brushContainerSize.width / containerWidth)));
                setRight(
                    Math.floor(
                        (scrollContainerEl.scrollLeft + scrollContainerSize.width) *
                            (brushContainerSize.width / containerWidth)
                    )
                );
                scrollContainerEl.scrollLeft = 10000;
            }
        }
    }, [brushContainerSize, scrollContainerSize, containerWidth]);

    // useEffect(() => {
    //     syncFromScrollPositionToBrush();
    // }, [brushContainerSize]);

    // const handleScrollRef = React.useRef<() => void>();
    // const handleScroll = React.useCallback(() => {
    //     if (!druggingRef.current) syncFromScrollPositionToBrush();
    // }, [syncFromScrollPositionToBrush]);
    // useEffect(() => {
    //     handleScrollRef.current = handleScroll;
    // }, [handleScroll]);
    // useLayoutEffect(() => {
    //     const scrollContainerEl = scrollContainer.current;
    //     if (scrollContainerEl) {
    //         scrollContainerEl.onscroll = () => {
    //             handleScrollRef.current?.();
    //         };
    //     }
    // }, []);

    useLayoutEffect(() => {
        const scrollContainerEl = scrollContainer.current;
        if (scrollContainerEl && brushContainerSize && containerWidth) {
            scrollContainerEl.scrollLeft = left * (containerWidth / brushContainerSize.width);
        }
    }, [left, containerWidth]);

    return (
        <div>
            <div className={styles.chartContainer}>
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
                    <div className={styles.container} ref={container}>
                        {barWidth != undefined &&
                            reverse(props.value).map((x, index) => (
                                <Tooltip trigger={"hover"} render={() => x[2]} key={index}>
                                    <div
                                        data-state={x[0] === "Success" ? "Success" : "Failed"}
                                        key={index}
                                        style={{
                                            height: `${(100 * (x[1] / maxVisibleDuration)).toString()}px`,
                                            flexBasis: `${barWidth.toString()}px`,
                                        }}>
                                        {index % Math.floor(200 / barWidth) === 0 && (
                                            <div className={styles.dateLabel}>{x[2]}</div>
                                        )}
                                    </div>
                                </Tooltip>
                            ))}
                    </div>
                    <div className={styles.datesContainer}></div>
                </div>
            </div>
            <div className={styles.brushContainer} style={{ height: "20px" }} ref={brushContainer}>
                <div className={styles.brushBackgroundContainer}>
                    {reverse(props.value).map((x, index) => (
                        <div
                            data-state={x[0] === "Success" ? "Success" : "Failed"}
                            style={{
                                height: `${(100 * (x[1] / maxVisibleDuration)).toString()}%`,
                                flexBasis: `100%`,
                            }}></div>
                    ))}
                </div>
                <Draggable
                    axis="x"
                    scale={1}
                    position={{ x: left, y: 0 }}
                    bounds={{ left: 0, right: right - 10 }}
                    onStart={() => {
                        druggingRef.current = true;
                    }}
                    onDrag={(e, { deltaX }) => {
                        setLeft(x => x + deltaX);
                    }}
                    onStop={(e, { x, y }) => {
                        druggingRef.current = false;
                        setLeft(x);
                    }}>
                    <div
                        style={{
                            position: "absolute",
                            top: 0,
                            bottom: 0,
                            width: "5px",
                            left: 0,
                            backgroundColor: "blue",
                            opacity: 0.5,
                        }}></div>
                </Draggable>
                <Draggable
                    axis="x"
                    scale={1}
                    position={{ x: left + 5, y: 0 }}
                    bounds={{ left: 5, right: (brushContainerSize?.width ?? 1000) - (right - left - 5) }}
                    onStart={() => {
                        druggingRef.current = true;
                    }}
                    onDrag={(e, { deltaX }) => {
                        setLeft(x => x + deltaX);
                        setRight(x => x + deltaX);
                    }}
                    onStop={(e, { x, y }) => {
                        druggingRef.current = false;
                    }}>
                    <div
                        style={{
                            position: "absolute",
                            top: 0,
                            bottom: 0,
                            width: right - left - 5,
                            left: 0,
                            backgroundColor: "green",
                            opacity: 0.5,
                        }}></div>
                </Draggable>
                <Draggable
                    axis="x"
                    position={{ x: right, y: 0 }}
                    bounds={{ left: left + 10, right: brushContainerSize?.width ?? 1000 }}
                    onStart={() => {
                        druggingRef.current = true;
                    }}
                    onDrag={(e, { x, y, deltaX }) => {
                        setRight(x => x + deltaX);
                    }}
                    onStop={(e, { x, y }) => {
                        druggingRef.current = false;
                        setRight(x);
                    }}>
                    <div
                        style={{
                            position: "absolute",
                            top: 0,
                            bottom: 0,
                            width: "5px",
                            backgroundColor: "blue",
                            opacity: 0.5,
                        }}></div>
                </Draggable>
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

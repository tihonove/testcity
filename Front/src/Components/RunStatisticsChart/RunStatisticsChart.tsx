import * as React from "react";
import { formatDuration } from "./DurationUtils";
import { useLayoutEffect, useRef } from "react";
import styles from "./RunStatisticsChart.module.css";
import Draggable from "react-draggable";
import { useElementSize } from "../../Utils/useElementSize";
import { ChartBars } from "./ChartBars";
import { BrushBackground } from "./BrushBackground";

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

    const brushContainerSize = useElementSize(brushContainer);
    const scrollContainerSize = useElementSize(scrollContainer);
    const druggingRef = React.useRef<boolean>(false);

    const [left, setLeft] = React.useState(0);
    const [right, setRight] = React.useState(100);

    const barWidth = React.useMemo(() => {
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
                        (scrollContainerSize.width * brushContainerSize.width) / (10 * props.value.length),
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
                    {barWidth != undefined && containerWidth != undefined && (
                        <ChartBars
                            data={props.value}
                            barWidth={barWidth}
                            maxVisibleDuration={maxVisibleDuration}
                            containerWidth={containerWidth}
                        />
                    )}
                </div>
            </div>
            <div className={styles.brushContainer} ref={brushContainer}>
                <BrushBackground data={props.value} maxVisibleDuration={maxVisibleDuration} />
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
                    <div className={styles.brushHandle}></div>
                </Draggable>
                <Draggable
                    axis="x"
                    scale={1}
                    position={{ x: left, y: 0 }}
                    bounds={{ left: 0, right: (brushContainerSize?.width ?? 1000) - (right - left) }}
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
                    <div className={styles.brushMiddle} style={{ width: right - left }}></div>
                </Draggable>
                <Draggable
                    axis="x"
                    position={{ x: right, y: 0 }}
                    bounds={{ left: 0, right: brushContainerSize?.width ?? 1000 }}
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
                    <div className={styles.brushHandleRight}></div>
                </Draggable>
            </div>
        </div>
    );
}

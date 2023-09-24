import * as React from "react";
import styled from "styled-components";
import {Button} from "@skbkontur/react-ui";
import {formatDuration} from "./DurationUtils";

interface RunStatisticsChartProps {
    value: Array<[state: string, duration: number, startDate: string]>;
}

export function RunStatisticsChart(props: RunStatisticsChartProps): React.JSX.Element {
    const [scale, setScale] = React.useState(1);
    const maxVisibleDuration = React.useMemo(() => {
        const maxDuration = props.value.reduce((x, y) => x > y[1] ? x : y[1], 0);
        if (maxDuration == 0)
            return 100;
        if (maxDuration < 100)
            return 100;
        if (maxDuration < 1000)
            return Math.ceil(maxDuration / 100) * 100;
        if (maxDuration < 0.8 * 60 * 1000)
            return Math.ceil(maxDuration / 1000) * 1000;
        if (maxDuration <= 60 * 1000)
            return 60 * 1000;
        if (maxDuration < 0.8 * 60 * 60 * 1000)
            return Math.ceil(maxDuration / (60 * 1000)) * (60 * 1000);
        if (maxDuration <= 60 * 60 * 1000)
            return 60 * 60 * 1000;
        return Math.ceil(maxDuration / (60 * 1000)) * (60 * 1000);
    }, [props.value]);

    const durationLabelStep = maxVisibleDuration / 4;

    return (
        <ChartContainer>
            <ScaleButtons>
                <Button onClick={() => setScale(scale / 2)}>-</Button>
                <Button onClick={() => setScale(scale * 2)}>+</Button>
            </ScaleButtons>
            <GaugeLabels>
                <GaugeLabel index={0}>{formatDuration(maxVisibleDuration, maxVisibleDuration - 0 * durationLabelStep)}</GaugeLabel>
                <GaugeLabel index={1}>{formatDuration(maxVisibleDuration, maxVisibleDuration - 1 * durationLabelStep)}</GaugeLabel>
                <GaugeLabel index={2}>{formatDuration(maxVisibleDuration, maxVisibleDuration - 2 * durationLabelStep)}</GaugeLabel>
                <GaugeLabel index={3}>{formatDuration(maxVisibleDuration, maxVisibleDuration - 3 * durationLabelStep)}</GaugeLabel>
                <GaugeLabel index={4}>{formatDuration(maxVisibleDuration, maxVisibleDuration - 4 * durationLabelStep)}</GaugeLabel>
            </GaugeLabels>
            <GridLine index={0} />
            <GridLine index={1} />
            <GridLine index={2} />
            <GridLine index={3} />
            <ScrollContainer>
                <Container>
                    {props.value.map((x, index) =>
                        <CharBar
                            success={x[0] == "Success"}
                            scale={scale}
                            key={index}
                            style={{height: 100 * (x[1] / maxVisibleDuration)}}
                        />
                    )}
                </Container>
            </ScrollContainer>
        </ChartContainer>
    );
}

const ChartContainer = styled.div({
    display: "flex",
    position: "relative",
    alignItems: "stretch",
});

const ScrollContainer = styled.div({
    flexBasis: "100%",
    flexShrink: 1,
    flexGrow: 1,
    overflowX: "scroll",
    position: "relative",
});

const GaugeLabels = styled.div({
    flexBasis: 50,
    flexShrink: 0,
    flexGrow: 1,
    width: 50,
    position: "relative",
});

const GaugeLabel = styled.div<{ index: number }>(props => ({
    position: "absolute",
    top: 10 + (props.index * 25) - 10,
    right: 5,
    textAlign: "right",
    fontSize: "12px",
}));

const GridLine = styled.div<{ index: number }>(props => ({
    position: "absolute",
    left: 50,
    right: 0,
    height: 1,
    backgroundColor: "rgba(0, 0, 0, 0.1)",
    top: 10 + props.index * 25,
}));

const ScaleButtons = styled.div({
    position: "absolute",
    right: 4,
    top: 4,
    zIndex: 10,
});

const Container = styled.div({
    display: "flex",
    flexDirection: "row",
    alignItems: "flex-end",
    height: 110,
    position: "relative",
});

const CharBar = styled.div<{ scale: number, success: boolean }>(props => ({
    flexGrow: 0,
    flexShrink: 0,
    flexBasis: 16 * props.scale,
    backgroundColor: "red",
    borderTop: "2px solid rgba(0,0,0,0.4)",
    marginLeft: 1,
    boxSizing: "border-box",
}), props => props.success
    ? ({backgroundColor: "rgba(0, 0, 0, 0.1)"})
    : ({
        backgroundColor: "rgba(255, 0, 0, 0.4)"
    }))


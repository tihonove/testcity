import * as React from "react";
import styled from "styled-components";
import { Hint } from "@skbkontur/react-ui";
import { formatRelativeTime, formatTestDuration, toLocalTimeFromUtc } from "../Utils";

interface TimingCellProps {
    startDateTime: string;
    duration: string | number;
}

export function TimingCell({ startDateTime, duration }: TimingCellProps) {
    return (
        <StyledTimingCell>
            <Hint
                maxWidth={400}
                text={
                    <TimingHintContent>
                        <div>
                            <TimingHintCaption>Start time:</TimingHintCaption>
                            {toLocalTimeFromUtc(startDateTime)}
                        </div>
                        <div>
                            <TimingHintCaption>Duration:</TimingHintCaption>
                            {formatTestDuration(duration.toString())}
                        </div>
                    </TimingHintContent>
                }>
                <Started>{formatRelativeTime(startDateTime)}</Started>
                <Duration
                    style={{
                        width: formatTestDuration(duration.toString()).length * 9,
                    }}>
                    {formatTestDuration(duration.toString())}
                </Duration>
            </Hint>
        </StyledTimingCell>
    );
}

const TimingHintContent = styled.div`
    text-align: left;
`;

const TimingHintCaption = styled.span`
    display: inline-block;
    width: 80px;
`;

const StyledTimingCell = styled.td`
    max-width: 250px;
    white-space: nowrap;
    text-align: right;
    cursor: default;
    font-size: 14px;
`;

const Started = styled.span`
    color: ${props => props.theme.mutedTextColor};
`;

const Duration = styled.span`
    display: inline-block;
    margin-left: 4px;
`;

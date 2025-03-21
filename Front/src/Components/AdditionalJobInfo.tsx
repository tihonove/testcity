import React from "react";
import styled from "styled-components";
import { toLocalTimeFromUtc, formatTestDuration } from "../Utils";

interface AdditionalJobInfoProps {
    startDateTime: string;
    endDateTime: string;
    duration: number;
    triggered: string;
    pipelineSource: string;
}

export function AdditionalJobInfo(props: AdditionalJobInfoProps) {
    return (
        <table>
            <tbody>
                <InfoRow>
                    <InfoKey>Time</InfoKey>
                    <td>
                        {toLocalTimeFromUtc(props.startDateTime, "short")} —{" "}
                        {toLocalTimeFromUtc(props.endDateTime, "short")} (
                        {formatTestDuration(props.duration.toString())})
                    </td>
                </InfoRow>
                <InfoRow>
                    <InfoKey>Triggered</InfoKey>
                    <td>{props.triggered.replace("@skbkontur.ru", "")}</td>
                </InfoRow>
                <InfoRow>
                    <InfoKey>Pipeline created by</InfoKey>
                    <td>{props.pipelineSource}</td>
                </InfoRow>
            </tbody>
        </table>
    );
}

const InfoRow = styled.tr`
    height: 20px;
`;

const InfoKey = styled.td`
    padding: 5px 0;
    width: 150px;
    font-weight: bold;
`;

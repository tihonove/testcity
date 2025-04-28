import React from "react";
import styles from "./AdditionalJobInfo.module.css";
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
                <tr className={styles.infoRow}>
                    <td className={styles.infoKey}>Time</td>
                    <td>
                        {toLocalTimeFromUtc(props.startDateTime, "short")} —{" "}
                        {toLocalTimeFromUtc(props.endDateTime, "short")} (
                        {formatTestDuration(props.duration.toString())})
                    </td>
                </tr>
                <tr className={styles.infoRow}>
                    <td className={styles.infoKey}>Triggered</td>
                    <td>{props.triggered.replace("@skbkontur.ru", "")}</td>
                </tr>
                <tr className={styles.infoRow}>
                    <td className={styles.infoKey}>Pipeline created by</td>
                    <td>{props.pipelineSource}</td>
                </tr>
            </tbody>
        </table>
    );
}

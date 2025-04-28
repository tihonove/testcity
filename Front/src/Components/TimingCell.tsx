import * as React from "react";
import { Hint } from "@skbkontur/react-ui";
import { formatRelativeTime, formatTestDuration, toLocalTimeFromUtc } from "../Utils";
import styles from "./TimingCell.module.css";

interface TimingCellProps {
    startDateTime: string;
    duration: string | number | null;
}

export function TimingCell({ startDateTime, duration }: TimingCellProps) {
    return (
        <td className={styles.styledTimingCell}>
            <Hint
                maxWidth={400}
                text={
                    <div className={styles.timingHintContent}>
                        <div>
                            <span className={styles.timingHintCaption}>Start time:</span>
                            {toLocalTimeFromUtc(startDateTime)}
                        </div>
                        {duration != null && (
                            <div>
                                <span className={styles.timingHintCaption}>Duration:</span>
                                {formatTestDuration(duration.toString())}
                            </div>
                        )}
                    </div>
                }>
                <span className={styles.started}>{formatRelativeTime(startDateTime)}</span>
                {duration != null && (
                    <span
                        className={styles.duration}
                        style={{
                            width: formatTestDuration(duration.toString()).length * 9,
                        }}>
                        {formatTestDuration(duration.toString())}
                    </span>
                )}
            </Hint>
        </td>
    );
}

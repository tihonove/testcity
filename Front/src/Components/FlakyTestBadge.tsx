import * as React from "react";
import styles from "./FlakyTestBadge.module.css";

interface FlakyTestBadgeProps {
    className?: string;
}

export function FlakyTestBadge({ className }: FlakyTestBadgeProps): React.JSX.Element | null {
    return (
        <span className={className}>
            <span className={styles.green}>f</span>
            <span className={styles.green}>l</span>
            <span className={styles.red}>a</span>
            <span className={styles.green}>k</span>
            <span className={styles.red}>y</span>
        </span>
    );
}

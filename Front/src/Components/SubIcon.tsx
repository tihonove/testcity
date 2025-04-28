import * as React from "react";
import styles from "./SubIcon.module.css";

export function SubIcon({
    children,
    sub,
    noclip,
}: {
    children: React.ReactNode;
    sub: React.ReactNode;
    noclip?: boolean;
}): React.JSX.Element {
    return (
        <div className={styles.iconContainer}>
            <div className={`${styles.mainIcon} ${!noclip ? styles.mainIconWithClip : ""}`}>{children}</div>
            <div className={styles.subIconContainer}>{sub}</div>
        </div>
    );
}

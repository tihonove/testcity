import { UiMenuDots3VIcon16Regular } from "@skbkontur/icons";
import * as React from "react";
import styles from "./KebabButton.module.css";

export function KebabButton() {
    return (
        <span className={styles.kebabButtonRoot}>
            <UiMenuDots3VIcon16Regular />
        </span>
    );
}

export const KebabButtonRoot = (props: React.HTMLProps<HTMLSpanElement>) => (
    <span {...props} className={`${styles.kebabButtonRoot} ${props.className || ""}`} />
);

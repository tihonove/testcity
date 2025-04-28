import * as React from "react";
import { UiLoadSpinnerIcon16Light } from "@skbkontur/icons";
import styles from "./RotatingSpinner.module.css";

export function RotatingSpinner() {
    return (
        <span className={styles.spinnerWrapper}>
            <UiLoadSpinnerIcon16Light />
        </span>
    );
}

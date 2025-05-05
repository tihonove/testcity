import { ToolHardHammersIcon64Regular } from "@skbkontur/icons";
import * as React from "react";
import styles from "./AppUnderMaintenance.module.css";

export function AppUnderMaintenance() {
    return (
        <React.StrictMode>
            <div className={styles.container}>
                <ToolHardHammersIcon64Regular />
                <h1>Service under maintenance</h1>
            </div>
        </React.StrictMode>
    );
}

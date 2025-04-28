import * as React from "react";
import { ShareNetworkIcon16Light } from "@skbkontur/icons";
import { DEFAULT_BRANCHE_NAMES } from "./BranchSelect";
import styles from "./BranchBox.module.css";

export function BranchBox({ name }: { name: string }) {
    const isDefaultBranch = DEFAULT_BRANCHE_NAMES.includes(name);
    return (
        <span data-default={isDefaultBranch ? "true" : "false"} className={styles.root} title={name}>
            <ShareNetworkIcon16Light />
            <span className={styles.name}>{name}</span>
        </span>
    );
}

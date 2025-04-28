import { CheckCircleIcon16Light, MinusCircleIcon16Light, WarningCircleIcon16Light } from "@skbkontur/icons";
import * as React from "react";
import { Link, To } from "react-router-dom";
import styles from "./JobLink.module.css";

export function JobLink(props: { state: string; to: To; children: React.ReactNode }) {
    const stateClass =
        props.state === "Success" ? styles.success : props.state === "Canceled" ? styles.canceled : styles.failed;

    return (
        <Link className={`${styles.jobLinkWithResults} ${stateClass}`} to={props.to}>
            {props.state == "Success" ? (
                <CheckCircleIcon16Light />
            ) : props.state == "Canceled" ? (
                <MinusCircleIcon16Light />
            ) : (
                <WarningCircleIcon16Light />
            )}

            {props.children}
        </Link>
    );
}

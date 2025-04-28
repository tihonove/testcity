import * as React from "react";
import { NetCloudIcon32Regular } from "@skbkontur/icons";
import { Spinner } from "@skbkontur/react-ui";
import styles from "./PageLoader.module.css";

export const PageLoader = () => (
    <div className={styles.loaderContainer}>
        <h2 className={styles.loaderMessage}>
            <Spinner type="big" caption="Loading" />
        </h2>
    </div>
);

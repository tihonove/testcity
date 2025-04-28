import * as React from "react";
import { Link } from "react-router-dom";
import Logo from "./Logo";
import { useBasePrefix } from "../Domain/Navigation";
import styles from "./LogoPageBlock.module.css";

export function LogoPageBlock() {
    const basePrefix = useBasePrefix();
    return (
        <Link to={basePrefix} className={styles.root}>
            <Logo />
            TestCity
        </Link>
    );
}

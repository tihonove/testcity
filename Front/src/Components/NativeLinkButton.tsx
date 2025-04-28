import * as React from "react";
import styles from "./NativeLinkButton.module.css";

interface NativeLinkButtonProps {
    onClick?: React.MouseEventHandler<HTMLSpanElement>;
    children?: React.ReactNode;
    className?: string;
}

export const NativeLinkButton: React.FC<NativeLinkButtonProps> = ({ onClick, children, className }) => {
    return (
        <span className={`${styles.button} ${className || ""}`} onClick={onClick}>
            {children}
        </span>
    );
};

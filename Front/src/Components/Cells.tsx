import * as React from "react";
import { Link } from "react-router-dom";
import styles from "./Cells.module.css";

export const BranchCell = ({ children, ...rest }: React.TdHTMLAttributes<HTMLTableCellElement>) => (
    <td className={styles.branchCell} {...rest}>
        {children}
    </td>
);

interface ColorByStateProps extends React.HTMLAttributes<HTMLSpanElement> {
    state: string;
}

export const ColorByState = ({ state, children, ...rest }: ColorByStateProps) => {
    const className =
        state === "Success"
            ? styles.colorByStateSuccess
            : state === "Canceled"
              ? styles.colorByStateCanceled
              : styles.colorByStateFailed;
    return (
        <span className={className} {...rest}>
            {children}
        </span>
    );
};

export const NumberCell = ({ children, ...rest }: React.TdHTMLAttributes<HTMLTableCellElement>) => (
    <td className={styles.numberCell} {...rest}>
        {children}
    </td>
);

export const SelectedOnHoverTr = ({ children, ...rest }: React.HTMLAttributes<HTMLTableRowElement>) => (
    <tr className={styles.selectedOnHoverTr} {...rest}>
        {children}
    </tr>
);

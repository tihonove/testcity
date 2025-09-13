import * as React from "react";
import styles from "./ProjectsWithRunsTable.module.css";

export const RunsTable = {
    columnCount: 6,
} as const;

export const ProjectsWithRunsTable = (props: React.TableHTMLAttributes<HTMLTableElement>) => (
    <table className={styles.projectsWithRunsTable} {...props} />
);

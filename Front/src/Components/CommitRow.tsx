import * as React from "react";
import { CommitRowProps } from "../Pages/JobRunTestListPage";
import { Fit, RowStack } from "@skbkontur/react-stack-layout";
import { GravatarImage } from "./GravatarImage";
import styles from "./CommitRow.module.css";

export function CommitRow(props: CommitRowProps) {
    return (
        <RowStack gap={2} block>
            <Fit>
                <GravatarImage
                    className={styles.avatarImage}
                    email={props.authorEmail}
                    size={32}
                    alt={props.authorName}
                />
            </Fit>
            <Fit>
                <div className={styles.message}>{props.messagePreview}</div>
                <div className={styles.details}>
                    {props.authorName} · #{props.sha.substring(0, 7)}
                </div>
            </Fit>
        </RowStack>
    );
}

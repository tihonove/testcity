import * as React from "react";
import { Fit, RowStack } from "@skbkontur/react-stack-layout";
import { GravatarImage } from "./GravatarImage";
import styles from "./CommitRow.module.css";
import { getLinkToCommit } from "../Domain/Navigation";

interface CommitRowProps {
    pathToProject: string[];
    sha: string;
    authorName: string;
    authorEmail: string;
    messagePreview: string;
}

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
                    {props.authorName} ·
                    <a href={getLinkToCommit(props.pathToProject, props.sha)}>#{props.sha.substring(0, 7)}</a>
                </div>
            </Fit>
        </RowStack>
    );
}

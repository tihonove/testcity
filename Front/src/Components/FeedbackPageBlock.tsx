import { CommentRect2TextIcon20Regular } from "@skbkontur/icons";
import { Hint } from "@skbkontur/react-ui";
import * as React from "react";
import { useBasePrefix } from "../Domain/Navigation";
import styles from "./FeedbackPageBlock.module.css";

export function FeedbackPageBlock() {
    const basePrefix = useBasePrefix();
    return (
        <div className={styles.root}>
            <Hint
                text="Click here to provide feedback or report issues (Mattermost channel)"
                pos="left top"
                maxWidth={400}>
                <a target="_blank" href={"https://chat.skbkontur.ru/kontur/channels/testcity"}>
                    <CommentRect2TextIcon20Regular />
                </a>
            </Hint>
        </div>
    );
}

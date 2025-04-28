import * as React from "react";
import { Group } from "../Domain/Storage/Projects/GroupNode";
import styles from "./GroupAvatar.module.css";

interface GroupAvatarProps {
    group: Group;
    size: "20px" | "32px";
}

export function GroupAvatar(props: GroupAvatarProps) {
    const rootClass = props.size === "20px" ? styles.groupAvatarRoot20 : styles.groupAvatarRoot32;
    return (
        <div className={rootClass} style={{ backgroundColor: deterministicColor(props.group.title) }}>
            {props.group.title[0]}
        </div>
    );
}

function deterministicColor(input: string) {
    let hash = 0;
    for (let i = 0; i < input.length; i++) {
        hash = input.charCodeAt(i) + ((hash << 5) - hash);
    }
    const color = (hash & 0x00ffffff).toString(16).toUpperCase();
    return `#${"000000".substring(0, 6 - color.length) + color}20`;
}

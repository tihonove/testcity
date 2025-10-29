import { People1Icon20Regular, People2Icon20Regular } from "@skbkontur/icons";
import * as React from "react";
import styles from "./UserAvatar.module.css";

interface UserAvatarProps {
    avatarUrl?: string | null;
    displayName?: string | null;
    userName?: string | null;
    size?: number;
}

export function UserAvatar({ avatarUrl, displayName, userName, size = 24 }: UserAvatarProps): React.JSX.Element {
    const avatarStyle = {
        width: size,
        height: size,
    };

    if (avatarUrl) {
        return (
            <div className={styles.avatar} style={avatarStyle}>
                <img src={avatarUrl} alt={displayName || userName || "User"} className={styles.avatarImage} />
            </div>
        );
    }

    return (
        <div className={`${styles.avatar} ${styles.avatarIcon}`} style={avatarStyle}>
            <People1Icon20Regular />
        </div>
    );
}

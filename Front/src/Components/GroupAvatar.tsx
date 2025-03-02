import * as React from "react";
import { Group } from "../Domain/Storage";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";

interface GroupAvatarProps {
    group: Group;
    size: "20px" | "32px";
}

export function GroupAvatar(props: GroupAvatarProps) {
    const Root = props.size == "20px" ? GroupAvatarRoot20 : GroupAvatarRoot32;
    return <Root style={{ backgroundColor: deterministicColor(props.group.title) }}>{props.group.title[0]}</Root>;
}

const GroupAvatarRoot20 = styled.div`
    font-size: 20px;
    line-height: 32px;
    text-align: center;
    text-transform: uppercase;
    width: 32px;
    height: 32px;
    border-radius: 4px;
    outline: 1px solid ${theme.borderLineColor2};
    outline-offset: -1px;
`;

const GroupAvatarRoot32 = styled.div`
    font-size: 24px;
    line-height: 40px;
    text-align: center;
    text-transform: uppercase;
    width: 40px;
    height: 40px;
    border-radius: 4px;
    outline: 1px solid ${theme.borderLineColor2};
    outline-offset: -1px;
`;

function deterministicColor(input: string) {
    let hash = 0;
    for (let i = 0; i < input.length; i++) {
        hash = input.charCodeAt(i) + ((hash << 5) - hash);
    }
    const color = (hash & 0x00ffffff).toString(16).toUpperCase();
    return `#${"000000".substring(0, 6 - color.length) + color}20`;
}

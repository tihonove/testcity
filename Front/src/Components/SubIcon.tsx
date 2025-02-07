import * as React from "react";
import styled from "styled-components";

export function SubIcon({ children, sub }: { children: React.ReactNode; sub: React.ReactNode }): React.JSX.Element {
    return (
        <IconContainer>
            <MainIcon>{children}</MainIcon>
            <SubIconContainer>{sub}</SubIconContainer>
        </IconContainer>
    );
}

const IconContainer = styled.div`
    position: relative;
    display: inline-block;
`;

const MainIcon = styled.div`
    position: relative;
    z-index: 1;
    overflow: hidden;
    clip-path: polygon(
        0 0,
        100% 0,
        100% calc(100% - 10px),
        calc(100% - 10px) calc(100% - 10px),
        calc(100% - 10px) 100%,
        0 100%,
        0 0
    );
`;

const SubIconContainer = styled.div`
    position: absolute;
    bottom: -3px;
    right: -3px;
    transform: scale(0.8);
    transform-origin: bottom right;
    z-index: 2;
`;

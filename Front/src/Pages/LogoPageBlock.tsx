import * as React from "react";
import styled from "styled-components";
import Logo from "../Components/Logo";
import { useBasePrefix } from "../Domain/Navigation";
import { theme } from "../Theme/ITheme";
import { Link } from "react-router-dom";

export function LogoPageBlock() {
    const basePrefix = useBasePrefix();
    return (
        <FixedLogoRoot to={basePrefix}>
            <Logo />
            TestCity
        </FixedLogoRoot>
    );
}

const FixedLogoRoot = styled(Link)`
    position: fixed;
    top: 10px;
    left: 10px;
    line-height: 32px;
    font-size: 20px;
    display: flex;
    text-decoration: none;
    color: ${theme.primaryTextColor};
`;

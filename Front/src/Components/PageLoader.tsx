import * as React from "react";
import { NetCloudIcon32Regular } from "@skbkontur/icons";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";
import { Loader, Spinner } from "@skbkontur/react-ui";

export const PageLoader = () => (
    <LoaderContainer>
        <LoaderMessage>
            <Spinner type="big" caption="Loading" />
        </LoaderMessage>
    </LoaderContainer>
);

const LoaderContainer = styled.div`
    display: flex;
    justify-content: center;
    align-items: center;
    height: 100vh;
`;

const LoaderMessage = styled.h2`
    font-size: 32px;
    line-height: 40px;
    color: ${theme.primaryTextColor};
`;

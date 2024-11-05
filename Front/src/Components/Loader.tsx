import * as React from "react";
import {NetCloudIcon32Regular} from "@skbkontur/icons";
import styled from "styled-components";

export const Loader = () => (
    <LoaderContainer>
        <LoaderMessage><NetCloudIcon32Regular/> Loading ...</LoaderMessage>
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
    color: #333;
`;

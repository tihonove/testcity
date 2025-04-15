import * as React from "react";
import styled, { keyframes } from "styled-components";
import { UiLoadSpinnerIcon16Light } from "@skbkontur/icons";

const rotate = keyframes`
    100% {
        transform: rotate(360deg);
    }
`;

const SpinnerWrapper = styled.span`
    display: inline-flex;
    animation: ${rotate} 2s linear infinite;
`;

export function RotatingSpinner() {
    return (
        <SpinnerWrapper>
            <UiLoadSpinnerIcon16Light />
        </SpinnerWrapper>
    );
}

import { CheckCircleIcon16Light, MinusCircleIcon16Light, WarningCircleIcon16Light } from "@skbkontur/icons";
import * as React from "react";
import { Link, To } from "react-router-dom";
import styled from "styled-components";

export function JobLink(props: { state: string; to: To; children: React.ReactNode }) {
    return (
        <JobLinkWithResults $state={props.state} to={props.to}>
            {props.state == "Success" ? (
                <CheckCircleIcon16Light />
            ) : props.state == "Canceled" ? (
                <MinusCircleIcon16Light />
            ) : (
                <WarningCircleIcon16Light />
            )}

            {props.children}
        </JobLinkWithResults>
    );
}

const JobLinkWithResults = styled(Link)<{ $state: string }>`
    color: ${props =>
        props.$state == "Success"
            ? props.theme.successTextColor
            : props.$state == "Canceled"
              ? props.theme.mutedTextColor
              : props.theme.failedTextColor};
    text-decoration: none;
    &:hover {
        text-decoration: underline;
    }
`;

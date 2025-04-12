import * as React from "react";
import { CommitRowProps } from "./JobRunTestListPage";
import { theme } from "../Theme/ITheme";
import styled from "styled-components";
import { GravatarImage } from "../Components/GravatarImage";
import { Fit, RowStack } from "@skbkontur/react-stack-layout";

export function CommitRow(props: CommitRowProps) {
    return (
        <RowStack gap={2} block>
            <Fit>
                <StyledGravatarImage email={props.authorEmail} size={32} alt={props.authorName} />
            </Fit>
            <Fit>
                <Message>{props.messagePreview}</Message>
                <Details>
                    {props.authorName}·#{props.sha.substring(0, 7)}
                </Details>
            </Fit>
        </RowStack>
    );
}

const Message = styled.div`
    line-height: 20px;
`;

const Details = styled.div`
    font-size: ${theme.smallTextSize};
`;

const StyledGravatarImage = styled(GravatarImage)`
    width: 32px;
    height: 32px;
    border-radius: 4px;
    margin-right: 4px;
    vertical-align: middle;
`;

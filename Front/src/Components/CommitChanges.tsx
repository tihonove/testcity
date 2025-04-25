import * as React from "react";
import styled from "styled-components";
import { Tooltip } from "@skbkontur/react-ui";
import { theme } from "../Theme/ITheme";
import { CoveredCommitsColumn } from "../Domain/Storage/CoveredCommitsColumn";

interface CommitChangesProps {
    totalCoveredCommitCount: number;
    coveredCommits: CoveredCommitsColumn;
}

export function CommitChanges({ totalCoveredCommitCount, coveredCommits }: CommitChangesProps): React.JSX.Element {
    if (totalCoveredCommitCount <= 0) {
        return <NoChanges>No changes</NoChanges>;
    }

    return (
        <Tooltip
            trigger="click"
            render={() => (
                <ChangesList>
                    <tbody>
                        {coveredCommits.map((commit, index) =>
                            Array.isArray(commit) ? (
                                <ChangeItem key={index}>
                                    <Message>{commit[3]}</Message>
                                    <Author>
                                        {commit[1]} &lt;
                                        {commit[2]}
                                        &gt;
                                    </Author>
                                    <Sha>{commit[0].substring(0, 7)}</Sha>
                                </ChangeItem>
                            ) : (
                                <ChangeItem key={index}>
                                    <Message>{commit.MessagePreview}</Message>
                                    <Author>
                                        {commit.AuthorName} &lt;
                                        {commit.AuthorEmail}
                                        &gt;
                                    </Author>
                                    <Sha>{commit.CommitSha.substring(0, 7)}</Sha>
                                </ChangeItem>
                            )
                        )}
                    </tbody>
                </ChangesList>
            )}>
            <ChangesLink>
                {totalCoveredCommitCount} change
                {totalCoveredCommitCount !== 1 ? "s" : ""}
            </ChangesLink>
        </Tooltip>
    );
}

const ChangesLink = styled.span`
    cursor: pointer;
    text-decoration: underline;

    &:hover {
        color: ${theme.activeLinkColor};
        text-decoration: underline;
    }
`;

const ChangesList = styled.table`
    padding: 10px;
    max-width: 800px;
`;

const ChangeItem = styled.tr``;

const Message = styled.td`
    font-weight: 600;
    max-width: 300px;
    font-size: 12px;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
`;

const Author = styled.td`
    display: inline-block;
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const Sha = styled.td`
    font-size: 12px;
    padding-left: 5px;
    color: ${theme.mutedTextColor};
`;

const NoChanges = styled.span`
    color: ${theme.mutedTextColor};
`;

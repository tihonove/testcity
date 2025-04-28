import * as React from "react";
import { Tooltip } from "@skbkontur/react-ui";
import { CoveredCommitsColumn } from "../Domain/Storage/CoveredCommitsColumn";
import style from "./CommitChanges.module.css";

interface CommitChangesProps {
    totalCoveredCommitCount: number;
    coveredCommits: CoveredCommitsColumn;
}

export function CommitChanges({ totalCoveredCommitCount, coveredCommits }: CommitChangesProps): React.JSX.Element {
    if (totalCoveredCommitCount <= 0) {
        return <span className={style.noChanges}>No changes</span>;
    }

    return (
        <Tooltip
            trigger="click"
            render={() => (
                <table className={style.changesList}>
                    <tbody>
                        {coveredCommits.map((commit, index) =>
                            Array.isArray(commit) ? (
                                <tr className={style.changeItem} key={index}>
                                    <td className={style.message}>{commit[3]}</td>
                                    <td className={style.author}>
                                        {commit[1]} &lt;
                                        {commit[2]}
                                        &gt;
                                    </td>
                                    <td className={style.sha}>{commit[0].substring(0, 7)}</td>
                                </tr>
                            ) : (
                                <tr className={style.changeItem} key={index}>
                                    <td className={style.message}>{commit.MessagePreview}</td>
                                    <td className={style.author}>
                                        {commit.AuthorName} &lt;
                                        {commit.AuthorEmail}
                                        &gt;
                                    </td>
                                    <td className={style.sha}>{commit.CommitSha.substring(0, 7)}</td>
                                </tr>
                            )
                        )}
                    </tbody>
                </table>
            )}>
            <span className={style.changesLink}>
                {totalCoveredCommitCount} change
                {totalCoveredCommitCount !== 1 ? "s" : ""}
            </span>
        </Tooltip>
    );
}

import { ClipboardTextIcon16Regular, CopyIcon16Light, TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { Button, DropdownMenu, Hint, MenuItem, Toast } from "@skbkontur/react-ui";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import styled from "styled-components";

import { useStorage } from "../ClickhouseClientHooksWrapper";
import { createLinkToTestHistory } from "../Domain/Navigation";
import { TestRunQueryRow, TestRunQueryRowNames } from "../Domain/Storage/TestRunQuery";
import { theme } from "../Theme/ITheme";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { KebabButton } from "./KebabButton";
import { formatDuration } from "./RunStatisticsChart/DurationUtils";
import { RunStatus } from "./RunStatus";
import { TestName } from "./TestName";

interface TestRunRowProps {
    testRun: TestRunQueryRow;
    basePrefix: string;
    pathToProject: string[];
    jobRunIds: string[];
    onSetSearchTextImmediate: (value: string) => void;
    onSetOutputModalIds: (ids: [string, string] | undefined) => void;
}

export function TestRunRow({
    testRun,
    basePrefix,
    pathToProject,
    jobRunIds,
    onSetSearchTextImmediate,
    onSetOutputModalIds,
}: TestRunRowProps): React.JSX.Element {
    const [expandOutput, setExpandOutput] = React.useState(false);
    const [[failedOutput, failedMessage, systemOutput], setOutputValues] = React.useState(["", "", ""]);
    const storage = useStorage();
    React.useEffect(() => {
        if (expandOutput && !failedOutput && !failedMessage && !systemOutput)
            runAsyncAction(async () => {
                setOutputValues(
                    await storage.getFailedTestOutput(
                        testRun[TestRunQueryRowNames.JobId],
                        testRun[TestRunQueryRowNames.TestId],
                        jobRunIds
                    )
                );
            });
    }, [expandOutput]);

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [
                testRun[TestRunQueryRowNames.TestId],
                "---",
                failedMessage,
                "---",
                failedOutput,
                "---",
                systemOutput,
            ].join("\n");
            await navigator.clipboard.writeText(textToCopy);
            Toast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, testRun[TestRunQueryRowNames.TestId]]);
    const statuses = testRun[TestRunQueryRowNames.AllStates].split(",");
    const failureCount = statuses.filter(x => x === "Failed").length;
    const successCount = statuses.filter(x => x === "Success").length;
    return (
        <>
            <TestRunsTableRow>
                <StatusCell status={testRun[TestRunQueryRowNames.State]}>
                    {testRun[TestRunQueryRowNames.State]}
                    {testRun[TestRunQueryRowNames.TotalRuns] > 1 && (
                        <RunCountInfo>
                            {" ("}
                            {failureCount == 0 && successCount > 0 ? (
                                <SuccessCount>{successCount}</SuccessCount>
                            ) : failureCount > 0 && successCount == 0 ? (
                                <FailedCount>{failureCount}</FailedCount>
                            ) : (
                                <>
                                    <FailedCount>{failureCount}</FailedCount>/
                                    <SuccessCount>{successCount}</SuccessCount>
                                </>
                            )}
                            )
                        </RunCountInfo>
                    )}
                </StatusCell>
                <TestNameCell>
                    <TestName
                        onTestNameClick={
                            testRun[TestRunQueryRowNames.State] === "Failed"
                                ? () => {
                                      setExpandOutput(!expandOutput);
                                  }
                                : undefined
                        }
                        onSetSearchValue={x => {
                            onSetSearchTextImmediate(x);
                        }}
                        value={testRun[TestRunQueryRowNames.TestId]}
                    />
                </TestNameCell>
                <DurationCell>
                    {formatDuration(
                        testRun[TestRunQueryRowNames.AvgDuration],
                        testRun[TestRunQueryRowNames.AvgDuration]
                    )}
                </DurationCell>
                <ActionsCell>
                    <DropdownMenu caption={<KebabButton />}>
                        <MenuItem
                            icon={<TimeClockFastIcon16Regular />}
                            href={createLinkToTestHistory(
                                basePrefix,
                                testRun[TestRunQueryRowNames.TestId],
                                pathToProject
                            )}>
                            Show test history
                        </MenuItem>
                        <MenuItem
                            icon={<ClipboardTextIcon16Regular />}
                            disabled={testRun[TestRunQueryRowNames.State] !== "Failed"}
                            onClick={() => {
                                onSetOutputModalIds([
                                    testRun[TestRunQueryRowNames.TestId],
                                    testRun[TestRunQueryRowNames.JobId],
                                ]);
                            }}>
                            Show test outpout
                        </MenuItem>
                    </DropdownMenu>
                </ActionsCell>
            </TestRunsTableRow>
            <TestOutputRow $expanded={expandOutput}>
                <TestOutputCell colSpan={4}>
                    {expandOutput && (failedOutput || failedMessage || systemOutput) && (
                        <>
                            <RowStack block>
                                <Fill />
                                <Fit style={{ fontSize: "12px" }}>
                                    <Button onClick={handleCopyToClipboard} use="link" icon={<CopyIcon16Light />}>
                                        Copy to clipboard
                                    </Button>
                                </Fit>
                            </RowStack>
                            <Code>
                                {failedOutput}
                                ---
                                {failedMessage}
                                ---
                                {systemOutput}
                            </Code>
                            <a
                                href="#"
                                onClick={e => {
                                    onSetOutputModalIds([
                                        testRun[TestRunQueryRowNames.TestId],
                                        testRun[TestRunQueryRowNames.JobId],
                                    ]);
                                    e.preventDefault();
                                    return false;
                                }}>
                                Open in modal window
                            </a>
                        </>
                    )}
                </TestOutputCell>
            </TestOutputRow>
        </>
    );
}

const TestRunsTableRow = styled.tr({
    td: {
        textAlign: "left",
        padding: "6px 8px",
    },
});

const TestOutputRow = styled.tr<{ $expanded?: boolean }>`
    max-height: ${props => (props.$expanded ? "none" : "0")};

    & > td {
        padding: ${props => (props.$expanded ? "6px 0 6px 24px" : "0 0 0 24px")};
    }
`;

const TestOutputCell = styled.td``;

const Code = styled.pre`
    font-size: 14px;
    line-height: 18px;
    max-height: 800px;
    margin-top: 5px;
    margin-bottom: 5px;
    overflow: hidden;
    padding: 15px;
    border: 1px solid ${theme.borderLineColor2};
`;

const DurationCell = styled.td`
    width: 80px;
`;

const ActionsCell = styled.td`
    width: 20px;
`;

const TestNameCell = styled.td`
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
`;

const CountInfo = styled.span`
    color: ${theme.mutedTextColor};
    margin-left: 5px;
`;

const RunCountInfo = styled.span`
    // font-size: ${theme.smallTextSize};
    color: ${theme.mutedTextColor};
`;

const SuccessCount = styled.span`
    color: ${theme.successTextColor};
`;

const FailedCount = styled.span`
    color: ${theme.failedTextColor};
`;

const StatusCell = styled.td<{ status: RunStatus }>`
    width: 100px;
    color: ${props =>
        props.status == "Success"
            ? props.theme.successTextColor
            : props.status == "Failed"
              ? props.theme.failedTextColor
              : undefined};
`;

import { ClipboardTextIcon16Regular, CopyIcon16Light, TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { Button, DropdownMenu, Hint, MenuItem, Toast } from "@skbkontur/react-ui";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import styles from "./TestRunRow.module.css";

import { useStorage } from "../ClickhouseClientHooksWrapper";
import { createLinkToTestHistory } from "../Domain/Navigation";
import { TestRunQueryRow, TestRunQueryRowNames } from "../Domain/Storage/TestRunQuery";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { KebabButton } from "./KebabButton";
import { formatDuration } from "./RunStatisticsChart/DurationUtils";
import { RunStatus } from "./RunStatus";
import { TestName } from "./TestName";
import { Link } from "react-router-dom";

interface TestRunRowProps {
    testRun: TestRunQueryRow;
    basePrefix: string;
    pathToProject: string[];
    jobRunIds: string[];
    onSetSearchTextImmediate: (value: string) => void;
    onSetOutputModalIds: (ids: [string, string] | undefined) => void;
    flakyTestNames: Set<string>;
}

export function TestRunRow({
    testRun,
    basePrefix,
    pathToProject,
    jobRunIds,
    onSetSearchTextImmediate,
    onSetOutputModalIds,
    flakyTestNames,
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

            // eslint-disable-next-line @typescript-eslint/no-deprecated
            Toast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, testRun[TestRunQueryRowNames.TestId]]);
    const statuses = testRun[TestRunQueryRowNames.AllStates].split(",");
    const failureCount = statuses.filter(x => x === "Failed").length;
    const successCount = statuses.filter(x => x === "Success").length;

    const getStatusCellClass = (status: RunStatus) => {
        const baseClass = styles.statusCellDefault;
        if (status === "Success") return `${baseClass} ${styles.statusCellSuccess}`;
        if (status === "Failed") return `${baseClass} ${styles.statusCellFailed}`;
        return baseClass;
    };

    return (
        <>
            <tr className={styles.testRunsTableRow}>
                <td className={getStatusCellClass(testRun[TestRunQueryRowNames.State])}>
                    {testRun[TestRunQueryRowNames.State]}
                    {testRun[TestRunQueryRowNames.TotalRuns] > 1 && (
                        <span className={styles.runCountInfo}>
                            {" ("}
                            {failureCount == 0 && successCount > 0 ? (
                                <span className={styles.successCount}>{successCount}</span>
                            ) : failureCount > 0 && successCount == 0 ? (
                                <span className={styles.failedCount}>{failureCount}</span>
                            ) : (
                                <>
                                    <span className={styles.failedCount}>{failureCount}</span>/
                                    <span className={styles.successCount}>{successCount}</span>
                                </>
                            )}
                            )
                        </span>
                    )}
                </td>
                <td className={styles.testNameCell}>
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
                        isFlaky={flakyTestNames.has(testRun[TestRunQueryRowNames.TestId])}
                    />
                </td>
                <td className={styles.durationCell}>
                    {formatDuration(
                        testRun[TestRunQueryRowNames.AvgDuration],
                        testRun[TestRunQueryRowNames.AvgDuration]
                    )}
                </td>
                <td className={styles.actionsCell}>
                    <div className={styles.actionsList}>
                        {testRun[TestRunQueryRowNames.State] === "Failed" && (
                            <>
                                <a
                                    href="#output"
                                    onClick={e => {
                                        onSetOutputModalIds([
                                            testRun[TestRunQueryRowNames.TestId],
                                            testRun[TestRunQueryRowNames.JobId],
                                        ]);
                                        e.preventDefault();
                                        return false;
                                    }}>
                                    <ClipboardTextIcon16Regular />
                                    Test output
                                </a>
                                &nbsp; &nbsp; &nbsp;
                            </>
                        )}
                        <Link
                            to={createLinkToTestHistory(
                                basePrefix,
                                testRun[TestRunQueryRowNames.TestId],
                                pathToProject
                            )}>
                            <TimeClockFastIcon16Regular />
                            Test history
                        </Link>
                    </div>
                    <div className={styles.dropdownMenu}>
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
                    </div>
                </td>
            </tr>
            <tr className={expandOutput ? styles.testOutputRowExpanded : styles.testOutputRow}>
                <td className={styles.testOutputCell} colSpan={4}>
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
                            <pre className={styles.code}>
                                {failedOutput}
                                ---
                                {failedMessage}
                                ---
                                {systemOutput}
                            </pre>
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
                </td>
            </tr>
        </>
    );
}

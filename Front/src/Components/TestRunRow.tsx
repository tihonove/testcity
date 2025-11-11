import { ClipboardTextIcon16Regular, CopyIcon16Light, TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { Button, DropdownMenu, Hint, MenuItem, Toast } from "@skbkontur/react-ui";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import * as React from "react";
import styles from "./TestRunRow.module.css";

import { createLinkToTestHistory } from "../Domain/Navigation";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { KebabButton } from "./KebabButton";
import { formatDuration } from "./RunStatisticsChart/DurationUtils";
import { RunStatus } from "./RunStatus";
import { TestName } from "./TestName";
import { Link } from "react-router-dom";
import { useTestCityClient } from "../Domain/Api/TestCityApiClient";
import { TestOutput } from "../Domain/Api/TestCityRunsApiClient";
import { TestRun } from "../Domain/ProjectDashboardNode";

interface TestRunRowProps {
    testRun: TestRun;
    basePrefix: string;
    pathToProject: string[];
    jobRunId: string;
    onSetSearchTextImmediate: (value: string) => void;
    onSetOutputModalIds: (ids: [string, string] | undefined) => void;
    flakyTestNames: Set<string>;
}

export function TestRunRow({
    testRun,
    basePrefix,
    pathToProject,
    jobRunId,
    onSetSearchTextImmediate,
    onSetOutputModalIds,
    flakyTestNames,
}: TestRunRowProps): React.JSX.Element {
    const [expandOutput, setExpandOutput] = React.useState(false);
    const [{ failureOutput, failureMessage, systemOutput }, setOutputValues] = React.useState<TestOutput>({
        failureOutput: null,
        failureMessage: null,
        systemOutput: null,
    });
    const client = useTestCityClient();
    React.useEffect(() => {
        if (expandOutput && !failureOutput && !failureMessage && !systemOutput)
            runAsyncAction(async () => {
                setOutputValues(
                    await client.runs.getTestOutput(pathToProject, testRun.jobId, jobRunId, testRun.testId)
                );
            });
    }, [expandOutput]);

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [testRun.testId, "---", failureMessage, "---", failureOutput, "---", systemOutput].join(
                "\n"
            );
            await navigator.clipboard.writeText(textToCopy);

            // eslint-disable-next-line @typescript-eslint/no-deprecated
            Toast.push("Copied to clipboard");
        });
    }, [failureMessage, failureOutput, systemOutput, testRun.testId]);
    const statuses = testRun.allStates.split(",");
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
                <td className={getStatusCellClass(testRun.finalState)}>
                    {testRun.finalState}
                    {testRun.totalRuns > 1 && (
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
                            testRun.finalState === "Failed"
                                ? () => {
                                      setExpandOutput(!expandOutput);
                                  }
                                : undefined
                        }
                        onSetSearchValue={x => {
                            onSetSearchTextImmediate(x);
                        }}
                        value={testRun.testId}
                        isFlaky={flakyTestNames.has(testRun.testId)}
                    />
                </td>
                <td className={styles.durationCell}>{formatDuration(testRun.avgDuration, testRun.avgDuration)}</td>
                <td className={styles.actionsCell}>
                    <div className={styles.actionsList}>
                        {testRun.finalState === "Failed" && (
                            <>
                                <a
                                    href="#output"
                                    onClick={e => {
                                        onSetOutputModalIds([testRun.testId, testRun.jobId]);
                                        e.preventDefault();
                                        return false;
                                    }}>
                                    <ClipboardTextIcon16Regular />
                                    Test output
                                </a>
                                &nbsp; &nbsp; &nbsp;
                            </>
                        )}
                        <Link to={createLinkToTestHistory(basePrefix, testRun.testId, pathToProject)}>
                            <TimeClockFastIcon16Regular />
                            Test history
                        </Link>
                    </div>
                    <div className={styles.dropdownMenu}>
                        <DropdownMenu caption={<KebabButton />}>
                            <MenuItem
                                icon={<TimeClockFastIcon16Regular />}
                                href={createLinkToTestHistory(basePrefix, testRun.testId, pathToProject)}>
                                Show test history
                            </MenuItem>
                            <MenuItem
                                icon={<ClipboardTextIcon16Regular />}
                                disabled={testRun.finalState !== "Failed"}
                                onClick={() => {
                                    onSetOutputModalIds([testRun.testId, testRun.jobId]);
                                }}>
                                Show test outpout
                            </MenuItem>
                        </DropdownMenu>
                    </div>
                </td>
            </tr>
            <tr className={expandOutput ? styles.testOutputRowExpanded : styles.testOutputRow}>
                <td className={styles.testOutputCell} colSpan={4}>
                    {expandOutput && (failureOutput || failureMessage || systemOutput) && (
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
                                {failureOutput}
                                ---
                                {failureMessage}
                                ---
                                {systemOutput}
                            </pre>
                            <a
                                href="#"
                                onClick={e => {
                                    onSetOutputModalIds([testRun.testId, testRun.jobId]);
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

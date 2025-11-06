import { Button, SingleToast } from "@skbkontur/react-ui";
import * as React from "react";

import { ClipboardTextIcon16Regular, CopyIcon16Light, TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Toast } from "@skbkontur/react-ui";
import { Suspense } from "react";
import styles from "./FailedTestListView.module.css";
import { useStorage, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { createLinkToTestHistory, useBasePrefix } from "../Domain/Navigation";
import { TestRunQueryRowNames } from "../Domain/Storage/TestRunQuery";

import { TestRunQueryRow } from "../Domain/Storage/TestRunQuery";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { splitTestName } from "./TestName";
import { Spoiler } from "./Spoiler";
import { FlakyTestBadge } from "./FlakyTestBadge";
import { Link } from "react-router-dom";
import { TestOutputModal } from "./TestOutputModal";

interface FailedTestListViewProps {
    projectId: string;
    jobId: string;
    jobRunId: string;
    pathToProject: string[];
    failedTestsCount: number;
    linksBlock?: React.ReactNode;
}

export function FailedTestListView(props: FailedTestListViewProps): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const testList = useStorageQuery(
        s => s.getTestList([props.jobRunId], undefined, undefined, "", "Failed", 50, 0),
        [props.jobRunId]
    );

    const flakyTestNamesArray = useStorageQuery(
        s => s.getFlakyTestNames(props.projectId, props.jobId),
        [props.projectId, props.jobId]
    );

    const flakyTestNamesSet = React.useMemo(() => {
        return new Set(flakyTestNamesArray);
    }, [flakyTestNamesArray]);

    const groupedTests = React.useMemo(() => {
        const groups: Record<string, TestRunQueryRow[]> = {};

        testList.forEach(test => {
            const [prefix] = splitTestName(test[TestRunQueryRowNames.TestId]);
            if (!(prefix in groups)) {
                groups[prefix] = [];
            }
            groups[prefix].push(test);
        });

        return groups;
    }, [testList]);

    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            {Object.entries(groupedTests).map(([prefix, tests]) => (
                <div key={prefix} style={{ marginBottom: "10px" }}>
                    <Spoiler
                        iconSize={16}
                        title={<span className={styles.groupTitle}>{prefix || "Без префикса"}</span>}
                        openedByDefault={true}>
                        {tests.map((test, i) => (
                            <TestRunRow
                                key={i.toString() + test[TestRunQueryRowNames.TestId]}
                                testRun={test}
                                jobRunId={props.jobRunId}
                                basePrefix={basePrefix}
                                pathToProject={props.pathToProject}
                                flakyTestNames={flakyTestNamesSet}
                            />
                        ))}
                    </Spoiler>
                </div>
            ))}
        </Suspense>
    );
}

interface TestRunRowProps {
    testRun: TestRunQueryRow;
    basePrefix: string;
    pathToProject: string[];
    jobRunId: string;
    flakyTestNames: Set<string>;
}

export function TestRunRow({
    testRun,
    basePrefix,
    pathToProject,
    jobRunId,
    flakyTestNames,
}: TestRunRowProps): React.JSX.Element {
    const [expandOutput, setExpandOutput] = React.useState(false);
    const [[failedOutput, failedMessage, systemOutput], setOutputValues] = React.useState(["", "", ""]);
    const storage = useStorage();
    const [showOutputModal, setShowOutputModal] = React.useState(false);

    React.useEffect(() => {
        if (expandOutput && !failedOutput && !failedMessage && !systemOutput)
            runAsyncAction(async () => {
                setOutputValues(
                    await storage.getFailedTestOutput(
                        testRun[TestRunQueryRowNames.JobId],
                        testRun[TestRunQueryRowNames.TestId],
                        [jobRunId]
                    )
                );
            });
    }, [expandOutput]);

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [testRun[TestRunQueryRowNames.TestId], failedMessage, failedOutput, systemOutput]
                .filter(x => x)
                .join("------\n");
            await navigator.clipboard.writeText(textToCopy);
            // eslint-disable-next-line @typescript-eslint/no-deprecated
            SingleToast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, testRun[TestRunQueryRowNames.TestId]]);

    const [prefix, name] = splitTestName(testRun[TestRunQueryRowNames.TestId]);
    const isFlaky = flakyTestNames.has(testRun[TestRunQueryRowNames.TestId]);

    return (
        <div className={styles.testRunRowContainer}>
            {showOutputModal && (
                <TestOutputModal
                    testId={testRun[TestRunQueryRowNames.TestId]}
                    jobId={testRun[TestRunQueryRowNames.JobId]}
                    jobRunIds={[jobRunId]}
                    onClose={() => {
                        setShowOutputModal(false);
                    }}
                />
            )}
            <div className={styles.testRunRowHeader}>
                <div className={styles.testRunRowContent}>
                    <a
                        className={styles.testRunRowLink}
                        href="#test"
                        onClick={e => {
                            setExpandOutput(!expandOutput);
                            e.preventDefault();
                            return false;
                        }}>
                        {name}
                    </a>
                    {isFlaky && <FlakyTestBadge />}
                </div>
                <div className={styles.testOutputLink}>
                    <a
                        href="#output"
                        onClick={e => {
                            setShowOutputModal(true);
                            e.preventDefault();
                            return false;
                        }}>
                        <ClipboardTextIcon16Regular />
                        Test output
                    </a>
                </div>
                <div className={styles.testHistoryLink}>
                    <Link to={createLinkToTestHistory(basePrefix, testRun[TestRunQueryRowNames.TestId], pathToProject)}>
                        <TimeClockFastIcon16Regular /> Test history
                    </Link>
                </div>
            </div>
            <div className={expandOutput ? styles.testOutputRowExpanded : styles.testOutputRow}>
                {expandOutput && (failedOutput || failedMessage || systemOutput) && (
                    <>
                        <RowStack block>
                            <Fill />
                            <Fit className={styles.copyButtonContainer}>
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
                    </>
                )}
            </div>
        </div>
    );
}

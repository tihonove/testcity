import { Button, SingleToast } from "@skbkontur/react-ui";
import * as React from "react";

import { ClipboardTextIcon16Regular, CopyIcon16Light, TimeClockFastIcon16Regular } from "@skbkontur/icons";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Suspense } from "react";
import { createLinkToTestHistory, useBasePrefix } from "../Domain/Navigation";
import styles from "./FailedTestListView.module.css";

import { Link } from "react-router-dom";
import { useTestCityClient, useTestCityRequest } from "../Domain/Api/TestCityApiClient";
import { TestOutput } from "../Domain/Api/TestCityRunsApiClient";
import { TestRun } from "../Domain/ProjectDashboardNode";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { FlakyTestBadge } from "./FlakyTestBadge";
import { Spoiler } from "./Spoiler";
import { splitTestName } from "./TestName";
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
    const testList = useTestCityRequest(
        c =>
            c.runs.getTestList(props.pathToProject, props.jobId, props.jobRunId, {
                testStateFilter: "Failed",
                itemsPerPage: 50,
                page: 0,
            }),
        [props.jobRunId]
    );

    const flakyTestNamesArray = useTestCityRequest(
        s => s.runs.getFlakyTestsNames(props.pathToProject, props.jobId),
        [props.pathToProject, props.jobId]
    );

    const flakyTestNamesSet = React.useMemo(() => {
        return new Set(flakyTestNamesArray);
    }, [flakyTestNamesArray]);

    const groupedTests = React.useMemo(() => {
        const groups: Record<string, TestRun[]> = {};

        testList.forEach(test => {
            const [prefix] = splitTestName(test.testId);
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
                                key={i.toString() + test.testId}
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
    testRun: TestRun;
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
    const [{ failureOutput, failureMessage, systemOutput }, setOutputValues] = React.useState<TestOutput>({
        failureOutput: null,
        failureMessage: null,
        systemOutput: null,
    });
    const client = useTestCityClient();
    const [showOutputModal, setShowOutputModal] = React.useState(false);

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
            const textToCopy = [testRun.testId, failureMessage, failureOutput, systemOutput]
                .filter(x => x)
                .join("------\n");
            await navigator.clipboard.writeText(textToCopy);
            // eslint-disable-next-line @typescript-eslint/no-deprecated
            SingleToast.push("Copied to clipboard");
        });
    }, [failureMessage, failureOutput, systemOutput, testRun.testId]);

    const [prefix, name] = splitTestName(testRun.testId);
    const isFlaky = flakyTestNames.has(testRun.testId);

    return (
        <div className={styles.testRunRowContainer}>
            {showOutputModal && (
                <TestOutputModal
                    pathToProject={pathToProject}
                    testId={testRun.testId}
                    jobId={testRun.jobId}
                    jobRunId={jobRunId}
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
                    <Link to={createLinkToTestHistory(basePrefix, testRun.testId, pathToProject)}>
                        <TimeClockFastIcon16Regular /> Test history
                    </Link>
                </div>
            </div>
            <div className={expandOutput ? styles.testOutputRowExpanded : styles.testOutputRow}>
                {expandOutput && (failureOutput || failureMessage || systemOutput) && (
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
                            {failureOutput}
                            ---
                            {failureMessage}
                            ---
                            {systemOutput}
                        </pre>
                    </>
                )}
            </div>
        </div>
    );
}

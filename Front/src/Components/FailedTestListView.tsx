import { Button } from "@skbkontur/react-ui";
import * as React from "react";

import { CopyIcon16Light } from "@skbkontur/icons";
import { Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { Toast } from "@skbkontur/react-ui";
import { Suspense } from "react";
import styles from "./FailedTestListView.module.css";
import { useStorage, useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { useBasePrefix } from "../Domain/Navigation";
import { TestRunQueryRowNames } from "../Domain/Storage/TestRunQuery";

import { TestRunQueryRow } from "../Domain/Storage/TestRunQuery";
import { runAsyncAction } from "../Utils/TypeHelpers";
import { splitTestName } from "./TestName";

interface FailedTestListViewProps {
    jobRunIds: string[];
    pathToProject: string[];
    failedTestsCount: number;
    linksBlock?: React.ReactNode;
}

export function FailedTestListView(props: FailedTestListViewProps): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const testList = useStorageQuery(
        s => s.getTestList(props.jobRunIds, undefined, undefined, "", "Failed", 50, 0),
        [props.jobRunIds]
    );

    return (
        <Suspense fallback={<div className="loading">Загрузка...</div>}>
            {testList.map((x, i) => (
                <TestRunRow
                    key={i.toString() + x[TestRunQueryRowNames.TestId]}
                    testRun={x}
                    jobRunIds={props.jobRunIds}
                    basePrefix={basePrefix}
                    pathToProject={props.pathToProject}
                />
            ))}
        </Suspense>
    );
}

interface TestRunRowProps {
    testRun: TestRunQueryRow;
    basePrefix: string;
    pathToProject: string[];
    jobRunIds: string[];
}

export function TestRunRow({ testRun, basePrefix, pathToProject, jobRunIds }: TestRunRowProps): React.JSX.Element {
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

    const [prefix, name] = splitTestName(testRun[TestRunQueryRowNames.TestId]);
    return (
        <>
            <a
                href="#"
                onClick={e => {
                    setExpandOutput(!expandOutput);
                    e.preventDefault();
                    return false;
                }}>
                {name}
            </a>{" "}
            <span className={styles.testNamePrefix}>({prefix})</span>
            <div className={expandOutput ? styles.testOutputRowExpanded : styles.testOutputRow}>
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
                    </>
                )}
            </div>
        </>
    );
}

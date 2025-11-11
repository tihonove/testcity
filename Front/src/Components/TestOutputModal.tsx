import * as React from "react";
import { Button, Modal, Toast } from "@skbkontur/react-ui";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { CopyIcon16Light } from "@skbkontur/icons";
import { runAsyncAction } from "../Utils/TypeHelpers";
import styles from "./TestOutputModal.module.css";
import { useTestCityRequest } from "../Domain/Api/TestCityApiClient";

interface TestOutputModalProps {
    pathToProject: string[];
    jobId: string;
    jobRunId: string;
    testId: string;
    onClose: () => void;
}

export function TestOutputModal(props: TestOutputModalProps): React.JSX.Element {
    const { failureOutput, failureMessage, systemOutput } = useTestCityRequest(
        x => x.runs.getTestOutput(props.pathToProject, props.jobId, props.jobRunId, props.testId),
        [props.pathToProject, props.jobId, props.testId, props.jobRunId]
    );

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [props.testId, "---", failureMessage, "---", failureOutput, "---", systemOutput].join(
                "\n"
            );
            await navigator.clipboard.writeText(textToCopy);

            // eslint-disable-next-line @typescript-eslint/no-deprecated
            Toast.push("Copied to clipboard");
        });
    }, [failureMessage, failureOutput, systemOutput, props.testId]);

    return (
        <Modal onClose={props.onClose} width="1000px">
            <Modal.Header>Test Output</Modal.Header>
            <Modal.Body>
                <ColumnStack block gap={2} stretch>
                    <Fit>
                        <h3 className={styles.testId}>{props.testId}</h3>
                    </Fit>
                    <Fit>
                        <RowStack block>
                            <Fill />
                            <Fit>
                                <Button onClick={handleCopyToClipboard} use="link" icon={<CopyIcon16Light />}>
                                    Copy to clipboard
                                </Button>
                            </Fit>
                        </RowStack>
                    </Fit>
                    <Fit>
                        <pre className={styles.code}>
                            {failureMessage}
                            ---
                            {failureOutput}
                            ---
                            {systemOutput}
                        </pre>
                    </Fit>
                </ColumnStack>
            </Modal.Body>
            <Modal.Footer>
                <Button onClick={props.onClose}>Close</Button>
            </Modal.Footer>
        </Modal>
    );
}

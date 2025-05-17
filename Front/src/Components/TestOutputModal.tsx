import * as React from "react";
import { Button, Modal, Toast } from "@skbkontur/react-ui";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { CopyIcon16Light } from "@skbkontur/icons";
import { runAsyncAction } from "../Utils/TypeHelpers";
import styles from "./TestOutputModal.module.css";

interface TestOutputModalProps {
    jobId: string;
    jobRunIds: string[];
    testId: string;
    onClose: () => void;
}

export function TestOutputModal(props: TestOutputModalProps): React.JSX.Element {
    const [failedOutput, failedMessage, systemOutput] = useStorageQuery(
        x => x.getFailedTestOutput(props.jobId, props.testId, props.jobRunIds),
        [props.jobId, props.testId, props.jobRunIds]
    );

    const handleCopyToClipboard = React.useCallback(() => {
        runAsyncAction(async () => {
            const textToCopy = [props.testId, "---", failedMessage, "---", failedOutput, "---", systemOutput].join(
                "\n"
            );
            await navigator.clipboard.writeText(textToCopy);

            // eslint-disable-next-line @typescript-eslint/no-deprecated
            Toast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, props.testId]);

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
                            {failedMessage}
                            ---
                            {failedOutput}
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

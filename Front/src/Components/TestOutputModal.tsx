import * as React from "react";
import { Button, Modal, Toast } from "@skbkontur/react-ui";
import styled from "styled-components";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { theme } from "../Theme/ITheme";
import { ColumnStack, Fill, Fit, RowStack } from "@skbkontur/react-stack-layout";
import { CopyIcon16Light } from "@skbkontur/icons";
import { runAsyncAction } from "../TypeHelpers";

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
            Toast.push("Copied to clipboard");
        });
    }, [failedMessage, failedOutput, systemOutput, props.testId]);

    return (
        <Modal onClose={props.onClose} width="1000px">
            <Modal.Header>Test Output</Modal.Header>
            <Modal.Body>
                <ColumnStack block gap={2} stretch>
                    <Fit>
                        <TestId>{props.testId}</TestId>
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
                        <Code>
                            {failedMessage}
                            ---
                            {failedOutput}
                            ---
                            {systemOutput}
                        </Code>
                    </Fit>
                </ColumnStack>
            </Modal.Body>
            <Modal.Footer>
                <Button onClick={props.onClose}>Close</Button>
            </Modal.Footer>
        </Modal>
    );
}

const TestId = styled.h3`
    font-size: 20px;
    line-height: 30px;
`;

const Code = styled.pre`
    font-size: 14px;
    line-height: 18px;
    padding: 15px;
    margin: 0 -15px;
    border: 1px solid ${theme.borderLineColor2};
    overflow: scroll;
    max-height: 500px;
`;

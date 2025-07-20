import * as React from "react";

import { useMemo } from "react";
import styles from "./TestName.module.css";
import { NativeLinkButton } from "./NativeLinkButton";
import { FlakyTestBadge } from "./FlakyTestBadge";

interface TestNameProps {
    value: string;
    onTestNameClick: (() => void) | undefined;
    onSetSearchValue: (value: string) => void;
    isFlaky?: boolean;
}

export function TestName(props: TestNameProps): React.JSX.Element {
    const splitValue = useMemo(() => splitTestName(props.value), [props.value]);
    return (
        <>
            {props.onTestNameClick ? (
                <NativeLinkButton
                    onClick={() => {
                        props.onTestNameClick?.();
                    }}>
                    {splitValue[1]}
                </NativeLinkButton>
            ) : (
                splitValue[1]
            )}
            {props.isFlaky && (
                <>
                    {" "}
                    <FlakyTestBadge />
                </>
            )}
            <div className={styles.testNamePrefix}>{splitValue[0]}</div>
        </>
    );
}

export function splitTestName(testName: string): [string, string] {
    const parts = testName.split("(");
    if (parts.length > 1) {
        const lastPart = parts.slice(1);
        const dotParts = (parts[0] ?? "").split(/[.]/);
        const prefix = dotParts.slice(0, -2).join(".");
        const testcaseName = dotParts.slice(-2).join(".");
        return [prefix, `${testcaseName}(${lastPart.join("(")}`];
    }

    const dotParts = testName.split(/[.]/);
    const prefix = dotParts.slice(0, -2).join(".");
    const testcaseName = dotParts.slice(-2).join(".");
    return [prefix, testcaseName];
}

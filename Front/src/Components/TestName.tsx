import * as React from "react";

import { useMemo } from "react";
import styled from "styled-components";
import { theme } from "../Theme/ITheme";
import { NativeLinkButton } from "./NativeLinkButton";

interface TestNameProps {
    value: string;
    onTestNameClick: (() => void) | undefined;
    onSetSearchValue: (value: string) => void;
}

export function TestName(props: TestNameProps): React.JSX.Element {
    const splitValue = useMemo(() => {
        const parts = props.value.split("(");
        if (parts.length > 1) {
            const lastPart = parts.slice(1);
            const dotParts = (parts[0] ?? "").split(/[.]/);
            const prefix = dotParts.slice(0, -2).join(".");
            const testcaseName = dotParts.slice(-2).join(".");
            return [prefix, `${testcaseName}(${lastPart.join("(")}`];
        }

        const dotParts = props.value.split(/[.]/);
        const prefix = dotParts.slice(0, -2).join(".");
        const testcaseName = dotParts.slice(-2).join(".");
        return [prefix, testcaseName];
    }, [props.value]);
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
            <TestNamePrefix>{splitValue[0]}</TestNamePrefix>
        </>
    );
}

const TestNamePrefix = styled.div`
    font-size: ${props => props.theme.smallTextSize};
    color: ${props => props.theme.mutedTextColor};
`;

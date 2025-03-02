import { CheckAIcon24Regular, XIcon24Regular, ShapeCircleIcon24Solid } from "@skbkontur/icons";
import { Button } from "@skbkontur/react-ui";
import * as React from "react";

interface TestTypeFilterButtonProps {
    count: string | number;
    type: undefined | "Success" | "Failed" | "Skipped";
    currentType: undefined | "Success" | "Failed" | "Skipped";
    onClick: (value: undefined | "Success" | "Failed" | "Skipped") => void;
}
export function TestTypeFilterButton({
    count,
    type,
    currentType,
    onClick,
    ...props
}: TestTypeFilterButtonProps): React.JSX.Element {
    if (count != "0") {
        return (
            <Button
                title={`${count.toString()} ${(type ?? "All").toLowerCase()} tests`}
                use={currentType === type ? "primary" : "backless"}
                icon={
                    type === "Success" ? (
                        <CheckAIcon24Regular />
                    ) : type === "Failed" ? (
                        <XIcon24Regular />
                    ) : type === "Skipped" ? (
                        <ShapeCircleIcon24Solid />
                    ) : (
                        <></>
                    )
                }
                onClick={() => {
                    onClick(type);
                }}
                {...props}>
                {type == undefined ? "All " : ""}
                {count}
            </Button>
        );
    }
    return <></>;
}

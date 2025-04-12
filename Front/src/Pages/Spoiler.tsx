import { ArrowCDownIcon, ArrowCRightIcon } from "@skbkontur/icons";
import { RowStack, Fit } from "@skbkontur/react-stack-layout";
import * as React from "react";
import styled from "styled-components";

interface SpoilerProps {
    iconSize: number;
    title: React.ReactNode;
    children: React.ReactNode;
    openedByDefault?: boolean;
}

export function Spoiler(props: SpoilerProps) {
    const [open, setOpen] = React.useState(props.openedByDefault || false);

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const a: any = {
        onClick: () => {
            setOpen(!open);
        },
    };
    return (
        <>
            <RowStack gap={2} block {...a} style={{ cursor: "pointer" }}>
                <Fit>{open ? <ArrowCDownIcon size={props.iconSize} /> : <ArrowCRightIcon size={props.iconSize} />}</Fit>
                <Fit>{props.title}</Fit>
            </RowStack>
            {open && <SpoilerContent style={{ paddingLeft: props.iconSize + 2 * 5 }}>{props.children}</SpoilerContent>}
        </>
    );
}

const SpoilerContent = styled.div`
    padding: 10px 10px 10px 20px;
`;

import * as React from "react";
import { ComboBox, MenuSeparator } from "@skbkontur/react-ui";
import { MediaUiAPlayIcon } from "@skbkontur/icons";

interface ProjectComboBoxProps {
    value: string | undefined;
    items: string[];
    handler: (item: string | undefined) => void;
}

export const ProjectComboBox = ({ value, items, handler }: ProjectComboBoxProps) => (
    <ComboBox<string | undefined>
        value={value}
        getItems={() => Promise.resolve([undefined, <MenuSeparator />, ...items])}
        onValueChange={x => {
            handler(x);
        }}
        itemToValue={x => (typeof x === "string" ? x : "")}
        valueToString={x => (typeof x === "string" ? x : "")}
        placeholder={"All jobs"}
        renderValue={x =>
            x == undefined ? (
                <span>
                    {" "}
                    <MediaUiAPlayIcon /> All jobs
                </span>
            ) : (
                <span>
                    {" "}
                    <MediaUiAPlayIcon /> {x}
                </span>
            )
        }
        renderItem={x =>
            x == undefined ? (
                <span>
                    {" "}
                    <MediaUiAPlayIcon /> All jobs
                </span>
            ) : (
                <span>
                    {" "}
                    <MediaUiAPlayIcon /> {x}
                </span>
            )
        }
    />
);

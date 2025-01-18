import * as React from "react";
import { ComboBox, MenuSeparator } from "@skbkontur/react-ui";
import { MediaUiAPlayIcon } from "@skbkontur/icons";

export const JobComboBox = ({
    value,
    items,
    handler,
}: {
    value: string | undefined;
    items: string[];
    handler: (item: string | undefined) => void;
}) => (
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

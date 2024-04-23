import * as React from "react";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { ComboBox, MenuSeparator } from "@skbkontur/react-ui";
import { ShareNetworkIcon } from "@skbkontur/icons";

interface BranchSelectProps {
    branch: undefined | string;
    onChangeBranch: (nextValue: undefined | string) => void;
    branchQuery?: string;
    branchNames?: string[];
}

export function BranchSelect(props: BranchSelectProps): React.JSX.Element {
    const client = useClickhouseClient();
    const [queriedBranches] = client.useData<[string]>(props.branchQuery ?? `SELECT 1 WHERE false`, []);

    const getItems = async (query: string) => {
        const branchesToFilter: string[] = [...(queriedBranches ?? []).map(x => x[0]), ...(props.branchNames ?? [])];
        const filteredBranches = branchesToFilter.filter(x => !query || x.includes(query));
        return [undefined, <MenuSeparator />, ...filteredBranches];
    };

    return (
        <ComboBox
            value={props.branch}
            getItems={getItems}
            onValueChange={x => {
                if (typeof x === "string" || x == undefined) props.onChangeBranch(x);
            }}
            itemToValue={x => (typeof x === "string" ? x : "")}
            valueToString={x => (typeof x === "string" ? x : "")}
            placeholder={"All branches"}
            renderValue={x =>
                x == undefined ? (
                    <span>
                        {" "}
                        <ShareNetworkIcon /> All branches
                    </span>
                ) : (
                    <span>
                        {" "}
                        <ShareNetworkIcon /> {x}
                    </span>
                )
            }
            renderItem={x =>
                x == undefined ? (
                    <span>
                        {" "}
                        <ShareNetworkIcon /> All branches
                    </span>
                ) : (
                    <span>
                        {" "}
                        <ShareNetworkIcon /> {x}
                    </span>
                )
            }
        />
    );
}

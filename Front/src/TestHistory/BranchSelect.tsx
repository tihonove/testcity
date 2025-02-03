import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { ComboBox, MenuSeparator } from "@skbkontur/react-ui";
import { ShareNetworkIcon } from "@skbkontur/icons";
import * as React from "react";

interface BranchSelectProps {
    branch: undefined | string;
    onChangeBranch: (nextValue: undefined | string) => void;
    projectIds?: string[];
    jobId?: string;
    branchNames?: string[];
}

export function BranchSelect(props: BranchSelectProps): React.JSX.Element {
    const client = useClickhouseClient();
    const [queriedBranches] = client.useData<[string]>(
        `
        SELECT DISTINCT 
            BranchName
        FROM JobInfo
        WHERE 
            StartDateTime >= DATE_ADD(MONTH, -1, NOW()) AND BranchName != '' 
            ${props.projectIds ? `AND ProjectId IN [${props.projectIds.map(x => "'" + x + "'").join(", ")}]` : ""}
            ${props.jobId ? `AND JobId = '${props.jobId}'` : ""}
        ORDER BY StartDateTime DESC;`,
        [props.projectIds, props.jobId]
    );

    const getItems = (query: string) => {
        const branchesToFilter: string[] = [...queriedBranches.map(x => x[0]), ...(props.branchNames ?? [])];
        const filteredBranches = branchesToFilter.filter(x => !query || x.includes(query));
        return Promise.resolve([undefined, <MenuSeparator />, ...filteredBranches]);
    };

    return (
        <ComboBox<undefined | string>
            value={props.branch}
            getItems={getItems}
            onValueChange={x => {
                props.onChangeBranch(x);
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

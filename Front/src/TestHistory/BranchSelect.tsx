import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
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

const TOP_BRANCHES = ["main", "master", "release"];

function createMoveToTopSorter<T>(topItems: T[]) {
    return (a: T, b: T) => {
        const aIsSpecial = topItems.includes(a);
        const bIsSpecial = topItems.includes(b);
        if (aIsSpecial && !bIsSpecial) return -1;
        if (!aIsSpecial && bIsSpecial) return 1;
        return 0;
    };
}

export function BranchSelect(props: BranchSelectProps): React.JSX.Element {
    const queriedBranches = useStorageQuery(
        storage => storage.findBranches(props.projectIds, props.jobId),
        [props.projectIds, props.jobId]
    );

    const sortedBranches = React.useMemo(() => {
        return [...queriedBranches, ...(props.branchNames ?? [])].sort(createMoveToTopSorter(TOP_BRANCHES));
    }, [queriedBranches, props.branchNames]);

    const getItems = (query: string) => {
        const filteredBranches = sortedBranches.filter(x => !query || x.includes(query));
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

import { ShareNetworkIcon, TimeClockMoveBackIcon16Light } from "@skbkontur/icons";
import { ComboBox, MenuSeparator } from "@skbkontur/react-ui";
import * as React from "react";
import { useStorageQuery } from "../ClickhouseClientHooksWrapper";
import { usePopularBranches } from "../Utils/PopularBranchStoring";
import styles from "./BranchSelect.module.css";

interface BranchSelectProps {
    branch: undefined | string;
    onChangeBranch: (nextValue: undefined | string) => void;
    projectIds?: string[];
    jobId?: string;
    branchNames?: string[];
}

const TOP_BRANCHES = ["main", "master", "release"];
export const DEFAULT_BRANCHE_NAMES = ["main", "master"];

export function BranchSelect({
    branch,
    projectIds,
    jobId,
    branchNames,
    onChangeBranch,
}: BranchSelectProps): React.JSX.Element {
    const queriedBranches = useStorageQuery(storage => storage.findBranches(projectIds, jobId), [projectIds, jobId]);

    const sortedBranches = React.useMemo(() => {
        return [...queriedBranches, ...(branchNames ?? [])].sort(createMoveToTopSorter(TOP_BRANCHES));
    }, [queriedBranches, branchNames]);

    const popularBranches = usePopularBranches()
        .filter(x => !TOP_BRANCHES.includes(x))
        .filter(x => queriedBranches.includes(x));
    const popularBranchesSet = React.useMemo(() => new Set(popularBranches), [popularBranches]);

    const filterBranches = React.useCallback(
        (all: string[], query: string) => all.filter(x => !query || x.includes(query)),
        []
    );

    const getItems = React.useCallback(
        (query: string) => {
            const popular = filterBranches(popularBranches, query);
            const others = filterBranches(sortedBranches, query).filter(x => !popularBranchesSet.has(x));
            return Promise.resolve([
                undefined,
                <MenuSeparator key="sep1" />,
                ...(popular.length > 0 ? [...popular, <MenuSeparator key="sep2" />] : []),
                ...others,
            ]);
        },
        [popularBranches, sortedBranches, popularBranchesSet, filterBranches]
    );

    return (
        <ComboBox<undefined | string>
            value={branch}
            getItems={getItems}
            onValueChange={onChangeBranch}
            itemToValue={x => (typeof x === "string" ? x : "")}
            valueToString={x => (typeof x === "string" ? x : "")}
            placeholder={"All branches"}
            renderValue={x =>
                x == null ? (
                    <span>
                        <ShareNetworkIcon /> All branches
                    </span>
                ) : (
                    <span>
                        <ShareNetworkIcon /> {x}
                    </span>
                )
            }
            renderItem={x =>
                x == null ? (
                    <span>
                        <ShareNetworkIcon /> All branches
                    </span>
                ) : (
                    <div className={styles.iconWrapper}>
                        <div className={styles.branchName}>
                            <ShareNetworkIcon /> {x}
                        </div>
                        {popularBranches.includes(x) && <TimeClockMoveBackIcon16Light />}
                    </div>
                )
            }
        />
    );
}

function createMoveToTopSorter<T>(topItems: T[]) {
    return (a: T, b: T) => {
        const aIsSpecial = topItems.includes(a);
        const bIsSpecial = topItems.includes(b);
        if (aIsSpecial && !bIsSpecial) return -1;
        if (!aIsSpecial && bIsSpecial) return 1;
        return 0;
    };
}

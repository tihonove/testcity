import { useEffect, useState } from "react";
import { useLocalStorage, useReadLocalStorage } from "usehooks-ts";

const POPULAR_BRANCHES_KEY = "popularBranches";
const MAX_POPULAR_BRANCHES = 5;

export function usePopularBranchStoring(branch: string | undefined): void {
    const [branches, setBranches] = useLocalStorage<string[]>(POPULAR_BRANCHES_KEY, []);
    useEffect(() => {
        if (!branch) return;

        const updatedBranches = [branch, ...branches.filter(b => b !== branch)];

        if (updatedBranches.length > MAX_POPULAR_BRANCHES) {
            updatedBranches.pop();
        }

        setBranches(updatedBranches);
    }, [branch]);
}

export function usePopularBranches(): string[] {
    return useReadLocalStorage<string[]>(POPULAR_BRANCHES_KEY) ?? [];
}

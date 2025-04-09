import { useReadLocalStorage } from "usehooks-ts";

export function useShowChangesFeature(): boolean {
    return useReadLocalStorage("changes") ?? false;
}

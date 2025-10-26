import usePromise from "react-promise-suspense";
import { apiUrlPrefix, useApiUrl } from "./Domain/Navigation";
import { Group, GroupNode } from "./Domain/Storage/Projects/GroupNode";

export function useTestCityClient(): TestCityApiClient {
    const apiUrl = useApiUrl();
    return new TestCityApiClient(apiUrl);
}

export function useTestCityApi<T>(fn: (client: TestCityApiClient) => Promise<T>, deps?: React.DependencyList): T {
    const client = useTestCityClient();
    return usePromise(async () => {
        return await fn(client);
    }, [fn.toString(), ...(deps ?? [])]);
}

export class TestCityApiClient {
    private readonly apiUrl: string;

    public constructor(apiUrl: string) {
        this.apiUrl = apiUrl;
    }

    public async getRootGroups(): Promise<Group[]> {
        const response = await fetch(`${this.apiUrl}groups`);
        if (!response.ok) {
            throw new Error("Unable to load root groups");
        }
        return (await response.json()) as Group[];
    }

    public async getRootGroup(groupIdOrTitle: string): Promise<GroupNode> {
        const response = await fetch(`${this.apiUrl}groups/${encodeURIComponent(groupIdOrTitle)}`);
        if (!response.ok) {
            throw new Error(`Unable to find group ${groupIdOrTitle}`);
        }
        return (await response.json()) as GroupNode;
    }
}

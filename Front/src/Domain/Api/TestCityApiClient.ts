import usePromise from "react-promise-suspense";
import { apiUrlPrefix, useApiUrl } from "../Navigation";
import { Group, GroupNode } from "../Storage/Projects/GroupNode";
import { TestCityRunsApiClient } from "./TestCityRunsApiClient";
import { UserInfoDto } from "./DTO/UserInfoDto";

export function useTestCityClient(): TestCityApiClient {
    const apiUrl = useApiUrl();
    return new TestCityApiClient(apiUrl);
}

export function useTestCityRequest<T>(fn: (client: TestCityApiClient) => Promise<T>, deps?: React.DependencyList): T {
    const client = useTestCityClient();
    return usePromise(async () => {
        return await fn(client);
    }, [fn.toString(), ...(deps ?? [])]);
}

export class TestCityApiClient {
    private readonly apiUrl: string;
    public readonly runs: TestCityRunsApiClient;

    public constructor(apiUrl: string) {
        this.apiUrl = apiUrl;
        this.runs = new TestCityRunsApiClient(apiUrl);
    }

    public async getUser(): Promise<UserInfoDto | null> {
        return (await (await fetch(`${this.apiUrl}auth/user`, { redirect: "manual" })).json()) as UserInfoDto | null;
    }
}

import * as React from "react";
import { WebClickHouseClient } from "@clickhouse/client-web/dist/client";
import { createClient } from "@clickhouse/client-web";
import fetchIntercept from "fetch-intercept";
import usePromise from "react-promise-suspense";
import { TestAnalyticsStorage } from "./Domain/Storage/Storage";
import { uuidv4 } from "./Utils/Guids";
import { apiUrlPrefix } from "./Domain/Navigation";

const unregister = fetchIntercept.register({
    request: function (url, config) {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
        return [url.replace("http://zzz", `${apiUrlPrefix}clickhouse`), config];
    },
});

const client = createClient({
    host: "http://zzz",
    database: "DATABASE",
    username: "USERNAME",
    password: "PASSWORD",
});

export function useStorageQuery<T>(
    fn: (storage: TestAnalyticsStorage) => T | Promise<T>,
    deps: React.DependencyList
): T {
    const storage = React.useRef(new TestAnalyticsStorage(client));
    return usePromise(async () => {
        return await fn(storage.current);
    }, [fn.toString(), ...deps]);
}

export function useClickhouseClient(): ClickhouseClientHooksWrapper {
    return new ClickhouseClientHooksWrapper(client);
}

export function useStorage(): TestAnalyticsStorage {
    return new TestAnalyticsStorage(client);
}

function getQueryId() {
    return uuidv4();
}

class ClickhouseClientHooksWrapper {
    private client: WebClickHouseClient;

    public constructor(client: WebClickHouseClient) {
        this.client = client;
    }

    public useData2<T>(query: string, deps?: React.DependencyList): T[] {
        const inputs = [query, ...(deps ?? [])];

        return usePromise(async () => {
            const id = getQueryId();
            const response = await client.query({ query: query, format: "JSONCompact", query_id: id });
            const result = await response.json();
            if (typeof result === "object") {
                return result["data"] as T[];
            } else {
                throw new Error("Invalid output");
            }
        }, inputs);
    }

    public async query<T>(query: string): Promise<T[]> {
        const response = await client.query({ query: query, format: "JSONCompact", query_id: getQueryId() });
        const result = await response.json();
        if (typeof result === "object") {
            return result["data"] as T[];
        } else {
            throw new Error("Invalid output");
        }
    }
}

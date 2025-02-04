import * as React from "react";
import { WebClickHouseClient } from "@clickhouse/client-web/dist/client";
import { createClient } from "@clickhouse/client-web";
import fetchIntercept from "fetch-intercept";
import usePromise from "react-promise-suspense";
import { TestAnalyticsStorage } from "./Domain/Storage";
import { uuidv4 } from "./Domain/Guids";

const unregister = fetchIntercept.register({
    request: function (url, config) {
        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
        return [url.replace("http://zzz", "/test-analytics/clickhouse"), config];
    },
});

const client = createClient({
    host: "http://zzz",
    database: "test_analytics",
    username: "tihonove",
    password: "12487562",
    clickhouse_settings: {
        add_http_cors_header: 1,
    },
});

export function useStorageQuery<T>(
    fn: (storage: TestAnalyticsStorage) => T | Promise<T>,
    deps?: React.DependencyList
): T {
    const storage = React.useRef(new TestAnalyticsStorage(client));
    /* eslint-disable @typescript-eslint/no-unsafe-return */
    return usePromise(async () => {
        return await fn(storage.current);
        // eslint-disable-next-line @typescript-eslint/ban-ts-comment
        // @ts-ignore
    }, [fn.toString(), ...deps]);
    /* eslint-enable @typescript-eslint/no-unsafe-return */
}

export function useClickhouseClient(): ClickhouseClientHooksWrapper {
    return new ClickhouseClientHooksWrapper(client);
}

function getQueryId() {
    return uuidv4();
}

class ClickhouseClientHooksWrapper {
    private client: WebClickHouseClient;

    public constructor(client: WebClickHouseClient) {
        this.client = client;
    }

    public useData<T>(query: string, deps?: React.DependencyList): [data: T[], loading: boolean] {
        const [data, setData] = React.useState<T[]>([]);
        const [loading, setLoading] = React.useState(true);
        React.useEffect(() => {
            setLoading(true);
            // eslint-disable-next-line @typescript-eslint/no-floating-promises
            (async () => {
                const response = await client.query({ query: query, format: "JSONCompact", query_id: getQueryId() });
                const result = await response.json();
                if (typeof result === "object") {
                    // eslint-disable-next-line @typescript-eslint/ban-ts-comment
                    // @ts-ignore
                    setData(result["data"]);
                    setLoading(false);
                } else {
                    throw new Error("Invalid output");
                }
            })();
        }, deps);

        return [data, loading];
    }

    public useData2<T>(query: string, deps?: React.DependencyList): T[] {
        const inputs = [query, ...(deps ?? [])];
        // eslint-disable-next-line @typescript-eslint/no-unsafe-return
        return usePromise(async () => {
            const id = getQueryId();
            const response = await client.query({ query: query, format: "JSONCompact", query_id: id });
            const result = await response.json();
            if (typeof result === "object") {
                return result["data"];
            } else {
                throw new Error("Invalid output");
            }
        }, inputs);
    }

    public async query<T>(query: string): Promise<T[]> {
        const response = await client.query({ query: query, format: "JSONCompact", query_id: getQueryId() });
        const result = await response.json();
        if (typeof result === "object") {
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore
            return result["data"];
        } else {
            throw new Error("Invalid output");
        }
    }
}

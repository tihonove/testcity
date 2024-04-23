import * as React from "react";
import { WebClickHouseClient } from "@clickhouse/client-web/dist/client";
const { createClient } = require("@clickhouse/client-web");
import fetchIntercept from "fetch-intercept";
import usePromise from "react-promise-suspense";

// const client = createClient({
//     host: "http://localhost:8080",
//     database: "default",
//     clickhouse_settings: {},
// })

// eslint-disable-next-line @typescript-eslint/no-unused-vars
const unregister = fetchIntercept.register({
    request: function (url, config) {
        return [url.replace("http://zzz", "/test-analytics/clickhouse"), config];
    },
});

const client = createClient({
    host: "http://zzz",
    database: "test_analytics",
    username: "tihonove",
    password: "12487562",
    clickhouse_settings: {
        add_http_cors_header: "true",
    },
});

export function useClickhouseClient(): ClickhouseClientHooksWrapper {
    return new ClickhouseClientHooksWrapper(client);
}

function getQueryId() {
    return uuidv4();
}

function uuidv4() {
    if (typeof crypto != "undefined") {
        return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
            (+c ^ (crypto.getRandomValues(new Uint8Array(1))[0] & (15 >> (+c / 4)))).toString(16)
        );
    } else {
        const w = () => {
            return Math.floor((1 + Math.random()) * 0x10000)
                .toString(16)
                .substring(1);
        };
        return `${w()}${w()}-${w()}-${w()}-${w()}-${w()}${w()}${w()}`;
    }
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
            (async () => {
                const response = await client.query({ query: query, format: "JSONCompact", query_id: getQueryId() });
                const result = await response.json();
                if (typeof result === "object") {
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
        const inputs = [...(deps ?? [])];
        return usePromise(async () => {
            const response = await client.query({ query: query, format: "JSONCompact", query_id: getQueryId() });
            const result = await response.json();
            if (typeof result === "object") {
                return result["data"];
            } else {
                throw new Error("Invalid output");
            }
        }, inputs);
    }
}

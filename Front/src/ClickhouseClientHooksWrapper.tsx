import * as React from "react";
import {WebClickHouseClient} from "@clickhouse/client-web/dist/client";
const {createClient} = require('@clickhouse/client-web');
import fetchIntercept from 'fetch-intercept';

// const client = createClient({
//     host: "http://localhost:8080",
//     database: "default",
//     clickhouse_settings: {},
// })


const unregister = fetchIntercept.register({
    request: function (url, config) {
        return [url.replace(/singular--TestAnalytics.nginx-clickhouse-proxy/i, "singular/TestAnalytics.nginx-clickhouse-proxy/"), config];
    },
});

const client = createClient({
    host: "http://singular--TestAnalytics.nginx-clickhouse-proxy",
    database: "test_analytics",
    username: "tihonove",
    password: "12487562",
    clickhouse_settings: {
        add_http_cors_header: "true",
    },
})

export function useClickhouseClient(): ClickhouseClientHooksWrapper {
    return new ClickhouseClientHooksWrapper(client);
}

let value = 0;
function getQueryId() {
    return (value++).toString();
}

class ClickhouseClientHooksWrapper {
    private client: WebClickHouseClient;

    public constructor(client: WebClickHouseClient) {
        this.client = client;
    }

    public useData<T>(query: string, deps?: React.DependencyList): [data: T[], loading: boolean] {
        const [data, setData] = React.useState();
        const [loading, setLoading] = React.useState(true);
        React.useEffect(() => {
            setLoading(true);
            (async () => {
                const response = await client.query({query: query, format: "JSONCompact", query_id: getQueryId() });
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
}

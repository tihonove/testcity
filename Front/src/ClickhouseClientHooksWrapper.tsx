import * as React from "react";
import {WebClickHouseClient} from "@clickhouse/client-web/dist/client";
const {createClient} = require('@clickhouse/client-web');

const client = createClient({
    host: "http://localhost:8080",
    database: "default",
    clickhouse_settings: {},
})

export function useClickhouseClient(): ClickhouseClientHooksWrapper {
    return new ClickhouseClientHooksWrapper(client);
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
                const response = await client.query({query: query, format: "JSONCompact"});
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

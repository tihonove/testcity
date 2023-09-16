import * as React from "react";
import {useSearchParams} from "react-router-dom";
import {WebClickHouseClient} from "@clickhouse/client-web/dist/client";
import {useEffect, useState} from "react";

const {createClient} = require('@clickhouse/client-web');

const client = createClient({
    host: "http://localhost:8080",
    database: "default",
    clickhouse_settings: {},
})

async function queryData(client: WebClickHouseClient, query: string): Promise<string[][]> {
    const response = await client.query({query: query, format: "JSONCompact"});
    const result = await response.json();
    if (typeof result === "object")
        return result["data"];
    throw new Error("Invalid output");
}

export function TestHistoryPage(): React.JSX.Element {
    let [searchParams, setSearchParams] = useSearchParams();
    const testId = searchParams.get("id");
    if (testId == null)
        return <div>Test id not specpifed</div>;

    const [testRuns, setTestRuns] = useState<string[][]>();

    useEffect(() => {
        (async () => {
            setTestRuns(await queryData(client, `
                SELECT TOP 5
                    JobId, JobRunId, BranchName, State, Duration, StartDateTime, AgentName, AgentOSName
                FROM TestRuns 
                WHERE 
                    TestId = '${testId}'
                ORDER BY 
                    StartDateTime DESC        
            `));
        })();

    }, [testId]);

    return <div>
        {testId}
        {testRuns?.map(run => <div>
            {JSON.stringify(run)}
        </div>)}
    </div>
}


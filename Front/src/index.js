const { createClient } = require('@clickhouse/client-web');
const connection_1 = require("@clickhouse/client-web/dist/connection");


async function doWork() {
    const client = createClient({
        host: "http://localhost:8080",
        database: "default",
        clickhouse_settings: {
        },
    })
    var response =  await client.query({ query: "select TestId from TestRuns", format: "JSONCompact"  })
    var result = await response.json();
    console.log(result);
}

console.log("Hi2!")
doWork();


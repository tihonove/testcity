import * as React from "react";
import { useState } from "react";
import { useClickhouseClient } from "../ClickhouseClientHooksWrapper";
import { RunStatus, TestHistory } from "../TestHistory/TestHistory";
import { useSearchParamAsState } from "../Utils";

export function TestHistoryPage(): React.JSX.Element {
    const [testId] = useSearchParamAsState("id");
    const [currentJobId, setCurrentJobId] = useSearchParamAsState("job");
    const [runId, _] = useSearchParamAsState("runId");
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");

    if (testId == null) return <div>Test id not specified</div>;

    const client = useClickhouseClient();
    const jobIds = client.useData2<[string]>(`SELECT DISTINCT JobId FROM TestRuns WHERE TestId = '${testId}'`, [
        testId,
    ]);
    const branches = client.useData2<[string]>(`SELECT DISTINCT BranchName FROM TestRuns WHERE TestId = '${testId}'`, [
        testId,
        currentJobId,
    ]);

    const condition = React.useMemo(() => {
        let result = `TestId = '${testId}'`;
        if (currentJobId != undefined) result += ` AND JobId = '${currentJobId}'`;
        if (currentBranchName != undefined) result += ` AND BranchName = '${currentBranchName}'`;
        return result;
    }, [testId, currentJobId, currentBranchName]);

    const stats = client.useData2<[string, number, string]>(
        `SELECT TOP 1000 State, Duration, StartDateTime FROM TestRuns WHERE ${condition} ORDER BY StartDateTime DESC;`,
        [condition]
    );

    const [testRunsPage, setTestRunsPage] = useState(1);

    const getTestRuns = React.useCallback(
        () =>
            client.useData2<[string, string, string, RunStatus, number, string, string, string, string]>(
                `SELECT JobId, JobRunId, BranchName, State, Duration, StartDateTime, JobUrl
                FROM TestRuns 
                WHERE ${condition} 
                ORDER BY StartDateTime 
                DESC 
                LIMIT ${(50 * (testRunsPage - 1)).toString()}, 50`,
                [testId, testRunsPage, condition]
            ),
        [testId, testRunsPage, condition]
    );

    const getTotalRunCount = React.useCallback(
        () => client.useData2<[string]>(`SELECT COUNT(*) FROM TestRuns WHERE ${condition}`, [condition]),
        [condition]
    );

    const statusMessages = client.useData2<[string, string]>(
        `SELECT JobRunId, CustomStatusMessage 
        FROM JobInfo 
        WHERE ${getTestRuns().map(t => `JobRunId = '${t[1]}'`).join(' OR ')};`,
        [testId, testRunsPage, condition]
    );

    return (
        <div>
            <TestHistory
                testId={testId}
                jobIds={jobIds.map(x => x[0])}
                jobId={currentJobId}
                onChangeJobId={setCurrentJobId}
                branchNames={branches.map(x => x[0])}
                branch={currentBranchName}
                onChangeBranch={setCurrentBranchName}
                stats={stats}
                runsFetcher={getTestRuns}
                statusMessages={statusMessages}
                totalRunCount={Number(getTotalRunCount()[0][0])}
                runsPage={testRunsPage}
                onRunsPageChange={setTestRunsPage}
                runIdBreadcrumb={runId}
            />
        </div>
    );
}

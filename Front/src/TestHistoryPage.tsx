import * as React from "react";
import {useState} from "react";
import {useClickhouseClient} from "./ClickhouseClientHooksWrapper";
import {TestHistory} from "./TestHistory/TestHistory";
import {useSearchParamAsState} from "./Utils";

export function TestHistoryPage(): React.JSX.Element {
    const [testId] = useSearchParamAsState("id");
    const [currentJobId, setCurrentJobId] = useSearchParamAsState("job");
    const [currentBranchName, setCurrentBranchName] = useSearchParamAsState("branch");
    if (testId == null)
        return <div>Test id not specified</div>;

    const client = useClickhouseClient();
    const [jobIds, jobLoading] = client.useData<[string]>(`SELECT DISTINCT JobId FROM TestRuns WHERE TestId = '${testId}'`, [testId]);
    const [branches, branchesLoading] = client.useData<[string]>(`SELECT DISTINCT BranchName FROM TestRuns WHERE TestId = '${testId}'`, [testId, currentJobId]);

    const condition = React.useMemo(() => {
        let result = `TestId = '${testId}'`;
        if (currentJobId != undefined)
            result += ` AND JobId = '${currentJobId}'`;
        if (currentBranchName != undefined)
            result += ` AND BranchName = '${currentBranchName}'`;
        return result;
    }, [testId, currentJobId, currentBranchName]);

    const [stats, statsLoading] = client.useData<[string, number, string]>(`SELECT TOP 1000 State, Duration, StartDateTime FROM TestRuns WHERE ${condition} ORDER BY StartDateTime DESC`, [condition]);
    const [testRunsPage, setTestRunsPage] = useState(1);
    const [testRuns, testRunsLoading] = client.useData(`SELECT JobId, JobRunId, BranchName, State, Duration, StartDateTime, AgentName, AgentOSName FROM TestRuns WHERE ${condition} ORDER BY StartDateTime DESC LIMIT ${50 * (testRunsPage - 1)}, 50`, [testId, testRunsPage]);
    const [totalRunCount, totalRunCountLoading] = client.useData<[string]>(`SELECT COUNT(*) FROM TestRuns WHERE ${condition}`, [condition]);
    if (jobLoading || branchesLoading || statsLoading || totalRunCountLoading || testRuns == undefined)
        return <div>Loading</div>;

    return <div>
        <TestHistory
            testId={testId}
            jobIds={jobIds.map(x => x[0])}
            jobId={currentJobId}
            onChangeJobId={setCurrentJobId}

            branchNames={branches.map(x => x[0])}
            branch={currentBranchName}
            onChangeBranch={setCurrentBranchName}

            stats={stats}
            runs={testRuns}
            totalRunCount={Number(totalRunCount[0][0])}
            runsPage={testRunsPage}
            onRunsPageChange={setTestRunsPage}
        />
    </div>
}


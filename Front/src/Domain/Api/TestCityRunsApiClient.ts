import { RunStatus } from "../../Components/RunStatus";
import { DashboardNode, GroupDashboardNode, JobRun, JobInfo, TestRun } from "../ProjectDashboardNode";

export interface TestOutput {
    failureMessage: string | null;
    failureOutput: string | null;
    systemOutput: string | null;
}

export interface FlakyTest {
    projectId: string;
    jobId: string;
    testId: string;
    lastRunDate: string;
    runCount: number;
    failCount: number;
    flipCount: number;
    updatedAt: string;
    flipRate: number;
}

export interface TestStats {
    state: string;
    duration: number;
    startDateTime: string;
}

export interface TestPerJobRun {
    jobId: string;
    jobRunId: string;
    branchName: string;
    state: RunStatus;
    duration: number;
    startDateTime: string;
    jobUrl: string;
    customStatusMessage: string;
}

export interface TestListStats {
    totalTestsCount: number;
    successTestsCount: number;
    skippedTestsCount: number;
    failedTestsCount: number;
}

export interface EntityNode {
    id: string;
    title: string;
    avatarUrl: string | null;
    type: "group" | "project";
}

export interface GroupEntityShortInfoNode extends EntityNode {
    type: "group";
}

export interface GroupEntityNode extends GroupEntityShortInfoNode {
    groups: GroupEntityNode[];
    projects: ProjectEntityNode[];
}

export interface ProjectEntityNode extends EntityNode {
    type: "project";
}

export class TestCityRunsApiClient {
    private readonly apiUrl: string;

    public constructor(apiUrl: string) {
        this.apiUrl = apiUrl;
    }

    public async getRootGroupsV2(): Promise<GroupEntityShortInfoNode[]> {
        const response = await fetch(`${this.apiUrl}groups-v2`);
        if (!response.ok) {
            throw new Error("Unable to load root groups");
        }
        return (await response.json()) as GroupEntityShortInfoNode[];
    }

    public async getEntity(groupOrProjectPath: string[]): Promise<ProjectEntityNode | GroupEntityNode> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load entity for ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as ProjectEntityNode | GroupEntityNode;
    }

    public async findAllBranches(groupOrProjectPath: string[], jobId: string | null = null): Promise<string[]> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/branches${jobId ? `?jobId=${encodeURIComponent(jobId)}` : ""}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load branches for ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as string[];
    }

    public async getFlakyTestsCount(
        groupOrProjectPath: string[],
        jobId: string,
        flipRateThreshold: number = 0.1
    ): Promise<number> {
        const queryParams = new URLSearchParams({
            flipRateThreshold: flipRateThreshold.toString(),
        });
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/flaky-tests-count?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load flaky tests count for ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as number;
    }

    public async getFlakyTests(
        groupOrProjectPath: string[],
        jobId: string,
        flipRateThreshold: number = 0.1,
        page: number = 0
    ): Promise<FlakyTest[]> {
        const queryParams = new URLSearchParams({
            flipRateThreshold: flipRateThreshold.toString(),
            page: page.toString(),
        });
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/flaky-tests?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load flaky tests for ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as FlakyTest[];
    }

    public async getFlakyTestsNames(
        groupOrProjectPath: string[],
        jobId: string,
        flipRateThreshold: number = 0.1
    ): Promise<string[]> {
        const queryParams = new URLSearchParams({
            flipRateThreshold: flipRateThreshold.toString(),
        });
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/flaky-tests-names?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load flaky test names for ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as string[];
    }

    public async getTestStats(groupOrProjectPath: string[], testId: string, branchName?: string): Promise<TestStats[]> {
        const queryParams = new URLSearchParams();
        if (branchName) {
            queryParams.append("branchName", branchName);
        }
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/tests/${encodeURIComponent(testId)}/stats?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load test stats for ${groupOrProjectPath.join("/")} and test ${testId}`);
        }
        return (await response.json()) as TestStats[];
    }

    public async getTestRuns(
        groupOrProjectPath: string[],
        testId: string,
        branchName?: string,
        page: number = 0
    ): Promise<TestPerJobRun[]> {
        const queryParams = new URLSearchParams();
        if (branchName) {
            queryParams.append("branchName", branchName);
        }
        queryParams.append("page", page.toString());
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/tests/${encodeURIComponent(testId)}/runs?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load test runs for ${groupOrProjectPath.join("/")} and test ${testId}`);
        }
        return (await response.json()) as TestPerJobRun[];
    }

    public async getTestRunCount(groupOrProjectPath: string[], testId: string, branchName?: string): Promise<number> {
        const queryParams = new URLSearchParams();
        if (branchName) {
            queryParams.append("branchName", branchName);
        }
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/tests/${encodeURIComponent(testId)}/run-count?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load test run count for ${groupOrProjectPath.join("/")} and test ${testId}`);
        }
        return (await response.json()) as number;
    }

    public async getJobRuns(
        groupOrProjectPath: string[],
        jobId: string,
        branchName?: string,
        page: number = 0
    ): Promise<JobRun[]> {
        const queryParams = new URLSearchParams();
        if (branchName) {
            queryParams.append("branchName", branchName);
        }
        queryParams.append("page", page.toString());

        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load job runs for ${groupOrProjectPath.join("/")} and job ${jobId}`);
        }
        return (await response.json()) as JobRun[];
    }

    public async getJobRun(groupOrProjectPath: string[], jobId: string, jobRunId: string): Promise<JobInfo> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}`
        );
        if (!response.ok) {
            throw new Error(`Unable to load job run ${jobRunId} for ${groupOrProjectPath.join("/")} and job ${jobId}`);
        }
        return (await response.json()) as JobInfo;
    }

    public async getTestList(
        groupOrProjectPath: string[],
        jobId: string,
        jobRunId: string,
        options?: {
            sortField?: string;
            sortDirection?: string;
            testIdQuery?: string;
            testStateFilter?: string;
            itemsPerPage?: number;
            page?: number;
        }
    ): Promise<TestRun[]> {
        const queryParams = new URLSearchParams();
        if (options?.sortField) {
            queryParams.append("sortField", options.sortField);
        }
        if (options?.sortDirection) {
            queryParams.append("sortDirection", options.sortDirection);
        }
        if (options?.testIdQuery) {
            queryParams.append("testIdQuery", options.testIdQuery);
        }
        if (options?.testStateFilter) {
            queryParams.append("testStateFilter", options.testStateFilter);
        }
        if (options?.itemsPerPage !== undefined) {
            queryParams.append("itemsPerPage", options.itemsPerPage.toString());
        }
        if (options?.page !== undefined) {
            queryParams.append("page", options.page.toString());
        }

        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/tests?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(
                `Unable to load test list for ${groupOrProjectPath.join("/")} job ${jobId} run ${jobRunId}`
            );
        }
        return (await response.json()) as TestRun[];
    }

    public async getTestsStats(
        groupOrProjectPath: string[],
        jobId: string,
        jobRunId: string,
        options?: {
            testIdQuery?: string;
            testStateFilter?: string;
        }
    ): Promise<TestListStats> {
        const queryParams = new URLSearchParams();
        if (options?.testIdQuery) {
            queryParams.append("testIdQuery", options.testIdQuery);
        }
        if (options?.testStateFilter) {
            queryParams.append("testStateFilter", options.testStateFilter);
        }

        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/tests-stats?${queryParams}`
        );
        if (!response.ok) {
            throw new Error(
                `Unable to load test stats for ${groupOrProjectPath.join("/")} job ${jobId} run ${jobRunId}`
            );
        }
        return (await response.json()) as TestListStats;
    }

    public async getTestOutput(
        groupOrProjectPath: string[],
        jobId: string,
        jobRunId: string,
        testId: string
    ): Promise<TestOutput> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/tests/${encodeURIComponent(testId)}/output`
        );
        if (!response.ok) {
            throw new Error(
                `Unable to load test output for ${groupOrProjectPath.join("/")} job ${jobId} run ${jobRunId} test ${testId}`
            );
        }
        return (await response.json()) as TestOutput;
    }

    public async getDashboard(groupOrProjectPath: string[], branchName?: string): Promise<DashboardNode> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/dashboard${branchName ? `?branchName=${encodeURIComponent(branchName)}` : ""}`
        );
        if (!response.ok) {
            throw new Error(`Unable to find group ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as GroupDashboardNode;
    }

    public getDownloadTestsCsvUrl(groupOrProjectPath: string[], jobId: string, jobRunId: string): string {
        return `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/jobs/${encodeURIComponent(jobId)}/runs/${encodeURIComponent(jobRunId)}/tests/download-csv`;
    }
}

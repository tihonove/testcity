import { DashboardNode, GroupDashboardNode } from "../ProjectDashboardNode";

export class TestCityRunsApiClient {
    private readonly apiUrl: string;

    public constructor(apiUrl: string) {
        this.apiUrl = apiUrl;
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

    public async getDashboard(groupOrProjectPath: string[], branchName?: string): Promise<DashboardNode> {
        const response = await fetch(
            `${this.apiUrl}groups-v2/${groupOrProjectPath.map(x => encodeURIComponent(x)).join("/")}/dashboard${branchName ? `?branchName=${encodeURIComponent(branchName)}` : ""}`
        );
        if (!response.ok) {
            throw new Error(`Unable to find group ${groupOrProjectPath.join("/")}`);
        }
        return (await response.json()) as GroupDashboardNode;
    }
}

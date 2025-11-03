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
}

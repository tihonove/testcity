import { Content } from "../types/Content";
import { Severity } from "../types/Severity";

export type SeverityOverview = {
    total: number;
    issues: Record<string, IssueOverview>;
};

export type IssueOverview = {
    count: number;
    content?: Content;
    categories?: string[];
};

export type DiffOverview = {
    fixed: Partial<Record<Severity | "unknown", SeverityOverview>>;
    added: Partial<Record<Severity | "unknown", SeverityOverview>>;
};

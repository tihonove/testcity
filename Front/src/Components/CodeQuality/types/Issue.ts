import { Location } from "./Location";
import { Severity } from "./Severity";
import { Content } from "./Content";

// https://docs.gitlab.com/ee/ci/testing/code_quality.html#code-quality-report-format
export type Issue = {
    type?: null | string;
    check_name?: string;
    description: string;
    content?: Content;
    categories?: string[];
    location: Location;
    severity: Severity;
    remediation_points?: number;
    fingerprint: string;
};

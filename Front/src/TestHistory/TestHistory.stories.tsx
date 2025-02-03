import { StoryFn } from "@storybook/react";

import { TestHistory } from "./TestHistory";
import * as React from "react";

export default {
    title: "TestHistory",
};

export const Simple: StoryFn = () => (
    <TestHistory
        testId="testId"
        jobId={undefined}
        onChangeJobId={() => {}}
        jobIds={["jobId"]}
        branch={undefined}
        onChangeBranch={() => {}}
        branchNames={["main", "develop"]}
        totalRunCount={10}
        runsFetcher={() => []}
        onRunsPageChange={() => {}}
        runsPage={1}
        statusMessages={[]}
        runIdBreadcrumb={undefined}
        stats={[
            ["Success", 12, "2010-01-01T12:00:00Z"],
            ["Success", 12, "2010-01-01T11:00:00Z"],
        ]}
    />
);

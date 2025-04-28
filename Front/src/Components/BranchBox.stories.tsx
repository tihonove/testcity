import * as React from "react";
import type { Story } from "@ladle/react";
import { BranchBox } from "./BranchBox";

export const DefaultBranch: Story = () => <BranchBox name="main" />;

export const RegularBranch: Story = () => <BranchBox name="feature/add-new-component" />;

export const LongBranchName: Story = () => (
    <div style={{ width: "200px" }}>
        <BranchBox name="feature/very-long-branch-name-that-should-be-truncated-with-ellipsis" />
    </div>
);

export const MultipleBranches: Story = () => (
    <div style={{ display: "flex", gap: "10px", flexWrap: "wrap" }}>
        <BranchBox name="main" />
        <BranchBox name="master" />
        <BranchBox name="develop" />
        <BranchBox name="feature/add-user-profile" />
        <BranchBox name="bugfix/fix-login-error" />
        <BranchBox name="release/v1.2.0" />
    </div>
);

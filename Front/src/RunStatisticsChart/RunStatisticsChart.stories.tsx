import * as React from "react";
import { RunStatisticsChart } from "./RunStatisticsChart";
import { StoryFn } from "@storybook/react";

export default {
    title: "RunStatisticsChart",
};

export const SingleItem: StoryFn = () => <RunStatisticsChart value={[["Success", 12, "2010-01-01T12:00:00Z"]]} />;

export const LotOfItems: StoryFn = () => (
    <RunStatisticsChart
        value={new Array(100)
            .fill(null)
            .map((x, index) => [
                Math.random() < 0.05 ? "Failed" : "Success",
                Math.random() * 10 * 60 * 1000,
                "2010-01-01T12:00:00Z",
            ])}
    />
);

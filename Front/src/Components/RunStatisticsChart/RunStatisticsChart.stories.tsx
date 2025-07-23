import * as React from "react";
import { RunStatisticsChart } from "./RunStatisticsChart";
import { Story } from "@ladle/react";

export default {
    title: "RunStatisticsChart",
};

export const SingleItem: Story = () => <RunStatisticsChart value={[["Success", 12, "2010-01-01T12:00:00Z"]]} />;

export const LotOfItems: Story = () => (
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

export const HugeAmountOfItems: Story = () => (
    <RunStatisticsChart
        value={new Array(1000)
            .fill(null)
            .map((x, index) => [
                Math.random() < 0.05 ? "Failed" : "Success",
                Math.random() * 10 * 60 * 1000,
                "2010-01-01T12:00:00Z",
            ])}
    />
);

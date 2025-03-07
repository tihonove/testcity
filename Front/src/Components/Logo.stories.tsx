import React from "react";
import { Meta, StoryFn } from "@storybook/react";
import TestCityLogo from "./Logo";

const meta: Meta = {
    title: "Logo",
    component: TestCityLogo,
};
export default meta;

export const Default: StoryFn = () => <TestCityLogo />;

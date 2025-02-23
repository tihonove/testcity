import { StoryFn } from "@storybook/react";

import { GroupsPage } from "./GroupsPage";
import * as React from "react";

export default {
    title: "GroupsPage",
};

export const Simple: StoryFn = () => <GroupsPage />;

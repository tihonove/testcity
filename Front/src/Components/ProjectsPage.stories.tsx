import { StoryFn } from "@storybook/react";

import { ProjectsPage } from "./ProjectsPage";
import * as React from "react";

export default {
    title: "ProjectsPage",
};

export const Simple: StoryFn = () => <ProjectsPage />;

import * as React from "react";
import { Route, Routes } from "react-router-dom";
import { TestHistoryPage } from "./Pages/TestHistoryPage";
import styled from "styled-components";
import { JobsPage } from "./Pages/JobsPage";
import { JobRunsPage } from "./Pages/JobRunsPage";
import { JobRunPage } from "./Pages/JobRunPage";
import { TestsTreeMapChart } from "./Pages/TestsTreeMapChart";
import { Button } from "@skbkontur/react-ui";
import { WeatherMoonIcon20Regular, WeatherSunIcon20Regular } from "@skbkontur/icons";
import { useDarkMode } from "usehooks-ts";
import { MergeRequestJobsPage } from "./Pages/MergeRequestJobsPage";

export function App(): React.JSX.Element {
    const { isDarkMode, toggle, enable, disable } = useDarkMode();

    return (
        <AppContainer>
            <ThemeSwitchContainer>
                <Button use="link" onClick={toggle}>
                    {isDarkMode ? <WeatherMoonIcon20Regular /> : <WeatherSunIcon20Regular />}
                </Button>
            </ThemeSwitchContainer>
            <Routes>
                <Route path="/test-analytics">
                    <Route index element={<JobsPage />} />
                    <Route path="projects" element={<JobsPage />} />
                    <Route path="projects/:projectId" element={<JobsPage />} />

                    <Route
                        path="projects/:projectId/merge-requests/:gitLabMergeRequestId/jobs"
                        element={<MergeRequestJobsPage />}
                    />

                    <Route path="jobs" element={<JobsPage />} />
                    <Route path="jobs/:jobId" element={<JobRunsPage />} />
                    <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunPage />} />
                    <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                    <Route path="history" element={<TestHistoryPage />} />
                </Route>
            </Routes>
        </AppContainer>
    );
}

const ThemeSwitchContainer = styled.div({
    position: "absolute",
    top: 20,
    right: 20,
});

const AppContainer = styled.div({
    padding: 20,
});

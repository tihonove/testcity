import { WeatherMoonIcon20Regular, WeatherSunIcon20Regular } from "@skbkontur/icons";
import { Button } from "@skbkontur/react-ui";
import * as React from "react";
import { Navigate, Route, Routes } from "react-router-dom";
import styled from "styled-components";
import { useBasePrefix } from "./Domain/Navigation";
import { CodeQualityPage } from "./Pages/CodeQualityPage";
import { GroupsPage } from "./Pages/GroupsPage";
import { JobRunsPage } from "./Pages/JobRunsPage";
import { JobRunTestListPage } from "./Pages/JobRunTestListPage";
import { MergeRequestJobsPage } from "./Pages/MergeRequestJobsPage";
import { PipelineRunTestListPage } from "./Pages/PipelineRunTestListPage";
import { ProjectsDashboardPage } from "./Pages/ProjectsDashboardPage";
import { TestHistoryPage } from "./Pages/TestHistoryPage";
import { TestsTreeMapChart } from "./Pages/TestsTreeMapChart";
import { TestsTreeView } from "./Pages/TestsTreeView";
import { useTestAnalyticsDarkMode } from "./Theme/UseTestAnalyticsDarkMode";

export function App(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { isDarkMode, toggle } = useTestAnalyticsDarkMode();

    return (
        <AppContainer>
            <ThemeSwitchContainer>
                <Button use="link" onClick={toggle}>
                    {isDarkMode ? <WeatherMoonIcon20Regular /> : <WeatherSunIcon20Regular />}
                </Button>
            </ThemeSwitchContainer>
            <Routes>
                <Route path={`/${basePrefix}`}>
                    <Route index element={<GroupsPage />} />
                    <Route path="history" element={<TestHistoryPage />} />
                    <Route
                        path="projects/:projectId/merge-requests/:gitLabMergeRequestId/jobs"
                        element={<MergeRequestJobsPage />}
                    />
                    <Route path="jobs" element={<Navigate to={`/${basePrefix}`} />} />

                    <Route path=":groupIdLevel1">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                    </Route>

                    <Route path=":groupIdLevel1/:groupIdLevel2">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                    </Route>

                    <Route path=":groupIdLevel1/:groupIdLevel2/:groupIdLevel3">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                    </Route>

                    <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                    <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                    <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />

                    <Route path="project/:projectId/jobs/:jobId/codequality" element={<CodeQualityPage />} />
                </Route>
            </Routes>
        </AppContainer>
    );
}

const ThemeSwitchContainer = styled.div({
    position: "fixed",
    top: 20,
    right: 20,
});

const AppContainer = styled.div({
    padding: 20,
});

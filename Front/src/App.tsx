import { WeatherMoonIcon20Regular, WeatherSunIcon20Regular, WeatherSunMoonIcon20Regular } from "@skbkontur/icons";
import { Button, Hint } from "@skbkontur/react-ui";
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
import { TestsTreeMapChart } from "./Components/TestsTreeMapChart";
import { TestsTreeView } from "./Components/TestsTreeView";
import { useTestAnalyticsDarkMode } from "./Theme/UseTestAnalyticsDarkMode";
import { RedirectToNewTestHistoryPage } from "./RedirectToNewTestHistoryPage";

export function App(): React.JSX.Element {
    const basePrefix = useBasePrefix();
    const { ternaryDarkMode, toggle } = useTestAnalyticsDarkMode();

    return (
        <AppContainer>
            <ThemeSwitchContainer>
                <Hint text={`Toggle color mode. Current: ${ternaryDarkMode}`} pos="left middle" maxWidth={400}>
                    <Button use="link" onClick={toggle}>
                        {ternaryDarkMode === "dark" ? (
                            <WeatherMoonIcon20Regular />
                        ) : ternaryDarkMode === "light" ? (
                            <WeatherSunIcon20Regular />
                        ) : (
                            <WeatherSunMoonIcon20Regular />
                        )}
                    </Button>
                </Hint>
            </ThemeSwitchContainer>
            <Routes>
                <Route path={basePrefix}>
                    <Route index element={<GroupsPage />} />
                    <Route path="history" element={<RedirectToNewTestHistoryPage />} />
                    <Route
                        path="projects/:projectId/merge-requests/:gitLabMergeRequestId/jobs"
                        element={<MergeRequestJobsPage />}
                    />
                    <Route path="jobs" element={<Navigate to={basePrefix} />} />

                    <Route path=":groupIdLevel1">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                        <Route path="test-history" element={<TestHistoryPage />} />
                    </Route>

                    <Route path=":groupIdLevel1/:groupIdLevel2">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                        <Route path="test-history" element={<TestHistoryPage />} />
                    </Route>

                    <Route path=":groupIdLevel1/:groupIdLevel2/:groupIdLevel3">
                        <Route index element={<ProjectsDashboardPage />} />
                        <Route path="jobs/:jobId" element={<JobRunsPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunTestListPage />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treemap" element={<TestsTreeMapChart />} />
                        <Route path="jobs/:jobId/runs/:jobRunId/treeview" element={<TestsTreeView />} />
                        <Route path="pipelines/:pipelineId" element={<PipelineRunTestListPage />} />
                        <Route path="test-history" element={<TestHistoryPage />} />
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
    top: 10,
    right: 10,
});

const AppContainer = styled.div({
    padding: 20,
});

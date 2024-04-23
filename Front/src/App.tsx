import * as React from "react";
import { Route, Routes } from "react-router-dom";
import { TestHistoryPage } from "./Pages/TestHistoryPage";
import styled from "styled-components";
import { JobsPage } from "./Pages/JobsPage";
import { JobRunsPage } from "./Pages/JobRunsPage";
import { JobRunPage } from "./Pages/JobRunPage";

export function App(): React.JSX.Element {
    return (
        <AppContainer>
            <Routes>
                <Route path="/test-analytics">
                    <Route index element={<JobsPage />} />
                    <Route path="jobs" element={<JobsPage />} />
                    <Route path="jobs/:jobId" element={<JobRunsPage />} />
                    <Route path="jobs/:jobId/runs/:jobRunId" element={<JobRunPage />} />
                    <Route path="history" element={<TestHistoryPage />} />
                </Route>
            </Routes>
        </AppContainer>
    );
}

const AppContainer = styled.div({
    padding: 20,
});

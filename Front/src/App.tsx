import * as React from "react";
import {Route, Routes} from "react-router-dom";
import {TestHistoryPage} from "./TestHistoryPage";
import styled from "styled-components";

export function App(): React.JSX.Element {
    return <AppContainer>
        <Routes>
            <Route path="/test-analytics">
                <Route index element={<div>Home</div>}/>
                <Route path="history" element={<TestHistoryPage />}/>
            </Route>
        </Routes>
    </AppContainer>;
}

const AppContainer = styled.div({
    padding: 20,
})
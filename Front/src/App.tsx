import * as React from "react";
import {Route, Routes} from "react-router-dom";
import {TestHistoryPage} from "./TestHistoryPage";

export function App(): React.JSX.Element {
    return <div>
        <Routes>
            <Route path="/test-analytics">
                <Route index element={<div>Home</div>}/>
                <Route path="history" element={<TestHistoryPage />}/>
            </Route>
        </Routes>
    </div>;
}


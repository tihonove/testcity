import * as React from "react";
import {createRoot} from "react-dom/client";
import {App} from "./App";
import {BrowserRouter} from "react-router-dom";

import "./CommonStyles/Fonts.css";
import "./CommonStyles/Reset.css";
import {ThemeContext, THEME_2022} from "@skbkontur/react-ui";
import {TestAnalyticsThemeProvider} from "./Theme/TestAnalyticsThemeProvider";

const root = createRoot(document.getElementById('root')!);
root.render(
    <React.StrictMode>
        <BrowserRouter>
            <TestAnalyticsThemeProvider>
                <App/>
            </TestAnalyticsThemeProvider>
        </BrowserRouter>
    </React.StrictMode>
);


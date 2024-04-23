import * as React from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { BrowserRouter } from "react-router-dom";
import { TestAnalyticsThemeProvider } from "./Theme/TestAnalyticsThemeProvider";

import "./CommonStyles/Fonts.css";
import "./CommonStyles/Reset.css";
import { GlobalStyle } from "./CommonStyles/GlobalStyle";
import {Suspense} from "react";

const root = createRoot(document.getElementById("root")!);

root.render(
    <React.StrictMode>
        <BrowserRouter>
            <TestAnalyticsThemeProvider>
                <Suspense fallback="Loading...">
                    <GlobalStyle />
                    <App />
                </Suspense>
            </TestAnalyticsThemeProvider>
        </BrowserRouter>
    </React.StrictMode>
);
